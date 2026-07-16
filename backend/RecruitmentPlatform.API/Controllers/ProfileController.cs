using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using System.Security.Claims;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public ProfileController(MongoDbContext context)
        {
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

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
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found." });

            var update = Builders<Models.User>.Update
                .Set(u => u.Bio, dto.Bio)
                .Set(u => u.Skills, dto.Skills)
                .Set(u => u.Experience, dto.Experience)
                .Set(u => u.Education, dto.Education);

            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPost("upload-resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var userId = GetUserId();
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

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

            var update = Builders<Models.User>.Update.Set(u => u.ResumeUrl, resumeUrl);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

            return Ok(new { message = "Resume uploaded successfully.", resumeUrl });
        }
    }
}
