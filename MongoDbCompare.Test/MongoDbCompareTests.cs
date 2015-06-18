using System;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDbCompare.Test
{
    [TestFixture]
    public class MongoDbCompareTests
    {
        private const string ConnectionString1 = "mongodb://localhost";
        private const string ConnectionString2 = "mongodb://localhost:27018";
        private const string Database1 = "TestDB";
        private const string Database2 = "TestDB";
        private const string Collection1 = "Items";
        private const string Collection2 = "Items";

        private IMongoCollection<Item> _collection1;
        private IMongoCollection<Item> _collection2;
        private MongoDbComparer<Item> _comparer;

        [SetUp]
        public void SetUp()
        {
            _collection1 = new MongoClient(ConnectionString1).GetDatabase(Database1).GetCollection<Item>(Collection1);
            _collection2 = new MongoClient(ConnectionString2).GetDatabase(Database2).GetCollection<Item>(Collection2);

            _comparer = new MongoDbComparer<Item>(ConnectionString1, Database1, Collection1, ConnectionString2, Database2, Collection2);

            // ReSharper disable UnusedVariable
            var r1 = _collection1.DeleteManyAsync(i => true).Result;
            var r2 = _collection2.DeleteManyAsync(i => true).Result;
            // ReSharper restore UnusedVariable
        }

        private async Task<Item> AddItemToCollection1Async(int ignored, string name, int number, DateTime date, BsonDocument subDocument)
        {
            var item = new Item(ignored, name, number, date, subDocument);
            await _collection1.InsertOneAsync(item);
            return item;
        }

        private async Task<Item> AddItemToCollection2Async(int ignored, string name, int number, DateTime date, BsonDocument subDocument)
        {
            var item = new Item(ignored, name, number, date, subDocument);
            await _collection2.InsertOneAsync(item);
            return item;
        }

        [Test]
        public async Task IdenticalItemsMatchAsync()
        {
            var now = DateTime.Now;

            await AddItemToCollection1Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection1Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));
            await AddItemToCollection2Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection2Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));

            var results = await _comparer.CompareAsync(i => i.Name);

            results.Match.Should().BeTrue();
            results.OnlyInCollection1.Should().BeEmpty();
            results.OnlyInCollection2.Should().BeEmpty();
            results.Different.Should().BeEmpty();
        }

        [Test]
        public async Task NonIdenticalItemsMatchIfTheNonMatchingFieldIsIgnoredAsync()
        {
            var now = DateTime.Now;

            await AddItemToCollection1Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection1Async(0, "Item2", 1, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));
            await AddItemToCollection2Async(0, "Item1", 2, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection2Async(0, "Item2", 2, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));

            var comparer = new MongoDbComparer<Item>(ConnectionString1, Database1, Collection1, ConnectionString2, Database2, Collection2, new[] {"Number"});
            var results = await comparer.CompareAsync(i => i.Name);

            results.Match.Should().BeTrue();
            results.OnlyInCollection1.Should().BeEmpty();
            results.OnlyInCollection2.Should().BeEmpty();
            results.Different.Should().BeEmpty();
        }

        [Test]
        public async Task NonIdenticalItemsDontMatchAndAreReturnedAsDifferencesAsync()
        {
            var now = DateTime.Now;

            await AddItemToCollection1Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection1Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("Foo", "2").AddRange(new BsonDocument("Bar", 1)));
            await AddItemToCollection2Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection2Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));

            var results = await _comparer.CompareAsync(i => i.Name);

            results.Match.Should().BeFalse();
            results.OnlyInCollection1.Should().BeEmpty();
            results.OnlyInCollection2.Should().BeEmpty();
            results.Different.Should().Contain(t => t.Item1.Name == "Item2" && t.Item2.Name == "Item2");
        }

        [Test]
        public async Task ItemsOnlyInOneCollectionDontMatchAndThoseItemsAreInTheResultsAsync()
        {
            var now = DateTime.Now;

            await AddItemToCollection1Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection1Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("Foo", "2").AddRange(new BsonDocument("Bar", 1)));
            await AddItemToCollection2Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection2Async(0, "Item3", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));

            var results = await _comparer.CompareAsync(i => i.Name);

            results.Match.Should().BeFalse();
            results.OnlyInCollection1.Should().Contain(i => i.Name == "Item2");
            results.OnlyInCollection2.Should().Contain(i => i.Name == "Item3");
            results.Different.Should().BeEmpty();
        }

        [Test]
        public async Task ItemsOnlyInOneCollectionAndDifferencesAreReturnedInTheResultsAsync()
        {
            var now = DateTime.Now;

            await AddItemToCollection1Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection1Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("Foo", "2").AddRange(new BsonDocument("Bar", 1)));
            await AddItemToCollection1Async(0, "Item4", 21, now.AddDays(1), new BsonDocument("Foo", "2").AddRange(new BsonDocument("Bar", 1)));
            await AddItemToCollection2Async(0, "Item1", 1, now, new BsonDocument("Foo", "1").AddRange(new BsonDocument("Bar", 0)));
            await AddItemToCollection2Async(0, "Item2", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));
            await AddItemToCollection2Async(0, "Item3", 21, now.AddDays(1), new BsonDocument("FooBar", "2").AddRange(new BsonDocument("BarFoo", 1)));

            var results = await _comparer.CompareAsync(i => i.Name);

            results.Match.Should().BeFalse();
            results.OnlyInCollection1.Should().Contain(i => i.Name == "Item4");
            results.OnlyInCollection2.Should().Contain(i => i.Name == "Item3");
            results.Different.Should().Contain(t => t.Item1.Name == "Item2" && t.Item2.Name == "Item2");
        }
    }
}
