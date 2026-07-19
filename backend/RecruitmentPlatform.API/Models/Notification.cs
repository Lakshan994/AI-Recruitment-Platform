using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPlatform.API.Models
{
    public class Notification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        [ForeignKey(nameof(CandidateId))]
        public User? Candidate { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;   // Email, SMS

        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}