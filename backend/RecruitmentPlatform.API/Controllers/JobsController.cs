using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;
using RecruitmentPlatform.API.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IAiService _aiService;

        public JobsController(
            AppDbContext context, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            IAiService aiService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _aiService = aiService;
        }

        // GET /api/jobs — List all active jobs (public)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _context.JobPostings.Include(j => j.Recruiter)
                .Where(j => j.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(j =>
                    EF.Functions.Like(j.Title, $"%{search}%") ||
                    EF.Functions.Like(j.RequiredSkills, $"%{search}%"));
            }

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var response = jobs.Select(job => new JobResponseDto
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                RecruiterName = job.Recruiter?.FirstName + " " + job.Recruiter?.LastName,
                RecruiterId = job.RecruiterId,
                CreatedAt = job.CreatedAt,
                IsActive = job.IsActive
            });

            return Ok(response);
        }

        // GET /api/jobs/recommendations — Get AI-powered job recommendations (Candidate only)
        [HttpGet("recommendations")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetRecommendations()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token." });

            var candidate = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (candidate == null)
                return NotFound(new { message = "Candidate not found." });

            if (string.IsNullOrWhiteSpace(candidate.Skills))
            {
                return Ok(new List<JobRecommendationDto>());
            }

            var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive)
                .ToListAsync();

            if (!activeJobs.Any())
            {
                return Ok(new List<JobRecommendationDto>());
            }

            // Use pure AI service engine to filter and score recommendations
            var aiRecommendations = await _aiService.GetJobRecommendationsAsync(candidate, activeJobs);

            var recommendations = new List<JobRecommendationDto>();

            foreach (var aiRec in aiRecommendations)
            {
                var job = activeJobs.FirstOrDefault(j => j.Id == aiRec.JobId);
                if (job == null) continue;

                var candidateSkills = candidate.Skills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLowerInvariant())
                    .ToList();

                var originalMatchingSkills = job.RequiredSkills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => candidateSkills.Contains(s.ToLowerInvariant()))
                    .ToList();

                recommendations.Add(new JobRecommendationDto
                {
                    Id = job.Id,
                    Title = job.Title,
                    Description = job.Description,
                    RequiredSkills = job.RequiredSkills,
                    RecruiterName = job.Recruiter != null ? (job.Recruiter.FirstName + " " + job.Recruiter.LastName) : "Unknown Recruiter",
                    CreatedAt = job.CreatedAt,
                    MatchScore = aiRec.MatchScore,
                    MatchingSkills = originalMatchingSkills,
                    RecommendationReason = aiRec.RecommendationReason
                });
            }

            recommendations = recommendations.OrderByDescending(r => r.MatchScore).ToList();
            return Ok(recommendations);
        }

        // GET /api/jobs/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var job = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
                return NotFound(new { message = "Job not found." });

            return Ok(new JobResponseDto
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                RecruiterName = job.Recruiter?.FirstName + " " + job.Recruiter?.LastName,
                RecruiterId = job.RecruiterId,
                CreatedAt = job.CreatedAt,
                IsActive = job.IsActive
            });
        }

        // POST /api/jobs — Create job (Recruiter only)
        [HttpPost]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Create(CreateJobDto dto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token." });

            var job = new JobPosting
            {
                Title = dto.Title,
                Description = dto.Description,
                RequiredSkills = dto.RequiredSkills,
                RecruiterId = userId
            };

            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job posted successfully.", id = job.Id });
        }

        // GET /api/jobs/my — Get recruiter's own jobs
        [HttpGet("my")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetMyJobs()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var jobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.RecruiterId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var response = jobs.Select(job => new JobResponseDto
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                RecruiterName = job.Recruiter?.FirstName + " " + job.Recruiter?.LastName,
                RecruiterId = job.RecruiterId,
                CreatedAt = job.CreatedAt,
                IsActive = job.IsActive
            }).ToList();

            return Ok(response);
        }

        // DELETE /api/jobs/{id} — Delete job (owner only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == id && j.RecruiterId == userId);

            if (job == null)
                return NotFound(new { message = "Job not found or not yours." });

            _context.JobPostings.Remove(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job deleted." });
        }

        // POST /api/jobs/generate-description — Generate JD with AI (Recruiter only)
        [HttpPost("generate-description")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateJobDescriptionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.RequiredSkills))
            {
                return BadRequest(new { message = "Title and Required Skills are mandatory for generation." });
            }

            try
            {
                var description = await _aiService.GenerateJobDescriptionAsync(request.Title, request.RequiredSkills, request.AdditionalContext);
                return Ok(new { description });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
