using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPlatform.API.Models
{
    public class Application
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string JobPostingId { get; set; } = string.Empty;
        public string CandidateId { get; set; } = string.Empty;
        public string ResumeUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Applied"; // Applied, Shortlisted, Interviewed, Rejected, Hired
        public decimal AiMatchScore { get; set; } // Score out of 100
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public int? EvaluationScore { get; set; } // Score out of 5
        public string? InterviewFeedback { get; set; } = string.Empty; // Feedback comments
        public string? AiMatchExplanation { get; set; } = string.Empty; // AI matching explanation

        [ForeignKey(nameof(JobPostingId))]
        public JobPosting? JobPosting { get; set; }

        [ForeignKey(nameof(CandidateId))]
        public User? Candidate { get; set; }
    }
}
