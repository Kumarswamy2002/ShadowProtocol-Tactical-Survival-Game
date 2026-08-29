namespace ShadowProtocol.Core.Events;

public interface IEvent
{
    DateTime Timestamp { get; }
}

public interface IEventBus
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
    void Clear();
}

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock)
        {
            var type = typeof(TEvent);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }
        }
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        List<Delegate> targets;
        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                return;
            }
            targets = new List<Delegate>(list);
        }

        foreach (var subscriber in targets)
        {
            if (subscriber is Action<TEvent> action)
            {
                try
                {
                    action(@event);
                }
                catch (Exception ex)
                {
                    // Log or handle event handler fault safely
                    Console.Error.WriteLine($"[EventBus] Error in handler for {typeof(TEvent).Name}: {ex.Message}");
                }
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}
