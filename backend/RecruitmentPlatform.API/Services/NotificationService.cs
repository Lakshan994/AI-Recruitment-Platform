using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.Interfaces;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public NotificationService(
            AppDbContext context,
            IEmailService emailService,
            ISmsService smsService)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<bool> SendEmailNotificationAsync(
            string candidateId,
            string email,
            string subject,
            string message)
        {
            bool success = await _emailService.SendEmailAsync(email, subject, message);

            var notification = new Notification
            {
                CandidateId = candidateId,
                Type = "Email",
                Subject = subject,
                Message = message,
                Status = success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return success;
        }

        public async Task<bool> SendSmsNotificationAsync(
            string candidateId,
            string phoneNumber,
            string message)
        {
            bool success = await _smsService.SendSmsAsync(phoneNumber, message);

            var notification = new Notification
            {
                CandidateId = candidateId,
                Type = "SMS",
                Subject = "SMS Notification",
                Message = message,
                Status = success ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return success;
        }

        public async Task<bool> SendApplicationStatusUpdateAsync(
            string applicationId,
            string status)
        {
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.JobPosting)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                return false;

            string subject = "Application Status Update";

            string message =
                $"Dear {application.Candidate!.FirstName} {application.Candidate.LastName},<br/><br/>" +
                $"Your application for <b>{application.JobPosting!.Title}</b> has been updated.<br/><br/>" +
                $"Current Status: <b>{status}</b><br/><br/>" +
                $"Thank you for using AI Recruitment Platform.";

            bool emailSent = await SendEmailNotificationAsync(
                application.CandidateId,
                application.Candidate.Email,
                subject,
                message);

            if (!string.IsNullOrWhiteSpace(application.Candidate.PhoneNumber))
            {
                string smsMessage = $"AI Recruitment Platform: Your application status for '{application.JobPosting.Title}' is now '{status}'.";
                await SendSmsNotificationAsync(application.CandidateId, application.Candidate.PhoneNumber, smsMessage);
            }

            return emailSent;
        }

        public async Task<bool> SendInterviewReminderAsync(string interviewId)
        {
            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.JobPosting)
                .FirstOrDefaultAsync(i => i.Id == interviewId);

            if (interview == null)
                return false;

            string subject = "Interview Reminder";

            string message =
                $"Dear {interview.Candidate!.FirstName} {interview.Candidate.LastName},<br/><br/>" +
                $"This is a reminder for your interview.<br/><br/>" +
                $"Job: <b>{interview.JobPosting!.Title}</b><br/>" +
                $"Date: <b>{interview.InterviewDate:dddd, dd MMMM yyyy}</b><br/>" +
                $"Time: <b>{interview.InterviewDate:hh:mm tt}</b><br/>" +
                $"Location: {interview.Location}<br/>" +
                $"Meeting Link: {interview.MeetingLink}<br/><br/>" +
                $"Best of luck!";

            bool emailSent = await SendEmailNotificationAsync(
                interview.CandidateId,
                interview.Candidate.Email,
                subject,
                message);

            if (!string.IsNullOrWhiteSpace(interview.Candidate.PhoneNumber))
            {
                string smsMessage = $"AI Recruitment: Interview reminder for '{interview.JobPosting.Title}' on {interview.InterviewDate:dddd, dd MMMM yyyy} at {interview.InterviewDate:hh:mm tt}. Location: {interview.Location}. Link: {interview.MeetingLink}";
                await SendSmsNotificationAsync(interview.CandidateId, interview.Candidate.PhoneNumber, smsMessage);
            }

            return emailSent;
        }
    }
}