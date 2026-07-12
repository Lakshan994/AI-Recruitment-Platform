using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecruitmentPlatform.API.Models
{
    public class JobPosting
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string RecruiterId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
