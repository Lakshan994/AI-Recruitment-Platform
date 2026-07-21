using System;
using System.Collections.Generic;

namespace RecruitmentPlatform.API.DTOs
{
    public class JobRecommendationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string RecruiterName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double MatchScore { get; set; }
        public List<string> MatchingSkills { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
    }
}
