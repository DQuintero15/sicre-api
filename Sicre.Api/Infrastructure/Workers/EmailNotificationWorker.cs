using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Infrastructure.Workers;

public class EmailNotificationWorker : BackgroundService
{
    private readonly IEmailBackgroundQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(
        IEmailBackgroundQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailNotificationWorker> logger
    )
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.SendEmailAsync(job.Email, job.Subject, job.Body);

                _logger.LogInformation(
                    "Email enviado a {Email} - {Subject}",
                    job.Email,
                    job.Subject
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email en EmailNotificationWorker");
            }
        }
    }
}
