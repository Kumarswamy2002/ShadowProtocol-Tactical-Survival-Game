using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowProtocol.Core.Events
{
    public enum EventPriority
    {
        MonitorFirst = 0,
        High = 1,
        Normal = 2,
        Low = 3,
        Lowest = 4
    }

    public interface IGameEvent
    {
        string EventId { get; }
        long TimestampTicks { get; }
        bool IsCancelled { get; set; }
    }

    public abstract class BaseGameEvent : IGameEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString("N");
        public long TimestampTicks { get; } = DateTime.UtcNow.Ticks;
        public bool IsCancelled { get; set; } = false;
    }

    public class PlayerDamageEvent : BaseGameEvent
    {
        public string AttackerId { get; set; }
        public string VictimId { get; set; }
        public float Amount { get; set; }
        public string DamageType { get; set; }
        public bool IsCritical { get; set; }
    }

    public class MissionCompletedEvent : BaseGameEvent
    {
        public string MissionId { get; set; }
        public string PlayerId { get; set; }
        public int RewardCredits { get; set; }
        public int RewardXp { get; set; }
    }

    public class InventoryItemAddedEvent : BaseGameEvent
    {
        public string PlayerId { get; set; }
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class CoreEventBusV2
    {
        private class HandlerEntry
        {
            public Delegate Callback { get; set; }
            public EventPriority Priority { get; set; }
            public int ExecutionOrder { get; set; }
        }

        private readonly ConcurrentDictionary<Type, List<HandlerEntry>> _handlers = new ConcurrentDictionary<Type, List<HandlerEntry>>();
        private readonly ConcurrentQueue<IGameEvent> _pendingEventQueue = new ConcurrentQueue<IGameEvent>();
        private int _handlerCount = 0;

        public void Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal) where T : IGameEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);
            var entry = new HandlerEntry
            {
                Callback = handler,
                Priority = priority,
                ExecutionOrder = Interlocked.Increment(ref _handlerCount)
            };

            _handlers.AddOrUpdate(eventType,
                _ => new List<HandlerEntry> { entry },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(entry);
                        list.Sort((a, b) =>
                        {
                            int pComp = a.Priority.CompareTo(b.Priority);
                            return pComp != 0 ? pComp : a.ExecutionOrder.CompareTo(b.ExecutionOrder);
                        });
                    }
                    return list;
                });
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            var eventType = typeof(T);

            if (_handlers.TryGetValue(eventType, out var list))
            {
                lock (list)
                {
                    list.RemoveAll(entry => (Action<T>)entry.Callback == handler);
                }
            }
        }

        public void PublishImmediate<T>(T gameEvent) where T : IGameEvent
        {
            if (gameEvent == null) return;
            var eventType = typeof(T);

            if (_handlers.TryGetValue(eventType, out var list))
            {
                HandlerEntry[] handlersSnapshot;
                lock (list)
                {
                    handlersSnapshot = list.ToArray();
                }

                foreach (var entry in handlersSnapshot)
                {
                    if (gameEvent.IsCancelled) break;
                    try
                    {
                        ((Action<T>)entry.Callback)(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CoreEventBusV2] Error dispatching event {eventType.Name}: {ex.Message}");
                    }
                }
            }
        }

        public void EnqueueEvent(IGameEvent gameEvent)
        {
            if (gameEvent != null)
            {
                _pendingEventQueue.Enqueue(gameEvent);
            }
        }

        public void ProcessQueuedEvents(int maxEventsToProcess = 100)
        {
            int processed = 0;
            while (processed < maxEventsToProcess && _pendingEventQueue.TryDequeue(out var gameEvent))
            {
                var eventType = gameEvent.GetType();
                if (_handlers.TryGetValue(eventType, out var list))
                {
                    HandlerEntry[] snapshot;
                    lock (list)
                    {
                        snapshot = list.ToArray();
                    }

                    foreach (var entry in snapshot)
                    {
                        if (gameEvent.IsCancelled) break;
                        try
                        {
                            entry.Callback.DynamicInvoke(gameEvent);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CoreEventBusV2] Queued event error: {ex.Message}");
                        }
                    }
                }
                processed++;
            }
        }
    }
}
