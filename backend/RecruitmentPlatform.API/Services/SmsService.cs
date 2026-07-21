using Microsoft.Extensions.Configuration;
using RecruitmentPlatform.API.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace RecruitmentPlatform.API.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _configuration["Twilio:AccountSid"];
                var authToken = _configuration["Twilio:AuthToken"];
                var fromNumber = _configuration["Twilio:PhoneNumber"];

                TwilioClient.Init(accountSid, authToken);

                await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(fromNumber),
                    to: new Twilio.Types.PhoneNumber(phoneNumber)
                );

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}