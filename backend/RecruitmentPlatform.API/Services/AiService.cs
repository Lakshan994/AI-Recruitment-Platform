using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Interfaces;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiService> _logger;

        public AiService(HttpClient httpClient, IConfiguration configuration, ILogger<AiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<string> CallGeminiAsync(string systemPrompt, string userPrompt)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API Key is not configured.");
                throw new InvalidOperationException("AI Service is not fully configured.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            
            // Reusing the model structure from ChatModels
            var geminiRequest = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = userPrompt } }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error response from Gemini API: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"Gemini API request failed: {response.StatusCode}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                // Parse the response using JsonDocument to avoid strong typing issues with different response shapes
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.ValueKind == JsonValueKind.Array && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var candidateContent) && 
                        candidateContent.TryGetProperty("parts", out var parts) && 
                        parts.ValueKind == JsonValueKind.Array && 
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? string.Empty;
                    }
                }

                _logger.LogWarning("Gemini response did not contain candidates or content parts. Response: {Response}", responseString);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while invoking Gemini API.");
                throw;
            }
        }

        private string CleanJson(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```json"))
            {
                text = text.Substring(7);
            }
            else if (text.StartsWith("```"))
            {
                text = text.Substring(3);
            }
            if (text.EndsWith("```"))
            {
                text = text.Substring(0, text.Length - 3);
            }
            return text.Trim();
        }

        public async Task<ParsedResumeDto> ParseResumeAsync(string resumeText)
        {
            var systemPrompt = "You are an expert recruitment parser AI. Analyze the provided resume text and extract the candidate details. You must respond ONLY with a valid, clean JSON object matching the schema: { \"Bio\": \"A short professional summary\", \"Skills\": \"Comma-separated list of skills like React, C#, SQL\", \"Experience\": \"Brief summary of past work roles and companies\", \"Education\": \"Brief summary of degrees and institutions\" }. Do not add any markdown, comments, or extra text.";
            
            try
            {
                var response = await CallGeminiAsync(systemPrompt, resumeText);
                var cleanedJson = CleanJson(response);
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<ParsedResumeDto>(cleanedJson, options);
                return result ?? new ParsedResumeDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse resume with AI service.");
                return new ParsedResumeDto
                {
                    Bio = "Failed to parse automatically. Please update your bio manually.",
                    Skills = string.Empty,
                    Experience = "Please update your experience details.",
                    Education = "Please update your education details."
                };
            }
        }

        public async Task<MatchResultDto> MatchCandidateToJobAsync(User candidate, JobPosting job)
        {
            var systemPrompt = "You are an expert technical recruiter AI. Evaluate how well a candidate fits a job posting. You must evaluate using the candidate's skills, experience, education, and bio against the job title, description, and required skills. Respond ONLY with a valid, clean JSON object matching the schema: { \"MatchScore\": 85.5, \"Explanation\": \"Detailed explanation explaining candidate strengths, skill matches, and key gaps\" }. MatchScore must be a decimal between 0 and 100. Do not include markdown code block syntax.";

            var userPrompt = $@"
Job Posting:
Title: {job.Title}
Required Skills: {job.RequiredSkills}
Description: {job.Description}

Candidate Profile:
Skills: {candidate.Skills}
Experience: {candidate.Experience}
Education: {candidate.Education}
Bio: {candidate.Bio}
";

            try
            {
                var response = await CallGeminiAsync(systemPrompt, userPrompt);
                var cleanedJson = CleanJson(response);
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<MatchResultDto>(cleanedJson, options);
                return result ?? new MatchResultDto { MatchScore = 50.0m, Explanation = "Neutral match fit." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate candidate job match with AI service.");
                return new MatchResultDto
                {
                    MatchScore = 60.0m,
                    Explanation = "Could not automatically calculate matching details due to service issues. Default score assigned."
                };
            }
        }

        public async Task<string> AnalyzeHiringTrendsAsync(List<JobPosting> jobs, List<Application> applications, List<Department> departments)
        {
            var systemPrompt = "You are an elite corporate recruitment consultant and market analyst. Analyze the company's hiring data and output a professional, rich-markdown formatted recruitment report. Do not include HTML tags. Provide three distinct sections: 1. Executive Summary & Hiring Forecast (predicting future department expansion based on vacancies), 2. High-Demand Skills Analysis (identifying most in-demand skills based on jobs and application pipelines), 3. Talent Pool Gap Recommendations (identifying gaps in current applications and advising how to resolve them).";

            var jobSummary = string.Join("\n", jobs.Select(j => $"- {j.Title} (Active: {j.IsActive}, Required Skills: {j.RequiredSkills})"));
            var deptSummary = string.Join("\n", departments.Select(d => $"- {d.Name} Code: {d.Code} (Current Staff: {d.StaffCount}, Active Vacancies: {d.Vacancies}, Manager: {d.Manager})"));
            var appSummary = $"Total Applications: {applications.Count}. Status Breakdown: Applied({applications.Count(a => a.Status == "Applied")}), Shortlisted({applications.Count(a => a.Status == "Shortlisted")}), Interviewed({applications.Count(a => a.Status == "Interviewed")}), Hired({applications.Count(a => a.Status == "Hired")}), Rejected({applications.Count(a => a.Status == "Rejected")}). Average AI Match Score: {(applications.Any() ? applications.Average(a => a.AiMatchScore).ToString("F2") : "N/A")}%";

            var userPrompt = $@"
Hiring Data Analysis Request:

1. Departments Details:
{deptSummary}

2. Active and Past Job Postings:
{jobSummary}

3. Application Pipeline Status:
{appSummary}

Generate the detailed trend report now.
";

            try
            {
                return await CallGeminiAsync(systemPrompt, userPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze hiring trends with AI service.");
                return "## Hiring Trend Analysis Unavailable\n\nAI service is currently unable to load hiring trend predictions. Please try again later.";
            }
        }

        public async Task<List<JobRecommendationResultDto>> GetJobRecommendationsAsync(User candidate, List<JobPosting> activeJobs)
        {
            var systemPrompt = "You are a professional recruitment matchmaking AI. Evaluate the candidate's profile against the list of active job postings. Recommend the top postings that match the candidate's background. Respond ONLY with a valid, clean JSON array matching the schema: [ { \"JobId\": \"guid-string\", \"MatchScore\": 88.5, \"RecommendationReason\": \"1-2 sentence description explaining why this job fits the candidate's skills and background.\" } ]. Do not include markdown code block syntax or extra text.";

            var activeJobsSummary = string.Join("\n\n", activeJobs.Select(j => $"Job ID: {j.Id}\nTitle: {j.Title}\nRequired Skills: {j.RequiredSkills}\nDescription: {j.Description}"));

            var userPrompt = $@"
Candidate Profile:
Skills: {candidate.Skills}
Experience: {candidate.Experience}
Education: {candidate.Education}
Bio: {candidate.Bio}

Active Job Postings:
{activeJobsSummary}
";

            try
            {
                var response = await CallGeminiAsync(systemPrompt, userPrompt);
                var cleanedJson = CleanJson(response);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<List<JobRecommendationResultDto>>(cleanedJson, options);
                return result ?? new List<JobRecommendationResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job recommendations with AI service.");
                return new List<JobRecommendationResultDto>();
            }
        }

        public async Task<string> GenerateFeedbackAsync(User candidate, JobPosting job, Application application, int evaluationScore)
        {
            var systemPrompt = "You are an expert technical interviewer and HR specialist. Write a professional, constructive candidate interview feedback assessment based on their rating and application background. Respond with a concise paragraph (2-3 sentences) detailing their strengths, potential suitability, and areas to improve. Do not include markdown formatting or HTML.";

            var userPrompt = $@"
Candidate Name: {candidate.FullName}
Job Title: {job.Title}
AI Job Match Fit Score: {application.AiMatchScore}%
AI Fit Analysis: {application.AiMatchExplanation}

Hiring Manager Interview Score: {evaluationScore} out of 5 stars (1 is Poor, 3 is Average, 5 is Outstanding).

Write a feedback comment for this candidate's evaluation:
";

            try
            {
                var response = await CallGeminiAsync(systemPrompt, userPrompt);
                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate candidate evaluation feedback with AI service.");
                return "The candidate demonstrated relevant experience for the position. We recommend further review of their portfolio and technical skills based on the selection criteria.";
            }
        }
        public async Task<string> ConductLiveInterviewAsync(JobPosting job, User candidate, List<ChatMessageDto> chatHistory, string newAnswer)
        {
            var systemPrompt = $@"You are an expert technical interviewer and hiring manager at a top tech company.
You are conducting a live, interactive text-based interview with a candidate.
Job Title: {job.Title}
Job Description: {job.Description}
Required Skills: {job.RequiredSkills}

Candidate Profile:
Name: {candidate.FullName}
Bio: {candidate.Bio}
Experience: {candidate.Experience}
Skills: {candidate.Skills}

Instructions:
1. Review the conversation history.
2. If this is the start of the interview (no history), greet the candidate, introduce yourself, and ask the first scenario-based question.
3. If there is history, evaluate the candidate's latest answer, acknowledge it briefly, and present a practical scenario or technical problem for them to solve.
4. Keep your responses concise (1-2 paragraphs max).
5. Act professionally, but warmly. Do not break character. Do not use Markdown formatting unless necessary (e.g. code snippets).";

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API Key is not configured.");
                throw new InvalidOperationException("AI Service is not fully configured.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            var contents = new List<object>();
            
            // Add history
            foreach (var msg in chatHistory)
            {
                contents.Add(new
                {
                    role = msg.Role == "model" ? "model" : "user",
                    parts = new[] { new { text = msg.Text } }
                });
            }

            // Add new message
            if (!string.IsNullOrWhiteSpace(newAnswer))
            {
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = newAnswer } }
                });
            }
            else if (chatHistory.Count == 0)
            {
                // If it's the very first message and no answer, just prompt the model to start
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = "Hello, I am ready for the interview." } }
                });
            }

            var geminiRequest = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = contents
            };

            var jsonPayload = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error response from Gemini API: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"Gemini API request failed: {response.StatusCode}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidatesNode) && 
                    candidatesNode.ValueKind == JsonValueKind.Array && 
                    candidatesNode.GetArrayLength() > 0)
                {
                    var firstCandidate = candidatesNode[0];
                    if (firstCandidate.TryGetProperty("content", out var candidateContent) && 
                        candidateContent.TryGetProperty("parts", out var parts) && 
                        parts.ValueKind == JsonValueKind.Array && 
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? string.Empty;
                    }
                }

                return "I'm sorry, I couldn't process that. Could you please repeat or elaborate?";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while invoking Gemini API for Live Interview.");
                return "We are experiencing technical difficulties with the interview system. Please try again later.";
            }
        }
    }
}
