using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPlatform.API.Models
{
    public class SkillAssessment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        public string SkillName { get; set; } = string.Empty;
        public decimal Score { get; set; } // Score out of 100
        public string Status { get; set; } = "Completed"; // Pending, Completed
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CandidateId))]
        public User? Candidate { get; set; }
    }
}
