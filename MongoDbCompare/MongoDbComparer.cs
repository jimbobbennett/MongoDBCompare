using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDbCompare
{
    /// <summary>
    /// A helper class that compares two MongoDB collections
    /// </summary>
    /// <typeparam name="T">The type for the document stored in the collection</typeparam>
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

        /// <summary>
        /// Creates a new instance of the comparer
        /// </summary>
        /// <param name="connectionString1">The connection string for the first MongoDB instsance for the comparison</param>
        /// <param name="database1">The name of the database in the first MongoDB instance to use for the comparison</param>
        /// <param name="collection1">The name of the collection in the first MongoDB instance to use for the comparison</param>
        /// <param name="connectionString2">The connection string for the second MongoDB instsance for the comparison</param>
        /// <param name="database2">The name of the database in the second MongoDB instance to use for the comparison.  If this is null then the name of the database from database1 is used</param>
        /// <param name="collection2">The name of the collection in the first MongoDB instance to use for the comparison.  If this is null then the name of the collection from collection1 is used</param>
        /// <param name="propertiesToIgnoreInTheComparison">The names of any properties to ignore when doing the comparison</param>
        public MongoDbComparer(string connectionString1, string database1, string collection1,
            string connectionString2, string database2 = null, string collection2 = null,
            IEnumerable<string> propertiesToIgnoreInTheComparison = null)
        {
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var toIgnore = new HashSet<string>(propertiesToIgnoreInTheComparison ?? Enumerable.Empty<string>());
            var propertyNames = new HashSet<string>(props.Select(p => p.Name));

            var name = toIgnore.FirstOrDefault(n => !propertyNames.Contains(n));
            if (name != null)
                throw new ArgumentException("Property " + name + " is not on the specified document type.", "propertiesToIgnoreInTheComparison");

            _propertyInfos = props.Where(p => !ShouldIgnore(p) && !toIgnore.Contains(p.Name)).ToList();

            _collection1 = new MongoClient(connectionString1).GetDatabase(database1).GetCollection<T>(collection1);
            _collection2 = new MongoClient(connectionString2).GetDatabase(database2 ?? database1).GetCollection<T>(collection2 ?? collection1);
        }

        /// <summary>
        /// Compares the two collections asyncronously and returns an IResult
        /// </summary>
        /// <typeparam name="TKey">The type for the unique key for the document</typeparam>
        /// <param name="idFunc">A func that returns the unique key for a given document</param>
        /// <returns>An IResult containing the results of the comparison</returns>
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
