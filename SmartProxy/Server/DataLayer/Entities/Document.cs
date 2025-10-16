using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace DataLayer.Entities
{
    public abstract class Document
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyOrder(-1)]
        public string Id { get; set; } = null!;

        [BsonElement("LastChangedAt")]
        public DateTime LastChangedAt { get; set; } = DateTime.UtcNow;
    }
}
