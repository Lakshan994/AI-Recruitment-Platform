using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Candidate"; // Candidate, Recruiter, HiringManager, Admin

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        // Computed property (Not stored in database)
        public string FullName => $"{FirstName} {LastName}";

        public string? PhoneNumber { get; set; }

        // Profile Information
        public string Bio { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Experience { get; set; } = string.Empty;

        public string Education { get; set; } = string.Empty;

        public string? ResumeUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties

        // Recruiter -> Job Postings
        public List<JobPosting> JobPostings { get; set; } = new();

        // Candidate -> Applications
        public List<Application> Applications { get; set; } = new();

        // Candidate -> Notifications
        public List<Notification> Notifications { get; set; } = new();
    }
}