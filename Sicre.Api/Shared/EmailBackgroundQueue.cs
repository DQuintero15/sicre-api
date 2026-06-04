using System.Threading.Channels;

namespace Sicre.Api.Shared;

public interface IEmailBackgroundQueue
{
    void Enqueue(EmailNotificationJob job);
    Task<EmailNotificationJob> DequeueAsync(CancellationToken ct);
}

public class EmailBackgroundQueue : IEmailBackgroundQueue
{
    private readonly Channel<EmailNotificationJob> _channel =
        Channel.CreateUnbounded<EmailNotificationJob>();

    public void Enqueue(EmailNotificationJob job) => _channel.Writer.TryWrite(job);

    public async Task<EmailNotificationJob> DequeueAsync(CancellationToken ct) =>
        await _channel.Reader.ReadAsync(ct);
}

public class EmailNotificationJob
{
    public required string Email { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
}
