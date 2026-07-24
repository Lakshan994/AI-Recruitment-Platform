using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs
{
    public class LiveInterviewRequestDto
    {
        [Required]
        public string ApplicationId { get; set; } = string.Empty;

        public List<ChatMessageDto> History { get; set; } = new();

        public string NewMessage { get; set; } = string.Empty;
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = "user"; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }

    public class LiveInterviewResponseDto
    {
        public string Reply { get; set; } = string.Empty;
    }
}
