using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPlatform.API.Models
{
    public class Interview
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        [Required]
        public string JobPostingId { get; set; } = string.Empty;

        [Required]
        public DateTime InterviewDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string MeetingLink { get; set; } = string.Empty;

        public string Status { get; set; } = "Scheduled";
        // Scheduled, Completed, Cancelled

        public bool ReminderSent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CandidateId))]
        public User? Candidate { get; set; }

        [ForeignKey(nameof(JobPostingId))]
        public JobPosting? JobPosting { get; set; }
    }
}