using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;
using RecruitmentPlatform.API.Interfaces;
using System.Security.Claims;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAiService _aiService;

        public ProfileController(AppDbContext context, IAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new ProfileResponseDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                Skills = user.Skills,
                Experience = user.Experience,
                Education = user.Education,
                ResumeUrl = user.ResumeUrl
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.Bio = dto.Bio;
            user.Skills = dto.Skills;
            user.Experience = dto.Experience;
            user.Education = dto.Education;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPost("upload-resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = HttpContext.Request;
            var resumeUrl = $"{request.Scheme}://{request.Host}/uploads/{fileName}";

            user.ResumeUrl = resumeUrl;

            // Extract text and call Gemini parsing service
            var ext = Path.GetExtension(filePath).ToLower();
            var resumeText = string.Empty;

            try
            {
                if (ext == ".pdf")
                {
                    using (var pdf = PdfDocument.Open(filePath))
                    {
                        var textBuilder = new StringBuilder();
                        foreach (var page in pdf.GetPages())
                        {
                            textBuilder.AppendLine(page.Text);
                        }
                        resumeText = textBuilder.ToString();
                    }
                }
                else if (ext == ".txt")
                {
                    resumeText = await System.IO.File.ReadAllTextAsync(filePath);
                }

                if (!string.IsNullOrWhiteSpace(resumeText))
                {
                    var parsed = await _aiService.ParseResumeAsync(resumeText);
                    if (parsed != null)
                    {
                        user.Bio = parsed.Bio;
                        user.Skills = parsed.Skills;
                        user.Experience = parsed.Experience;
                        user.Education = parsed.Education;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception but still allow successful file upload
                System.Diagnostics.Debug.WriteLine($"AI Parsing Error: {ex.Message}");
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Resume uploaded and analyzed by AI successfully.",
                resumeUrl,
                parsedProfile = new
                {
                    bio = user.Bio,
                    skills = user.Skills,
                    experience = user.Experience,
                    education = user.Education
                }
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Audit logging before delete
            _context.RecruitmentAnalytics.Add(new RecruitmentAnalytic
            {
                EventType = "DataPrivacyDeletion",
                Description = $"User {userId} requested permanent account deletion under privacy regulations.",
                RecordedAt = DateTime.UtcNow
            });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account permanently deleted in compliance with GDPR and data privacy regulations." });
        }
    }

}

