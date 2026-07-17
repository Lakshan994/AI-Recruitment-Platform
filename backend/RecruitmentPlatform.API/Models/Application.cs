using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecruitmentPlatform.API.Models
{
    public class Application
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string JobPostingId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string CandidateId { get; set; } = string.Empty;
        public string ResumeUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Applied"; // Applied, Shortlisted, Interviewed, Rejected, Hired
        public decimal AiMatchScore { get; set; } // Score out of 100
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
