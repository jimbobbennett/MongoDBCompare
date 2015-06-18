using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbCompare.Test
{
    [BsonIgnoreExtraElements]
    public class Item
    {
        public Item(int ignored, string name, int number, DateTime date, BsonDocument subDocument)
        {
            Ignored = ignored;
            Name = name;
            Number = number;
            Date = date;
            SubDocument = subDocument;
        }

        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonIgnore]
        public int Ignored { get; set; }

        public string Name { get; set; }

        public int Number { get; set; }

        public DateTime Date { get; set; }

        public BsonDocument SubDocument { get; set; }
    }
}
