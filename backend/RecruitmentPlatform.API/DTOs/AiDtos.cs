namespace RecruitmentPlatform.API.DTOs
{
    public class ParsedResumeDto
    {
        public string Bio { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty; // Comma-separated list of skills
        public string Experience { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
    }

    public class MatchResultDto
    {
        public decimal MatchScore { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class PerformanceAnalyticsDto
    {
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public double ShortlistingRate { get; set; } // Shortlisted applications / Total
        public double AverageMatchScore { get; set; }
        public double AverageTimeToHireDays { get; set; } // Average duration in days from Job.CreatedAt to Application.Status = Hired
    }

    public class JobRecommendationResultDto
    {
        public string JobId { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
    }
}
