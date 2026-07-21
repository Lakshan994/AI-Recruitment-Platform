using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Interfaces
{
    public interface INotificationService
    {
        Task<bool> SendEmailNotificationAsync(
            string candidateId,
            string email,
            string subject,
            string message);

        Task<bool> SendSmsNotificationAsync(
            string candidateId,
            string phoneNumber,
            string message);

        Task<bool> SendApplicationStatusUpdateAsync(
            string applicationId,
            string status);

        Task<bool> SendInterviewReminderAsync(
            string interviewId);
    }
}