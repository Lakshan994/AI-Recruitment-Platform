using System.Collections.Generic;
using System.Threading.Tasks;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Interfaces
{
    public interface IAiService
    {
        Task<ParsedResumeDto> ParseResumeAsync(string resumeText);
        Task<MatchResultDto> MatchCandidateToJobAsync(User candidate, JobPosting job);
        Task<string> AnalyzeHiringTrendsAsync(List<JobPosting> jobs, List<Application> applications, List<Department> departments);
        Task<List<JobRecommendationResultDto>> GetJobRecommendationsAsync(User candidate, List<JobPosting> activeJobs);
        Task<string> GenerateFeedbackAsync(User candidate, JobPosting job, Application application, int evaluationScore);
        Task<string> ConductLiveInterviewAsync(JobPosting job, User candidate, List<ChatMessageDto> chatHistory, string newAnswer);
        Task<string> GenerateJobDescriptionAsync(string title, string skills, string additionalPrompt);
    }
}
