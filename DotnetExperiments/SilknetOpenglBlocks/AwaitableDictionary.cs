namespace SilknetOpenglBlocks;

/// <summary>
/// A thread safe dictionary which, if getting a non-existent value is attempted, waits for the value to be set and
/// then returns it. Doesn't allow setting a value more than once.
/// </summary>
public sealed class AwaitableDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Lock _lock = new();
    
    private readonly Dictionary<TKey, TaskCompletionSource<TValue>> _taskCompletionSources = [];
    
    public TValue this[TKey key]
    {
        get => GetAsync(key).Result;
        set
        {
            lock (_lock)
            {
                TaskCompletionSource<TValue> tcs = GetOrCreateTaskCompletionSource(key);
                if (tcs.Task.IsCompleted)
                {
                    throw new InvalidOperationException($"Attempted to add value {value} by " +
                        $"the key {key}, but the key already has a value {tcs.Task.Result}.");
                }
                tcs.SetResult(value);
            }
        }
    }
    
    private TaskCompletionSource<TValue> GetOrCreateTaskCompletionSource(TKey key)
    {
        if (!_taskCompletionSources.TryGetValue(key, out TaskCompletionSource<TValue>? tcs))
        {
            tcs = new();
            _taskCompletionSources[key] = tcs;
        }
        return tcs;
    }
    
    public async Task<TValue> GetAsync(TKey key)
    {
        TaskCompletionSource<TValue> taskCompletionSource;
        lock (_lock)
        {
            taskCompletionSource = GetOrCreateTaskCompletionSource(key);
        }
        return await taskCompletionSource.Task;
    }
    
    public bool HasKey(TKey key)
    {
        lock (_lock)
        {
            return _taskCompletionSources.TryGetValue(key, out TaskCompletionSource<TValue>? tcs) &&
                tcs.Task.IsCompleted;
        }
    }
    
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_taskCompletionSources.TryGetValue(key, out TaskCompletionSource<TValue>? tcs) && !tcs.Task.IsCompleted)
                throw new InvalidOperationException("Can't remove a key for which a value is being awaited.");
            return _taskCompletionSources.Remove(key);
        }
    }
}