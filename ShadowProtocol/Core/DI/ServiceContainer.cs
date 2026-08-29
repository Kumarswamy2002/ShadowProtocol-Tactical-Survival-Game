namespace ShadowProtocol.Core.DI;

public interface IServiceContainer
{
    void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface;
    void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class;
    void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface;
    TInterface Resolve<TInterface>() where TInterface : class;
    bool TryResolve<TInterface>(out TInterface? service) where TInterface : class;
    void Clear();
}

public class ServiceContainer : IServiceContainer
{
    private static ServiceContainer? _instance;
    public static ServiceContainer Instance => _instance ??= new ServiceContainer();

    private readonly Dictionary<Type, ServiceDescriptor> _services = new();
    private readonly object _lock = new();

    private enum ServiceLifetime { Singleton, Transient }

    private class ServiceDescriptor
    {
        public Type ServiceType { get; set; } = null!;
        public Type? ImplementationType { get; set; }
        public object? Instance { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }

    public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface
    {
        lock (_lock)
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Singleton
            };
        }
    }

    public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_lock)
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Instance = instance,
                Lifetime = ServiceLifetime.Singleton
            };
        }
    }

    public void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface
    {
        lock (_lock)
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }
    }

    public TInterface Resolve<TInterface>() where TInterface : class
    {
        if (TryResolve<TInterface>(out var service) && service != null)
        {
            return service;
        }
        throw new InvalidOperationException($"Service of type {typeof(TInterface).FullName} is not registered.");
    }

    public bool TryResolve<TInterface>(out TInterface? service) where TInterface : class
    {
        lock (_lock)
        {
            var type = typeof(TInterface);
            if (!_services.TryGetValue(type, out var descriptor))
            {
                service = null;
                return false;
            }

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (descriptor.Instance == null)
                {
                    descriptor.Instance = Activator.CreateInstance(descriptor.ImplementationType!);
                }
                service = (TInterface)descriptor.Instance!;
                return true;
            }
            else
            {
                service = (TInterface)Activator.CreateInstance(descriptor.ImplementationType!)!;
                return true;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
        }
    }
}
