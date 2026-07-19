using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs;
using RecruitmentPlatform.API.Interfaces;
using RecruitmentPlatform.API.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InterviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public InterviewsController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET /api/interviews/job/{jobId} — Get all interviews for a job
        [HttpGet("job/{jobId}")]
        [Authorize(Roles = "Recruiter,HiringManager")]
        public async Task<IActionResult> GetByJob(string jobId)
        {
            var interviews = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.JobPosting)
                .Where(i => i.JobPostingId == jobId)
                .OrderBy(i => i.InterviewDate)
                .ToListAsync();

            var response = interviews.Select(i => new InterviewResponseDto
            {
                Id = i.Id,
                CandidateId = i.CandidateId,
                CandidateName = i.Candidate != null ? $"{i.Candidate.FirstName} {i.Candidate.LastName}" : "Unknown",
                CandidateEmail = i.Candidate?.Email ?? string.Empty,
                JobTitle = i.JobPosting?.Title ?? "Unknown Job",
                InterviewDate = i.InterviewDate,
                Location = i.Location,
                MeetingLink = i.MeetingLink,
                Status = i.Status
            });

            return Ok(response);
        }

        // POST /api/interviews — Schedule an interview
        [HttpPost]
        [Authorize(Roles = "Recruiter,HiringManager")]
        public async Task<IActionResult> Create(CreateInterviewDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid data." });

            var candidate = await _context.Users.FindAsync(dto.CandidateId);
            if (candidate == null)
                return NotFound(new { message = "Candidate not found." });

            var job = await _context.JobPostings.FindAsync(dto.JobPostingId);
            if (job == null)
                return NotFound(new { message = "Job posting not found." });

            // Create Interview
            var interview = new Interview
            {
                CandidateId = dto.CandidateId,
                JobPostingId = dto.JobPostingId,
                InterviewDate = dto.InterviewDate,
                Location = dto.Location,
                MeetingLink = dto.MeetingLink,
                Status = "Scheduled",
                ReminderSent = false
            };

            _context.Interviews.Add(interview);

            // Automatically update applicant status to 'Interviewed' if they applied
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobPostingId == dto.JobPostingId && a.CandidateId == dto.CandidateId);

            if (application != null)
            {
                application.Status = "Interviewed";
            }

            await _context.SaveChangesAsync();

            // Trigger notification service
            try
            {
                await _notificationService.SendInterviewReminderAsync(interview.Id);
            }
            catch
            {
                // Silently ignore notification failure so it doesn't block interview creation
            }

            return Ok(new { message = "Interview scheduled successfully.", id = interview.Id });
        }

        // PUT /api/interviews/{id}/status — Update interview status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Recruiter,HiringManager")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateInterviewStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Invalid status." });

            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
                return NotFound(new { message = "Interview not found." });

            var validStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { message = $"Status must be one of: {string.Join(", ", validStatuses)}" });

            interview.Status = dto.Status;

            // Also synchronize application status if cancelled
            if (dto.Status == "Cancelled")
            {
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.JobPostingId == interview.JobPostingId && a.CandidateId == interview.CandidateId);

                if (application != null && application.Status == "Interviewed")
                {
                    application.Status = "Shortlisted"; // Revert back to Shortlisted if interview is cancelled
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Interview status updated successfully." });
        }

        // GET /api/interviews/my — Get all interviews for logged-in candidate
        [HttpGet("my")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyInterviews()
        {
            var userId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid token." });

            var interviews = await _context.Interviews
                .Include(i => i.JobPosting)
                .Where(i => i.CandidateId == userId)
                .OrderBy(i => i.InterviewDate)
                .ToListAsync();

            var response = interviews.Select(i => new
            {
                i.Id,
                i.CandidateId,
                JobTitle = i.JobPosting?.Title ?? "Unknown Job",
                i.JobPostingId,
                i.InterviewDate,
                i.Location,
                i.MeetingLink,
                i.Status
            });

            return Ok(response);
        }

        // GET /api/interviews/{id}/google-calendar — Get Google Calendar quick add URL
        [HttpGet("{id}/google-calendar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGoogleCalendarLink(string id)
        {
            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.JobPosting)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
                return NotFound(new { message = "Interview not found." });

            var title = Uri.EscapeDataString($"Interview for {interview.JobPosting?.Title ?? "Position"}");
            var start = interview.InterviewDate.ToString("yyyyMMddTHHmmssZ");
            var end = interview.InterviewDate.AddHours(1).ToString("yyyyMMddTHHmmssZ");
            var details = Uri.EscapeDataString($"Interview scheduled for {interview.Candidate?.FirstName} {interview.Candidate?.LastName}. Meeting Link: {interview.MeetingLink}");
            var location = Uri.EscapeDataString(interview.Location);

            var googleUrl = $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={title}&dates={start}/{end}&details={details}&location={location}";
            return Ok(new { url = googleUrl });
        }

        // GET /api/interviews/{id}/outlook-calendar — Get Outlook Calendar quick add URL
        [HttpGet("{id}/outlook-calendar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOutlookCalendarLink(string id)
        {
            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.JobPosting)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
                return NotFound(new { message = "Interview not found." });

            var title = Uri.EscapeDataString($"Interview for {interview.JobPosting?.Title ?? "Position"}");
            var start = interview.InterviewDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var end = interview.InterviewDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var details = Uri.EscapeDataString($"Interview scheduled for {interview.Candidate?.FirstName} {interview.Candidate?.LastName}. Meeting Link: {interview.MeetingLink}");
            var location = Uri.EscapeDataString(interview.Location);

            var outlookUrl = $"https://outlook.live.com/calendar/0/deeplink/compose?path=/calendar/action/compose&rru=addevent&subject={title}&startdt={start}&enddt={end}&body={details}&location={location}";
            return Ok(new { url = outlookUrl });
        }

        // GET /api/interviews/{id}/ics — Download iCalendar (.ics) file
        [HttpGet("{id}/ics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetIcsFile(string id)
        {
            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.JobPosting)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
                return NotFound(new { message = "Interview not found." });

            var start = interview.InterviewDate.ToString("yyyyMMddTHHmmssZ");
            var end = interview.InterviewDate.AddHours(1).ToString("yyyyMMddTHHmmssZ");
            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//AI Recruitment Platform//NONSGML v1.0//EN");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{interview.Id}");
            sb.AppendLine($"DTSTAMP:{stamp}");
            sb.AppendLine($"DTSTART:{start}");
            sb.AppendLine($"DTEND:{end}");
            sb.AppendLine($"SUMMARY:Interview for {interview.JobPosting?.Title ?? "Position"}");
            sb.AppendLine($"DESCRIPTION:Interview scheduled for {interview.Candidate?.FirstName} {interview.Candidate?.LastName}. Meeting Link: {interview.MeetingLink}");
            sb.AppendLine($"LOCATION:{interview.Location}");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/calendar", $"interview_{interview.Id}.ics");
        }
    }
}
