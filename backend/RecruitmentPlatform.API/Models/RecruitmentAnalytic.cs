using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class RecruitmentAnalytic
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty; // JobCreated, ApplicationSubmitted, CandidateHired, InterviewScheduled
        public string Description { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
