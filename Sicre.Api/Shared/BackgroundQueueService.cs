using System.Threading.Channels;

namespace Sicre.Api.Shared;

public interface IBackgroundQueueService
{
    void Enqueue(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundQueueService : IBackgroundQueueService
{
    private readonly Channel<Func<CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<CancellationToken, Task>>();

    public void Enqueue(Func<CancellationToken, Task> workItem) =>
        _channel.Writer.TryWrite(workItem);

    public async Task<Func<CancellationToken, Task>> DequeueAsync(
        CancellationToken cancellationToken
    ) => await _channel.Reader.ReadAsync(cancellationToken);
}
