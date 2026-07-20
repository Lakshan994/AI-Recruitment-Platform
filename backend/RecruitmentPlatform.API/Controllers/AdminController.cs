using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var response = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            });
            return Ok(response);
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateRole(string id, UpdateRoleDto dto)
        {
            var validRoles = new[] { "Candidate", "Recruiter", "HiringManager", "Admin" };
            if (!validRoles.Contains(dto.Role))
                return BadRequest(new { message = "Invalid role." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User role updated to {dto.Role}." });
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var users = await _context.Users.ToListAsync();
            var jobs = await _context.JobPostings.ToListAsync();
            var apps = await _context.Applications.ToListAsync();

            var stats = new AdminAnalyticsDto
            {
                TotalUsers = users.Count,
                TotalCandidates = users.Count(u => u.Role == "Candidate"),
                TotalRecruiters = users.Count(u => u.Role == "Recruiter"),
                TotalHiringManagers = users.Count(u => u.Role == "HiringManager"),
                TotalAdmins = users.Count(u => u.Role == "Admin"),

                TotalJobs = jobs.Count,
                ActiveJobs = jobs.Count(j => j.IsActive),

                TotalApplications = apps.Count,
                ApplicationsApplied = apps.Count(a => a.Status == "Applied"),
                ApplicationsShortlisted = apps.Count(a => a.Status == "Shortlisted"),
                ApplicationsInterviewed = apps.Count(a => a.Status == "Interviewed"),
                ApplicationsHired = apps.Count(a => a.Status == "Hired"),
                ApplicationsRejected = apps.Count(a => a.Status == "Rejected")
            };

            return Ok(stats);
        }
    }
}
