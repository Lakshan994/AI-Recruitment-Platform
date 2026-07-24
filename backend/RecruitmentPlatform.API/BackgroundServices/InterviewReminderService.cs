using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.Interfaces;

namespace RecruitmentPlatform.API.BackgroundServices
{
    public class InterviewReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InterviewReminderService> _logger;

        public InterviewReminderService(
            IServiceProvider serviceProvider,
            ILogger<InterviewReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var notificationService =
                        scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var interviews = await context.Interviews
                        .Where(i =>
                            !i.ReminderSent &&
                            i.InterviewDate <= DateTime.Now.AddHours(24) &&
                            i.InterviewDate > DateTime.Now)
                        .ToListAsync(stoppingToken);

                    foreach (var interview in interviews)
                    {
                        bool sent = await notificationService
                            .SendInterviewReminderAsync(interview.Id);

                        if (sent)
                        {
                            interview.ReminderSent = true;
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error occurred while sending interview reminders.");
                }

                // Wait 1 hour before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}