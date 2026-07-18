using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs
{
    public class SmsRequest
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string CandidateId { get; set; } = string.Empty;
    }
}