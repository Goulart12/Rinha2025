namespace RinhaDeBackend2025.Services.Interfaces;

public interface IBackgroundTaskQueue<T>
{
    void Enqueue(T item);
    Task<T> DequeueAsync(CancellationToken cancellationToken);
}
