namespace RecruitmentPlatform.API.DTOs
{
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }

    public class AdminAnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalRecruiters { get; set; }
        public int TotalHiringManagers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public int ApplicationsApplied { get; set; }
        public int ApplicationsShortlisted { get; set; }
        public int ApplicationsInterviewed { get; set; }
        public int ApplicationsHired { get; set; }
        public int ApplicationsRejected { get; set; }
    }
}
