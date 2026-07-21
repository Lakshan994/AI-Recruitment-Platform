using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class Organization
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string HQLocation { get; set; } = string.Empty;
        public int Headcount { get; set; }
        public string Website { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
