using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Interfaces;
using RecruitmentPlatform.API.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommunicationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;

        public CommunicationController(INotificationService notificationService, AppDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        // GET /api/communication
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var list = await _context.Notifications
                .Include(n => n.Candidate)
                .OrderByDescending(n => n.SentAt)
                .Select(n => new
                {
                    n.Id,
                    n.CandidateId,
                    CandidateName = n.Candidate != null ? (n.Candidate.FirstName + " " + n.Candidate.LastName) : "Unknown Candidate",
                    n.Type,
                    n.Subject,
                    n.Message,
                    n.Status,
                    n.SentAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // Send Email
        [HttpPost("email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _notificationService.SendEmailNotificationAsync(
                request.CandidateId,
                request.ToEmail,
                request.Subject,
                request.Body);

            if (!result)
                return BadRequest(new
                {
                    Success = false,
                    Message = "Email could not be sent."
                });

            return Ok(new
            {
                Success = true,
                Message = "Email sent successfully."
            });
        }

        // Send SMS
        [HttpPost("sms")]
        public async Task<IActionResult> SendSms([FromBody] SmsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _notificationService.SendSmsNotificationAsync(
                request.CandidateId,
                request.PhoneNumber,
                request.Message);

            if (!result)
                return BadRequest(new
                {
                    Success = false,
                    Message = "SMS could not be sent."
                });

            return Ok(new
            {
                Success = true,
                Message = "SMS sent successfully."
            });
        }

        // Application Status Update
        [HttpPost("application-status/{applicationId}")]
        public async Task<IActionResult> SendApplicationStatus(
            string applicationId,
            [FromQuery] string status)
        {
            var result = await _notificationService
                .SendApplicationStatusUpdateAsync(applicationId, status);

            if (!result)
                return BadRequest(new
                {
                    Success = false,
                    Message = "Application status notification failed."
                });

            return Ok(new
            {
                Success = true,
                Message = "Application status notification sent."
            });
        }

        // Interview Reminder
        [HttpPost("interview-reminder/{interviewId}")]
        public async Task<IActionResult> SendInterviewReminder(string interviewId)
        {
            var result = await _notificationService
                .SendInterviewReminderAsync(interviewId);

            if (!result)
                return BadRequest(new
                {
                    Success = false,
                    Message = "Interview reminder failed."
                });

            return Ok(new
            {
                Success = true,
                Message = "Interview reminder sent."
            });
        }
    }
}