
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataLayer.Entities
{
    public class Employee : Document
    {
        [BsonElement("FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("LastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("Email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("Position")]
        public string Position { get; set; } = string.Empty;

        [BsonElement("Salary")]
        public decimal Salary { get; set; } = 0;
    }
}
