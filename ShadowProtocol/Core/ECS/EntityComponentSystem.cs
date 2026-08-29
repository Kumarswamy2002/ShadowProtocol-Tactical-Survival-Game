// =============================================================================
// ShadowProtocol.Core — Entity Component System: Core Architecture
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowProtocol.Core.ECS
{
    /// <summary>
    /// Unique identifier for an entity in the ECS world.
    /// Entities are lightweight handles that serve as keys for component data.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        /// <summary>The raw integer ID value.</summary>
        public readonly int Id;
        /// <summary>Generation counter for detecting stale references after recycling.</summary>
        public readonly int Generation;

        /// <summary>An invalid entity ID sentinel.</summary>
        public static readonly EntityId Invalid = new EntityId(-1, 0);

        public EntityId(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool IsValid => Id >= 0;
        public bool Equals(EntityId other) => Id == other.Id && Generation == other.Generation;
        public int CompareTo(EntityId other) => Id.CompareTo(other.Id);
        public override bool Equals(object obj) => obj is EntityId e && Equals(e);
        public override int GetHashCode() => HashCode.Combine(Id, Generation);
        public override string ToString() => $"Entity({Id}:{Generation})";
        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);
    }

    /// <summary>
    /// Base interface for all components in the ECS.
    /// Components are pure data containers with no behavior.
    /// </summary>
    public interface IComponent { }

    /// <summary>
    /// Interface for components that need initialization when attached.
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }

    /// <summary>
    /// Interface for components that need cleanup when detached.
    /// </summary>
    public interface IDisposableComponent : IComponent, IDisposable { }

    /// <summary>
    /// Base class for all ECS systems that process entities with specific component sets.
    /// Systems contain the logic; they operate on component data each frame.
    /// </summary>
    public abstract class SystemBase
    {
        /// <summary>The world this system belongs to.</summary>
        public World World { get; internal set; }

        /// <summary>Whether this system is currently enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Execution priority (lower values execute first).</summary>
        public virtual int Priority => 0;

        /// <summary>Called once when the system is added to the world.</summary>
        public virtual void OnCreate() { }

        /// <summary>Called every frame to update the system.</summary>
        public abstract void OnUpdate(float deltaTime);

        /// <summary>Called when the system is removed from the world.</summary>
        public virtual void OnDestroy() { }

        /// <summary>Called after all systems have completed their update for the current frame.</summary>
        public virtual void OnLateUpdate(float deltaTime) { }

        /// <summary>Called at a fixed timestep for physics-related updates.</summary>
        public virtual void OnFixedUpdate(float fixedDeltaTime) { }
    }

    /// <summary>
    /// Generic system base that automatically filters entities matching a component signature.
    /// </summary>
    public abstract class SystemBase<T1> : SystemBase where T1 : IComponent
    {
        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in World.Query<T1>())
            {
                ref var comp = ref World.GetComponent<T1>(entity);
                ProcessEntity(entity, ref comp, deltaTime);
            }
        }

        protected abstract void ProcessEntity(EntityId entity, ref T1 component, float deltaTime);
    }

    /// <summary>
    /// System base for entities with two required components.
    /// </summary>
    public abstract class SystemBase<T1, T2> : SystemBase
        where T1 : IComponent where T2 : IComponent
    {
        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in World.Query<T1, T2>())
            {
                ref var c1 = ref World.GetComponent<T1>(entity);
                ref var c2 = ref World.GetComponent<T2>(entity);
                ProcessEntity(entity, ref c1, ref c2, deltaTime);
            }
        }

        protected abstract void ProcessEntity(EntityId entity, ref T1 c1, ref T2 c2, float deltaTime);
    }

    /// <summary>
    /// System base for entities with three required components.
    /// </summary>
    public abstract class SystemBase<T1, T2, T3> : SystemBase
        where T1 : IComponent where T2 : IComponent where T3 : IComponent
    {
        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in World.Query<T1, T2, T3>())
            {
                ref var c1 = ref World.GetComponent<T1>(entity);
                ref var c2 = ref World.GetComponent<T2>(entity);
                ref var c3 = ref World.GetComponent<T3>(entity);
                ProcessEntity(entity, ref c1, ref c2, ref c3, deltaTime);
            }
        }

        protected abstract void ProcessEntity(EntityId entity, ref T1 c1, ref T2 c2, ref T3 c3, float deltaTime);
    }

    /// <summary>
    /// Type-erased component storage interface for the internal registry.
    /// </summary>
    internal interface IComponentPool
    {
        bool Has(int entityId);
        void Remove(int entityId);
        void Clear();
        int Count { get; }
        Type ComponentType { get; }
    }

    /// <summary>
    /// Strongly-typed dense array storage for a specific component type.
    /// Uses sparse-dense mapping for cache-friendly iteration.
    /// </summary>
    internal sealed class ComponentPool<T> : IComponentPool where T : IComponent
    {
        private const int InitialCapacity = 256;

        // Sparse array: maps entity ID to dense array index
        private int[] _sparse;
        // Dense arrays: stores component data contiguously for cache efficiency
        private T[] _denseComponents;
        private int[] _denseEntities;
        private int _count;

        public int Count => _count;
        public Type ComponentType => typeof(T);

        public ComponentPool()
        {
            _sparse = new int[InitialCapacity];
            _denseComponents = new T[InitialCapacity];
            _denseEntities = new int[InitialCapacity];
            _count = 0;

            Array.Fill(_sparse, -1);
        }

        /// <summary>
        /// Adds or sets a component for the given entity.
        /// </summary>
        public void Set(int entityId, T component)
        {
            EnsureSparseCapacity(entityId + 1);

            if (_sparse[entityId] >= 0)
            {
                // Entity already has this component, update it
                _denseComponents[_sparse[entityId]] = component;
                return;
            }

            // Add new entry
            EnsureDenseCapacity(_count + 1);
            _sparse[entityId] = _count;
            _denseComponents[_count] = component;
            _denseEntities[_count] = entityId;
            _count++;
        }

        /// <summary>
        /// Gets a reference to the component for the given entity.
        /// Throws if the entity doesn't have this component.
        /// </summary>
        public ref T Get(int entityId)
        {
            if (entityId < 0 || entityId >= _sparse.Length || _sparse[entityId] < 0)
                throw new InvalidOperationException(
                    $"Entity {entityId} does not have component {typeof(T).Name}");
            return ref _denseComponents[_sparse[entityId]];
        }

        /// <summary>
        /// Tries to get the component for the given entity.
        /// </summary>
        public bool TryGet(int entityId, out T component)
        {
            if (entityId >= 0 && entityId < _sparse.Length && _sparse[entityId] >= 0)
            {
                component = _denseComponents[_sparse[entityId]];
                return true;
            }
            component = default;
            return false;
        }

        public bool Has(int entityId)
        {
            return entityId >= 0 && entityId < _sparse.Length && _sparse[entityId] >= 0;
        }

        public void Remove(int entityId)
        {
            if (entityId < 0 || entityId >= _sparse.Length || _sparse[entityId] < 0)
                return;

            int denseIdx = _sparse[entityId];
            int lastIdx = _count - 1;

            if (denseIdx != lastIdx)
            {
                // Swap with last element to maintain density
                _denseComponents[denseIdx] = _denseComponents[lastIdx];
                _denseEntities[denseIdx] = _denseEntities[lastIdx];
                _sparse[_denseEntities[lastIdx]] = denseIdx;
            }

            _sparse[entityId] = -1;
            _denseComponents[lastIdx] = default;
            _count--;
        }

        public void Clear()
        {
            Array.Fill(_sparse, -1);
            Array.Clear(_denseComponents, 0, _count);
            _count = 0;
        }

        /// <summary>
        /// Returns all entity IDs that have this component type.
        /// </summary>
        public ReadOnlySpan<int> GetEntities() => new ReadOnlySpan<int>(_denseEntities, 0, _count);

        /// <summary>
        /// Returns all component data as a span for efficient batch processing.
        /// </summary>
        public Span<T> GetComponents() => new Span<T>(_denseComponents, 0, _count);

        private void EnsureSparseCapacity(int needed)
        {
            if (needed <= _sparse.Length) return;
            int newSize = System.Math.Max(needed, _sparse.Length * 2);
            int oldSize = _sparse.Length;
            Array.Resize(ref _sparse, newSize);
            for (int i = oldSize; i < newSize; i++) _sparse[i] = -1;
        }

        private void EnsureDenseCapacity(int needed)
        {
            if (needed <= _denseComponents.Length) return;
            int newSize = System.Math.Max(needed, _denseComponents.Length * 2);
            Array.Resize(ref _denseComponents, newSize);
            Array.Resize(ref _denseEntities, newSize);
        }
    }

    /// <summary>
    /// The ECS World is the central manager that owns all entities, components, and systems.
    /// It provides entity creation/destruction, component management, and system execution.
    /// </summary>
    public sealed class World : IDisposable
    {
        // Entity management
        private int _nextEntityId;
        private int[] _generations;
        private readonly Queue<int> _recycledIds = new Queue<int>();
        private readonly HashSet<int> _aliveEntities = new HashSet<int>();

        // Component storage
        private readonly Dictionary<Type, IComponentPool> _componentPools = new Dictionary<Type, IComponentPool>();

        // Systems
        private readonly List<SystemBase> _systems = new List<SystemBase>();
        private bool _systemsSorted;

        // Deferred operations (thread-safe command buffer)
        private readonly List<Action> _deferredCommands = new List<Action>();

        // Statistics
        private int _totalEntitiesCreated;
        private int _totalEntitiesDestroyed;

        /// <summary>Number of currently alive entities.</summary>
        public int EntityCount => _aliveEntities.Count;

        /// <summary>Total entities ever created.</summary>
        public int TotalEntitiesCreated => _totalEntitiesCreated;

        /// <summary>Number of registered systems.</summary>
        public int SystemCount => _systems.Count;

        /// <summary>
        /// Creates a new ECS world with the specified initial entity capacity.
        /// </summary>
        public World(int initialCapacity = 1024)
        {
            _generations = new int[initialCapacity];
        }

        // =====================================================================
        // Entity Management
        // =====================================================================

        /// <summary>
        /// Creates a new entity and returns its ID.
        /// Recycled entity IDs have incremented generation counters.
        /// </summary>
        public EntityId CreateEntity()
        {
            int id;
            if (_recycledIds.Count > 0)
            {
                id = _recycledIds.Dequeue();
            }
            else
            {
                id = _nextEntityId++;
                if (id >= _generations.Length)
                    Array.Resize(ref _generations, _generations.Length * 2);
            }

            _aliveEntities.Add(id);
            _totalEntitiesCreated++;

            return new EntityId(id, _generations[id]);
        }

        /// <summary>
        /// Creates an entity with an initial set of components (builder pattern).
        /// </summary>
        public EntityBuilder CreateEntityWith()
        {
            return new EntityBuilder(this, CreateEntity());
        }

        /// <summary>
        /// Destroys an entity and removes all its components.
        /// </summary>
        public void DestroyEntity(EntityId entity)
        {
            if (!IsAlive(entity)) return;

            // Remove all components
            foreach (var pool in _componentPools.Values)
            {
                pool.Remove(entity.Id);
            }

            _aliveEntities.Remove(entity.Id);
            _generations[entity.Id]++;
            _recycledIds.Enqueue(entity.Id);
            _totalEntitiesDestroyed++;
        }

        /// <summary>
        /// Checks if an entity is currently alive and its generation matches.
        /// </summary>
        public bool IsAlive(EntityId entity)
        {
            return entity.Id >= 0
                && entity.Id < _generations.Length
                && _aliveEntities.Contains(entity.Id)
                && _generations[entity.Id] == entity.Generation;
        }

        /// <summary>
        /// Returns all currently alive entity IDs.
        /// </summary>
        public IEnumerable<EntityId> GetAllEntities()
        {
            foreach (int id in _aliveEntities)
            {
                yield return new EntityId(id, _generations[id]);
            }
        }

        // =====================================================================
        // Component Management
        // =====================================================================

        /// <summary>
        /// Adds or replaces a component on the given entity.
        /// </summary>
        public void SetComponent<T>(EntityId entity, T component) where T : IComponent
        {
            if (!IsAlive(entity))
                throw new InvalidOperationException($"Cannot add component to dead entity {entity}");

            GetOrCreatePool<T>().Set(entity.Id, component);

            if (component is IInitializable init)
                init.Initialize();
        }

        /// <summary>
        /// Gets a reference to the component on the given entity.
        /// Throws if the entity doesn't have this component.
        /// </summary>
        public ref T GetComponent<T>(EntityId entity) where T : IComponent
        {
            return ref GetOrCreatePool<T>().Get(entity.Id);
        }

        /// <summary>
        /// Tries to get the component. Returns false if not present.
        /// </summary>
        public bool TryGetComponent<T>(EntityId entity, out T component) where T : IComponent
        {
            return GetOrCreatePool<T>().TryGet(entity.Id, out component);
        }

        /// <summary>
        /// Checks if the entity has a specific component type.
        /// </summary>
        public bool HasComponent<T>(EntityId entity) where T : IComponent
        {
            return GetOrCreatePool<T>().Has(entity.Id);
        }

        /// <summary>
        /// Removes a component from the entity.
        /// </summary>
        public void RemoveComponent<T>(EntityId entity) where T : IComponent
        {
            GetOrCreatePool<T>().Remove(entity.Id);
        }

        private ComponentPool<T> GetOrCreatePool<T>() where T : IComponent
        {
            var type = typeof(T);
            if (!_componentPools.TryGetValue(type, out var pool))
            {
                pool = new ComponentPool<T>();
                _componentPools[type] = pool;
            }
            return (ComponentPool<T>)pool;
        }

        // =====================================================================
        // Queries — Find entities matching component requirements
        // =====================================================================

        /// <summary>
        /// Returns all entities that have the specified component type.
        /// </summary>
        public IEnumerable<EntityId> Query<T1>() where T1 : IComponent
        {
            var pool = GetOrCreatePool<T1>();
            var entities = pool.GetEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                int id = entities[i];
                if (_aliveEntities.Contains(id))
                    yield return new EntityId(id, _generations[id]);
            }
        }

        /// <summary>
        /// Returns all entities that have both component types.
        /// </summary>
        public IEnumerable<EntityId> Query<T1, T2>()
            where T1 : IComponent where T2 : IComponent
        {
            var pool1 = GetOrCreatePool<T1>();
            var pool2 = GetOrCreatePool<T2>();

            // Iterate over the smaller pool for efficiency
            var smallerPool = pool1.Count <= pool2.Count ? (IComponentPool)pool1 : pool2;
            var entities = smallerPool == pool1 ? pool1.GetEntities() : pool2.GetEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                int id = entities[i];
                if (_aliveEntities.Contains(id) && pool1.Has(id) && pool2.Has(id))
                    yield return new EntityId(id, _generations[id]);
            }
        }

        /// <summary>
        /// Returns all entities that have all three component types.
        /// </summary>
        public IEnumerable<EntityId> Query<T1, T2, T3>()
            where T1 : IComponent where T2 : IComponent where T3 : IComponent
        {
            var pool1 = GetOrCreatePool<T1>();
            var pool2 = GetOrCreatePool<T2>();
            var pool3 = GetOrCreatePool<T3>();

            // Find smallest pool
            IComponentPool smallest = pool1;
            if (pool2.Count < smallest.Count) smallest = pool2;
            if (pool3.Count < smallest.Count) smallest = pool3;

            ReadOnlySpan<int> entities;
            if (smallest == pool1) entities = pool1.GetEntities();
            else if (smallest == pool2) entities = pool2.GetEntities();
            else entities = pool3.GetEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                int id = entities[i];
                if (_aliveEntities.Contains(id) && pool1.Has(id) && pool2.Has(id) && pool3.Has(id))
                    yield return new EntityId(id, _generations[id]);
            }
        }

        /// <summary>
        /// Returns all entities matching the include filter and not matching the exclude filter.
        /// </summary>
        public IEnumerable<EntityId> QueryExclude<TInclude, TExclude>()
            where TInclude : IComponent where TExclude : IComponent
        {
            var include = GetOrCreatePool<TInclude>();
            var exclude = GetOrCreatePool<TExclude>();
            var entities = include.GetEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                int id = entities[i];
                if (_aliveEntities.Contains(id) && !exclude.Has(id))
                    yield return new EntityId(id, _generations[id]);
            }
        }

        // =====================================================================
        // System Management
        // =====================================================================

        /// <summary>
        /// Registers a system in this world.
        /// </summary>
        public T AddSystem<T>() where T : SystemBase, new()
        {
            var system = new T { World = this };
            _systems.Add(system);
            _systemsSorted = false;
            system.OnCreate();
            return system;
        }

        /// <summary>
        /// Registers a pre-created system instance.
        /// </summary>
        public void AddSystem(SystemBase system)
        {
            system.World = this;
            _systems.Add(system);
            _systemsSorted = false;
            system.OnCreate();
        }

        /// <summary>
        /// Removes a system from this world.
        /// </summary>
        public void RemoveSystem<T>() where T : SystemBase
        {
            var system = _systems.FirstOrDefault(s => s is T);
            if (system != null)
            {
                system.OnDestroy();
                _systems.Remove(system);
            }
        }

        /// <summary>
        /// Gets a system of the specified type.
        /// </summary>
        public T GetSystem<T>() where T : SystemBase
        {
            return _systems.OfType<T>().FirstOrDefault();
        }

        // =====================================================================
        // Update Loop
        // =====================================================================

        /// <summary>
        /// Runs all enabled systems' OnUpdate methods in priority order.
        /// Also processes any deferred commands after the update pass.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_systemsSorted)
            {
                _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                _systemsSorted = true;
            }

            // Main update pass
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Enabled)
                    _systems[i].OnUpdate(deltaTime);
            }

            // Late update pass
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Enabled)
                    _systems[i].OnLateUpdate(deltaTime);
            }

            // Process deferred commands
            ProcessDeferredCommands();
        }

        /// <summary>
        /// Runs all enabled systems' OnFixedUpdate at a fixed timestep.
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Enabled)
                    _systems[i].OnFixedUpdate(fixedDeltaTime);
            }

            ProcessDeferredCommands();
        }

        // =====================================================================
        // Deferred Commands
        // =====================================================================

        /// <summary>
        /// Enqueues a command to be executed after the current update frame.
        /// Use this to safely create/destroy entities during system updates.
        /// </summary>
        public void Defer(Action command)
        {
            _deferredCommands.Add(command);
        }

        private void ProcessDeferredCommands()
        {
            if (_deferredCommands.Count == 0) return;

            var commands = new List<Action>(_deferredCommands);
            _deferredCommands.Clear();

            foreach (var cmd in commands)
                cmd.Invoke();
        }

        // =====================================================================
        // Cleanup
        // =====================================================================

        public void Dispose()
        {
            foreach (var system in _systems)
                system.OnDestroy();
            _systems.Clear();

            foreach (var pool in _componentPools.Values)
                pool.Clear();
            _componentPools.Clear();

            _aliveEntities.Clear();
            _recycledIds.Clear();
        }

        /// <summary>
        /// Returns diagnostic information about the world's current state.
        /// </summary>
        public WorldStats GetStats()
        {
            var stats = new WorldStats
            {
                AliveEntities = _aliveEntities.Count,
                TotalCreated = _totalEntitiesCreated,
                TotalDestroyed = _totalEntitiesDestroyed,
                RecycledIds = _recycledIds.Count,
                RegisteredSystems = _systems.Count,
                ComponentPoolCount = _componentPools.Count,
            };

            foreach (var kvp in _componentPools)
            {
                stats.ComponentCounts[kvp.Key.Name] = kvp.Value.Count;
            }

            return stats;
        }
    }

    /// <summary>
    /// Diagnostic statistics about the ECS world.
    /// </summary>
    public class WorldStats
    {
        public int AliveEntities;
        public int TotalCreated;
        public int TotalDestroyed;
        public int RecycledIds;
        public int RegisteredSystems;
        public int ComponentPoolCount;
        public Dictionary<string, int> ComponentCounts = new Dictionary<string, int>();

        public override string ToString()
        {
            return $"ECS World: {AliveEntities} entities alive, {RegisteredSystems} systems, " +
                   $"{ComponentPoolCount} component types, {TotalCreated} total created";
        }
    }

    /// <summary>
    /// Fluent builder for creating entities with components.
    /// </summary>
    public class EntityBuilder
    {
        private readonly World _world;
        private readonly EntityId _entity;

        internal EntityBuilder(World world, EntityId entity)
        {
            _world = world;
            _entity = entity;
        }

        /// <summary>Adds a component to the entity being built.</summary>
        public EntityBuilder With<T>(T component) where T : IComponent
        {
            _world.SetComponent(_entity, component);
            return this;
        }

        /// <summary>Finalizes and returns the entity ID.</summary>
        public EntityId Build() => _entity;

        /// <summary>Implicit conversion to EntityId.</summary>
        public static implicit operator EntityId(EntityBuilder builder) => builder._entity;
    }

    // =====================================================================
    // Common Game Components
    // =====================================================================

    /// <summary>
    /// Transform component: position, rotation, and scale of an entity in the world.
    /// </summary>
    public struct TransformComponent : IComponent
    {
        public Math.Vector3F Position;
        public Math.QuaternionF Rotation;
        public Math.Vector3F Scale;
        public Math.Vector3F Forward => Rotation.GetForward();
        public Math.Vector3F Right => Rotation.GetRight();
        public Math.Vector3F Up => Rotation.GetUp();

        public static TransformComponent Default => new TransformComponent
        {
            Position = Math.Vector3F.Zero,
            Rotation = Math.QuaternionF.Identity,
            Scale = new Math.Vector3F(1, 1, 1)
        };

        public Math.Matrix4x4F ToMatrix() =>
            Math.Matrix4x4F.TRS(Position, Rotation, Scale);
    }

    /// <summary>
    /// Velocity component for physics-based movement.
    /// </summary>
    public struct VelocityComponent : IComponent
    {
        public Math.Vector3F Linear;
        public Math.Vector3F Angular;
        public float Drag;
        public float AngularDrag;

        public static VelocityComponent Default => new VelocityComponent
        {
            Drag = 0.01f,
            AngularDrag = 0.05f
        };
    }

    /// <summary>
    /// Tag component to mark entities as "active" in gameplay.
    /// </summary>
    public struct ActiveTag : IComponent { }

    /// <summary>
    /// Tag component for entities pending destruction at end of frame.
    /// </summary>
    public struct DestroyTag : IComponent { }

    /// <summary>
    /// Component for entities with a visual name/label.
    /// </summary>
    public struct NameComponent : IComponent
    {
        public string Name;
        public string Tag;

        public NameComponent(string name, string tag = "")
        {
            Name = name;
            Tag = tag;
        }
    }

    /// <summary>
    /// Component for timed lifetime (auto-destroy after duration).
    /// </summary>
    public struct LifetimeComponent : IComponent
    {
        public float RemainingTime;
        public float TotalLifetime;

        public float NormalizedAge => 1f - (RemainingTime / TotalLifetime);

        public LifetimeComponent(float seconds)
        {
            RemainingTime = seconds;
            TotalLifetime = seconds;
        }
    }

    /// <summary>
    /// Hierarchical parent-child relationship component.
    /// </summary>
    public struct ParentComponent : IComponent
    {
        public EntityId Parent;
        public Math.Vector3F LocalPosition;
        public Math.QuaternionF LocalRotation;
    }

    /// <summary>
    /// Children tracking component.
    /// </summary>
    public struct ChildrenComponent : IComponent
    {
        public List<EntityId> Children;

        public void AddChild(EntityId child)
        {
            Children ??= new List<EntityId>();
            Children.Add(child);
        }

        public void RemoveChild(EntityId child)
        {
            Children?.Remove(child);
        }
    }

    // =====================================================================
    // Common Game Systems
    // =====================================================================

    /// <summary>
    /// System that moves entities based on their velocity components.
    /// </summary>
    public class MovementSystem : SystemBase<TransformComponent, VelocityComponent>
    {
        public override int Priority => 10;

        protected override void ProcessEntity(EntityId entity,
            ref TransformComponent transform, ref VelocityComponent velocity, float deltaTime)
        {
            // Apply linear velocity
            transform.Position = transform.Position + velocity.Linear * deltaTime;

            // Apply angular velocity (simplified)
            if (velocity.Angular.MagnitudeSquared() > 0.0001f)
            {
                float angMag = velocity.Angular.Magnitude();
                var axis = velocity.Angular * (1f / angMag);
                transform.Rotation = Math.QuaternionF.FromAxisAngle(axis, angMag * deltaTime) * transform.Rotation;
            }

            // Apply drag
            velocity.Linear = velocity.Linear * (1f - velocity.Drag * deltaTime);
            velocity.Angular = velocity.Angular * (1f - velocity.AngularDrag * deltaTime);
        }
    }

    /// <summary>
    /// System that counts down lifetime components and marks expired entities for destruction.
    /// </summary>
    public class LifetimeSystem : SystemBase<LifetimeComponent>
    {
        public override int Priority => 100;

        protected override void ProcessEntity(EntityId entity, ref LifetimeComponent lifetime, float deltaTime)
        {
            lifetime.RemainingTime -= deltaTime;
            if (lifetime.RemainingTime <= 0)
            {
                World.Defer(() => World.DestroyEntity(entity));
            }
        }
    }

    /// <summary>
    /// System that processes destroy-tagged entities.
    /// </summary>
    public class DestroySystem : SystemBase
    {
        public override int Priority => 999;

        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in World.Query<DestroyTag>())
            {
                World.Defer(() => World.DestroyEntity(entity));
            }
        }
    }

    /// <summary>
    /// System that updates child transforms based on parent hierarchy.
    /// </summary>
    public class HierarchySystem : SystemBase
    {
        public override int Priority => 5;

        public override void OnUpdate(float deltaTime)
        {
            foreach (var entity in World.Query<TransformComponent, ParentComponent>())
            {
                ref var transform = ref World.GetComponent<TransformComponent>(entity);
                ref var parent = ref World.GetComponent<ParentComponent>(entity);

                if (!World.IsAlive(parent.Parent)) continue;
                if (!World.HasComponent<TransformComponent>(parent.Parent)) continue;

                ref var parentTransform = ref World.GetComponent<TransformComponent>(parent.Parent);

                // Compute world transform from parent
                var parentMatrix = parentTransform.ToMatrix();
                var localPos = parent.LocalRotation.Rotate(parent.LocalPosition);
                transform.Position = parentTransform.Position + localPos;
                transform.Rotation = parentTransform.Rotation * parent.LocalRotation;
            }
        }
    }
}
