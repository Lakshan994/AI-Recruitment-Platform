using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs
{
    public class EmailRequest
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public string CandidateId { get; set; } = string.Empty;
    }
}