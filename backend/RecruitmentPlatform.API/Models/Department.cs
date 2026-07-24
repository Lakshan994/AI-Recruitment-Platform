using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class Department
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public int StaffCount { get; set; }
        public int Vacancies { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
