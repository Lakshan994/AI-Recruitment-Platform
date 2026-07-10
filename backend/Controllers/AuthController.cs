using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (existingUser != null)
                    return BadRequest(new { message = "Email is already in use." });

                var user = new User
                {
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Role = dto.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                // TEMP: surface the real error so we can diagnose it.
                // Remove this catch block (or the ex.Message/inner details) once things work.
                return StatusCode(500, new
                {
                    message = "Registration failed: " + ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized(new { message = "Invalid email or password." });

                var token = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    Role = user.Role,
                    FirstName = user.FirstName
                });
            }
            catch (Exception ex)
            {
                // TEMP: surface the real error so we can diagnose it.
                return StatusCode(500, new
                {
                    message = "Login failed: " + ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FirstName", user.FirstName)
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetMe()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
    }
}