using Sicre.Api.Shared;

namespace Sicre.Api.Infrastructure.Workers;

public class DriveUploadWorker(
    IBackgroundQueueService queue,
    ILogger<DriveUploadWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var task = await queue.DequeueAsync(stoppingToken);
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await task(stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error subiendo archivo a Drive. Intento {Attempt}/{Max}.",
                        attempt,
                        maxRetries
                    );

                    if (attempt == maxRetries)
                        logger.LogCritical("Falló definitivamente la subida del archivo.");

                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
    }
}
