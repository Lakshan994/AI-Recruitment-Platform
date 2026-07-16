using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RecruitmentPlatform.API.Data;
using System.Linq;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersDebugController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public UsersDebugController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var users = _context.Users.Find(_ => true).ToList();
            return Ok(users.Select(u => new { u.Email, u.Role, u.FirstName }));
        }
    }
}
