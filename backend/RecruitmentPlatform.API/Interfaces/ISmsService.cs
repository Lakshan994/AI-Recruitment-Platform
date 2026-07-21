using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}