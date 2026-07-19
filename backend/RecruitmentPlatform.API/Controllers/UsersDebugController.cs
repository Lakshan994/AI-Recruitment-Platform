using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.API.Data;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersDebugController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersDebugController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var users = _context.Users.ToList();
            return Ok(users.Select(u => new { u.Email, u.Role, u.FirstName }));
        }
    }
}
