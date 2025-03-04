using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace QueueApi.Models
{
    public class DelayEntity
    {
        // Map MongoDB's _id to this property.
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("timestamp")]
        public long TimeStamp { get; set; }

        [BsonElement("delay")]
        public double Delay { get; set; }

        [BsonElement("queue")]
        public string QueueName { get; set; }
    }
}
