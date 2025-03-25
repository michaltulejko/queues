using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QueueApi.Models
{
    public class DelayEntity
    {
        // Map MongoDB's _id to this property.
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("queueCreationTime")]
        public long QueueCreationTime { get; set; }

        [BsonElement("queueCreationDelay")]
        public double QueueCreationDelay { get; set; }

        [BsonElement("processingTime")]
        public long ProcessingTime { get; set; }

        [BsonElement("processingDelay")]
        public double ProcessingDelay { get; set; }

        [BsonElement("queue")]
        public string QueueName { get; set; }
    }
}
