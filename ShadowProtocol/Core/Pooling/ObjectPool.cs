namespace ShadowProtocol.Core.Pooling;

public interface IPoolable
{
    bool IsActive { get; }
    void OnSpawn();
    void OnDespawn();
}

public interface IObjectPool<T> where T : class, IPoolable
{
    int TotalCount { get; }
    int ActiveCount { get; }
    int InactiveCount { get; }
    T Rent();
    void Return(T item);
    void Prewarm(int count);
    void Clear();
}

public class ObjectPool<T> : IObjectPool<T> where T : class, IPoolable
{
    private readonly Func<T> _factory;
    private readonly Stack<T> _pool = new();
    private readonly HashSet<T> _active = new();
    private readonly object _lock = new();

    public int TotalCount
    {
        get
        {
            lock (_lock) return _pool.Count + _active.Count;
        }
    }

    public int ActiveCount
    {
        get
        {
            lock (_lock) return _active.Count;
        }
    }

    public int InactiveCount
    {
        get
        {
            lock (_lock) return _pool.Count;
        }
    }

    public ObjectPool(Func<T> factory, int initialCapacity = 0)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (initialCapacity > 0)
        {
            Prewarm(initialCapacity);
        }
    }

    public void Prewarm(int count)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                var item = _factory();
                _pool.Push(item);
            }
        }
    }

    public T Rent()
    {
        T item;
        lock (_lock)
        {
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
            }
            else
            {
                item = _factory();
            }
            _active.Add(item);
        }

        item.OnSpawn();
        return item;
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_lock)
        {
            if (_active.Remove(item))
            {
                item.OnDespawn();
                _pool.Push(item);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _pool.Clear();
            _active.Clear();
        }
    }
}
