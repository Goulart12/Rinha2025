using System.Collections.Concurrent;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Services;

public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(0);

    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
            
        _queue.Enqueue(item);
        _semaphore.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var item);
        
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        return item;
    }
}
