using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;
        private readonly AppDbContext _context;

        public ChatController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatController> logger,
            AppDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API Key is not configured.");
                return StatusCode(500, "Chatbot is currently unavailable.");
            }

            // 1. Resolve logged-in user claims if present
            string? userId = null;
            string? userRole = null;
            string? firstName = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                firstName = User.FindFirst("FirstName")?.Value ?? User.Identity.Name;
            }

            // 2. Fetch live database context
            var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive)
                .OrderByDescending(j => j.CreatedAt)
                .Take(10) // Limit to 10 most recent jobs for prompt efficiency
                .ToListAsync();

            var dbContextInfo = new StringBuilder();
            dbContextInfo.AppendLine("Live Platform Database Context:");

            // Append available jobs
            dbContextInfo.AppendLine("--- ACTIVE JOB POSTINGS ---");
            if (activeJobs.Any())
            {
                foreach (var job in activeJobs)
                {
                    dbContextInfo.AppendLine($"- ID: {job.Id}, Title: '{job.Title}', Required Skills: '{job.RequiredSkills}', Recruiter: '{job.Recruiter?.FirstName ?? "System"}'");
                    dbContextInfo.AppendLine($"  Description: {job.Description}");
                }
            }
            else
            {
                dbContextInfo.AppendLine("No active job postings are currently open.");
            }

            // User-specific data context
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userRole))
            {
                dbContextInfo.AppendLine("\n--- CURRENT USER INFO ---");
                dbContextInfo.AppendLine($"Logged-in User: {firstName} (ID: {userId})");
                dbContextInfo.AppendLine($"Role: {userRole}");

                if (userRole == "Candidate")
                {
                    var profile = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    var apps = await _context.Applications
                        .Include(a => a.JobPosting)
                        .Where(a => a.CandidateId == userId)
                        .ToListAsync();

                    if (profile != null)
                    {
                        dbContextInfo.AppendLine($"Candidate Profile - Skills: '{profile.Skills}', Experience: '{profile.Experience}', Education: '{profile.Education}'");
                    }

                    dbContextInfo.AppendLine("Candidate's Applications:");
                    if (apps.Any())
                    {
                        foreach (var app in apps)
                        {
                            dbContextInfo.AppendLine($"- Job: '{app.JobPosting?.Title}', Applied: {app.AppliedAt:yyyy-MM-dd}, Status: {app.Status}, AI Match Score: {app.AiMatchScore}%");
                        }
                    }
                    else
                    {
                        dbContextInfo.AppendLine("User has not submitted any job applications yet.");
                    }
                }
                else if (userRole == "Recruiter")
                {
                    var recruiterJobs = await _context.JobPostings
                        .Where(j => j.RecruiterId == userId)
                        .Include(j => j.Applications)
                        .ToListAsync();

                    dbContextInfo.AppendLine("Recruiter's Active Jobs & Applicant Counts:");
                    if (recruiterJobs.Any())
                    {
                        foreach (var job in recruiterJobs)
                        {
                            dbContextInfo.AppendLine($"- '{job.Title}' (ID: {job.Id}) - Status: {(job.IsActive ? "Active" : "Inactive")} - Applicants: {job.Applications.Count}");
                        }
                    }
                    else
                    {
                        dbContextInfo.AppendLine("Recruiter has not posted any jobs yet.");
                    }
                }
                else if (userRole == "Admin")
                {
                    var totalCandidates = await _context.Users.CountAsync(u => u.Role == "Candidate");
                    var totalRecruiters = await _context.Users.CountAsync(u => u.Role == "Recruiter");
                    var totalJobs = await _context.JobPostings.CountAsync();
                    var totalApps = await _context.Applications.CountAsync();

                    dbContextInfo.AppendLine("System Analytics (Admin Only View):");
                    dbContextInfo.AppendLine($"- Total Registered Candidates: {totalCandidates}");
                    dbContextInfo.AppendLine($"- Total Registered Recruiters: {totalRecruiters}");
                    dbContextInfo.AppendLine($"- Total Job Postings: {totalJobs}");
                    dbContextInfo.AppendLine($"- Total Applications Submitted: {totalApps}");
                }
            }
            else
            {
                dbContextInfo.AppendLine("\n--- USER IDENTITY ---");
                dbContextInfo.AppendLine("Anonymous user. Not logged in.");
            }

            // 3. Compose rich prompt
            var systemPrompt = "You are a helpful and professional AI assistant for 'TalentAI', an AI-powered recruitment platform.\n" +
                "Your purpose is to answer user queries using the live platform data provided below.\n\n" +
                dbContextInfo.ToString() + "\n" +
                "GUIDELINES FOR YOUR RESPONSES:\n" +
                "1. If the user asks about jobs, recommend the ones from the active job list that match their queries/skills.\n" +
                "2. If the user is logged in, greet them by their name and answer personalized questions like 'What is the status of my applications?' using their application records.\n" +
                "3. Explain that AI Match Score is calculated using resume analysis matching job requirements (scores range between 60-100%).\n" +
                "4. Keep answers friendly, professional, and concise. Do not make up jobs or applications that are not in the provided database context.\n" +
                "5. If they are not logged in and ask personalized questions, politely invite them to sign in first.";

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

            var geminiRequest = new GeminiRequest
            {
                SystemInstruction = new GeminiSystemInstruction
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                },
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = new List<GeminiPart> { new GeminiPart { Text = request.Message } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(geminiRequest), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from Gemini API: {StatusCode} - {Error}", response.StatusCode, error);
                    return StatusCode(500, "Error generating response from AI.");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseString);

                var replyText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "I'm sorry, I couldn't understand that.";

                return Ok(new ChatResponse { Reply = replyText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API.");
                return StatusCode(500, "Internal server error while processing your request.");
            }
        }
    }
}
