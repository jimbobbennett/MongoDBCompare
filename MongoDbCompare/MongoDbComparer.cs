using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDbCompare
{
    public class MongoDbComparer<T>
        where T : class
    {
        private readonly IMongoCollection<T> _collection1;
        private readonly IMongoCollection<T> _collection2;
        private readonly IList<PropertyInfo> _propertyInfos;
        
        private static bool ShouldIgnore(MemberInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes<BsonIgnoreAttribute>().Any() ||
                    propertyInfo.GetCustomAttributes<BsonIdAttribute>().Any();
        }

        public MongoDbComparer(string connectionString1, string database1, string collection1, 
            string connectionString2, string database2, string collection2,
            IEnumerable<string> propertiesToIgnoreInTheComparison = null)
        {
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var toIgnore = new HashSet<string>(propertiesToIgnoreInTheComparison ?? Enumerable.Empty<string>());

            _propertyInfos = props.Where(p => !ShouldIgnore(p) && !toIgnore.Contains(p.Name)).ToList();

            _collection1 = new MongoClient(connectionString1).GetDatabase(database1).GetCollection<T>(collection1);
            _collection2 = new MongoClient(connectionString2).GetDatabase(database2).GetCollection<T>(collection2);
        }

        public async Task<IResults<T>> CompareAsync<TKey>(Func<T, TKey> idFunc)
        {
            var itemsIn1Only = new List<T>();
            var itemsIn2Only = new List<T>();
            var itemsThatDontMatch = new List<Tuple<T, T>>();

            var items1 = await GetItemsDictionary(idFunc, _collection1);
            var items2 = await GetItemsDictionary(idFunc, _collection2);

            foreach (var item in items1)
            {
                T otherItem;
                if (items2.TryGetValue(item.Key, out otherItem))
                {
                    if (!CompareItems(item.Value, otherItem))
                        itemsThatDontMatch.Add(Tuple.Create(item.Value, otherItem));
                }
                else
                    itemsIn1Only.Add(item.Value);
            }

            foreach (var item in items2)
            {
                T otherItem;
                if (!items1.TryGetValue(item.Key, out otherItem))
                    itemsIn2Only.Add(item.Value);
            }

            return new Results<T>(itemsIn1Only, itemsIn2Only, itemsThatDontMatch);
        }

        private bool CompareItems(T item, T otherItem)
        {
            return !(from propertyInfo in _propertyInfos
                let val1 = propertyInfo.GetValue(item)
                let val2 = propertyInfo.GetValue(otherItem)
                where !Equals(val1, val2)
                select val1).Any();
        }

        private static async Task<Dictionary<TKey, T>> GetItemsDictionary<TKey>(Func<T, TKey> idFunc, IMongoCollection<T> collection)
        {
            var collectionItems = await collection.FindAsync(i => true);
            return (await collectionItems.ToListAsync()).ToDictionary(idFunc, i => i);
        }
    }
}
