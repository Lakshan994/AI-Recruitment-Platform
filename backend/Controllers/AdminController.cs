using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public AdminController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.Find(_ => true).SortByDescending(u => u.CreatedAt).ToListAsync();
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

            var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(new { message = "User not found." });

            var update = Builders<User>.Update.Set(u => u.Role, dto.Role);
            await _context.Users.UpdateOneAsync(u => u.Id == id, update);

            return Ok(new { message = $"User role updated to {dto.Role}." });
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var users = await _context.Users.Find(_ => true).ToListAsync();
            var jobs = await _context.JobPostings.Find(_ => true).ToListAsync();
            var apps = await _context.Applications.Find(_ => true).ToListAsync();

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
