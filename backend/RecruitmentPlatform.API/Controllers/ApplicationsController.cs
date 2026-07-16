using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        // POST /api/applications — Apply for a job (Candidate)
        [HttpPost]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Apply(ApplyDto dto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token." });

            // Check if job exists
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == dto.JobPostingId && j.IsActive);

            if (job == null)
                return NotFound(new { message = "Job not found or inactive." });

            // Check if already applied
            var existing = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobPostingId == dto.JobPostingId && a.CandidateId == userId);

            if (existing != null)
                return BadRequest(new { message = "You have already applied for this job." });

            // Fetch Candidate to get ResumeUrl
            var candidate = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (candidate == null)
                return NotFound(new { message = "Candidate profile not found." });

            if (string.IsNullOrEmpty(candidate.ResumeUrl))
                return BadRequest(new { message = "You must upload a resume to your profile before applying." });

            // Generate a simple AI match score (random for now, would use real AI in production)
            var random = new Random();
            var matchScore = Math.Round((decimal)(random.NextDouble() * 40 + 60), 2); // Score between 60-100

            var application = new Application
            {
                JobPostingId = dto.JobPostingId,
                CandidateId = userId,
                ResumeUrl = candidate.ResumeUrl,
                AiMatchScore = matchScore
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Application submitted successfully.", matchScore });
        }

        // GET /api/applications/my — Get candidate's own applications
        [HttpGet("my")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var applications = await _context.Applications
                .Include(a => a.JobPosting)
                .Where(a => a.CandidateId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var response = applications.Select(app => new ApplicationResponseDto
            {
                Id = app.Id,
                JobPostingId = app.JobPostingId,
                JobTitle = app.JobPosting?.Title ?? "Deleted Job",
                CandidateId = app.CandidateId,
                ResumeUrl = app.ResumeUrl,
                Status = app.Status,
                AiMatchScore = app.AiMatchScore,
                AppliedAt = app.AppliedAt
            }).ToList();

            return Ok(response);
        }

        // GET /api/applications/job/{jobId} — Get applications for a job (Recruiter)
        [HttpGet("job/{jobId}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetByJob(string jobId)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify this job belongs to the recruiter
            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == jobId && j.RecruiterId == userId);

            if (job == null)
                return NotFound(new { message = "Job not found or not yours." });

            var applications = await _context.Applications
                .Include(a => a.Candidate)
                .Where(a => a.JobPostingId == jobId)
                .OrderByDescending(a => a.AiMatchScore)
                .ToListAsync();

            var response = applications.Select(app => new ApplicationResponseDto
            {
                Id = app.Id,
                JobPostingId = app.JobPostingId,
                JobTitle = job.Title,
                CandidateId = app.CandidateId,
                CandidateName = (app.Candidate?.FirstName + " " + app.Candidate?.LastName).Trim(),
                CandidateEmail = app.Candidate?.Email ?? "",
                ResumeUrl = app.ResumeUrl,
                Status = app.Status,
                AiMatchScore = app.AiMatchScore,
                AppliedAt = app.AppliedAt
            }).ToList();

            return Ok(response);
        }

        // PUT /api/applications/{id}/status — Update application status (Recruiter)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> UpdateStatus(string id, UpdateStatusDto dto)
        {
            var validStatuses = new[] { "Applied", "Shortlisted", "Interviewed", "Rejected", "Hired" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { message = "Invalid status." });

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { message = "Application not found." });

            // Verify the recruiter owns the job
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == application.JobPostingId && j.RecruiterId == userId);

            if (job == null)
                return Forbid();

            application.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Status updated to {dto.Status}." });
        }

        // GET /api/applications/shortlisted — Get shortlisted applications for Hiring Manager
        [HttpGet("shortlisted")]
        [Authorize(Roles = "HiringManager")]
        public async Task<IActionResult> GetShortlisted()
        {
            var applications = await _context.Applications
                .Include(a => a.JobPosting)
                .Include(a => a.Candidate)
                .Where(a => a.Status == "Shortlisted" || a.Status == "Interviewed")
                .OrderByDescending(a => a.AiMatchScore)
                .ToListAsync();

            var response = applications.Select(app => new ApplicationResponseDto
            {
                Id = app.Id,
                JobPostingId = app.JobPostingId,
                JobTitle = app.JobPosting?.Title ?? "Deleted Job",
                CandidateId = app.CandidateId,
                CandidateName = (app.Candidate?.FirstName + " " + app.Candidate?.LastName).Trim(),
                CandidateEmail = app.Candidate?.Email ?? "",
                ResumeUrl = app.ResumeUrl,
                Status = app.Status,
                AiMatchScore = app.AiMatchScore,
                AppliedAt = app.AppliedAt
            }).ToList();

            return Ok(response);
        }

        // PUT /api/applications/{id}/hiring-decision — Make hiring decision (HiringManager)
        [HttpPut("{id}/hiring-decision")]
        [Authorize(Roles = "HiringManager")]
        public async Task<IActionResult> HiringDecision(string id, UpdateStatusDto dto)
        {
            var validStatuses = new[] { "Interviewed", "Hired", "Rejected" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { message = "Invalid hiring decision status." });

            var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == id);
            if (application == null)
                return NotFound(new { message = "Application not found." });

            application.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Candidate {dto.Status.ToLower()} successfully." });
        }

        // GET /api/applications/stats — Dashboard stats
        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue("role");

            if (role == "Recruiter")
            {
                var myJobs = await _context.JobPostings
                    .Where(j => j.RecruiterId == userId)
                    .ToListAsync();
                var jobIds = myJobs.Select(j => j.Id).ToList();

                var applications = await _context.Applications
                    .Where(a => jobIds.Contains(a.JobPostingId))
                    .ToListAsync();

                return Ok(new DashboardStatsDto
                {
                    TotalJobs = myJobs.Count(j => j.IsActive),
                    TotalApplications = applications.Count,
                    Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                    Interviewed = applications.Count(a => a.Status == "Interviewed"),
                    Hired = applications.Count(a => a.Status == "Hired"),
                    Rejected = applications.Count(a => a.Status == "Rejected"),
                    AvgMatchScore = applications.Count > 0 ? Math.Round(applications.Average(a => a.AiMatchScore), 1) : 0
                });
            }
            else if (role == "HiringManager")
            {
                var applications = await _context.Applications.ToListAsync();
                return Ok(new DashboardStatsDto
                {
                    TotalJobs = await _context.JobPostings.CountAsync(j => j.IsActive),
                    TotalApplications = applications.Count,
                    Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                    Interviewed = applications.Count(a => a.Status == "Interviewed"),
                    Hired = applications.Count(a => a.Status == "Hired"),
                    Rejected = applications.Count(a => a.Status == "Rejected"),
                    AvgMatchScore = applications.Count > 0 ? Math.Round(applications.Average(a => a.AiMatchScore), 1) : 0
                });
            }
            else
            {
                var applications = await _context.Applications
                    .Where(a => a.CandidateId == userId)
                    .ToListAsync();

                return Ok(new DashboardStatsDto
                {
                    TotalJobs = 0,
                    TotalApplications = applications.Count,
                    Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                    Interviewed = applications.Count(a => a.Status == "Interviewed"),
                    Hired = applications.Count(a => a.Status == "Hired"),
                    Rejected = applications.Count(a => a.Status == "Rejected"),
                    AvgMatchScore = applications.Count > 0 ? Math.Round(applications.Average(a => a.AiMatchScore), 1) : 0
                });
            }
        }
    }
}