using System;
using System.Collections.Generic;
using System.Linq;
using GildedRoseKata.Core.Configuration;
using GildedRoseKata.Core.Factories;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.Services;
using GildedRoseKata.Core.Strategies;

namespace GildedRoseKata.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGildedRose(this IServiceCollection services, 
        Action<GildedRoseConfiguration> configureOptions = null)
    {
        var configuration = new GildedRoseConfiguration();
        configureOptions?.Invoke(configuration);

        services.AddSingleton<IGildedRoseConfiguration>(configuration);
        
        // Core services
        services.AddTransient<IGildedRose, EnhancedGildedRose>();
        services.AddSingleton<IItemFactory, ItemFactory>();
        services.AddSingleton<IQualityUpdateStrategyFactory, QualityUpdateStrategyFactory>();
        
        // Repositories and persistence
        services.AddSingleton<IItemRepository, InMemoryItemRepository>();
        
        // Validation and metrics
        services.AddTransient<IItemValidator, DefaultItemValidator>();
        services.AddSingleton<IItemMetrics, ItemMetricsService>();
        
        // Processing pipeline
        services.AddTransient<IQualityUpdatePipeline, QualityUpdatePipeline>();
        
        // Event system
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IEventSubscriptionManager, EventSubscriptionManager>();
        
        // Logging
        if (configuration.LoggingSettings.EnableLogging)
        {
            services.AddSingleton<IGildedRoseLogger, GildedRoseLogger>();
        }
        else
        {
            services.AddSingleton<IGildedRoseLogger, NullLogger>();
        }
        
        // Plugin system
        if (configuration.ExtensibilitySettings.EnablePluginSystem)
        {
            services.AddSingleton<IPluginManager, PluginManager>();
            services.AddSingleton<IPluginLoader, DynamicPluginLoader>();
        }
        
        return services;
    }

    public static IServiceCollection AddCustomStrategy<T>(this IServiceCollection services)
        where T : class, IQualityUpdateStrategy
    {
        services.AddTransient<IQualityUpdateStrategy, T>();
        return services;
    }

    public static IServiceCollection AddCustomItemValidator<T>(this IServiceCollection services)
        where T : class, IItemValidator
    {
        services.AddTransient<IItemValidator, T>();
        return services;
    }

    public static IServiceCollection AddCustomRepository<T>(this IServiceCollection services)
        where T : class, IItemRepository
    {
        services.AddSingleton<IItemRepository, T>();
        return services;
    }
}

// Simple service collection interface for dependency injection
public interface IServiceCollection
{
    IServiceCollection AddSingleton<TInterface, TImplementation>()
        where TImplementation : class, TInterface;
    IServiceCollection AddSingleton<T>(T instance);
    IServiceCollection AddTransient<TInterface, TImplementation>()
        where TImplementation : class, TInterface;
    IServiceCollection AddScoped<TInterface, TImplementation>()
        where TImplementation : class, TInterface;
}

public interface IServiceProvider
{
    T GetService<T>();
    object GetService(Type serviceType);
    IEnumerable<T> GetServices<T>();
}

public class SimpleServiceCollection : IServiceCollection
{
    private readonly Dictionary<Type, ServiceDescriptor> _services = new();
    private readonly Dictionary<Type, List<ServiceDescriptor>> _multipleServices = new();

    public IServiceCollection AddSingleton<TInterface, TImplementation>()
        where TImplementation : class, TInterface
    {
        _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Singleton);
        return this;
    }

    public IServiceCollection AddSingleton<T>(T instance)
    {
        _services[typeof(T)] = new ServiceDescriptor(instance);
        return this;
    }

    public IServiceCollection AddTransient<TInterface, TImplementation>()
        where TImplementation : class, TInterface
    {
        var interfaceType = typeof(TInterface);
        var descriptor = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Transient);
        
        if (!_multipleServices.ContainsKey(interfaceType))
            _multipleServices[interfaceType] = new List<ServiceDescriptor>();
        
        _multipleServices[interfaceType].Add(descriptor);
        _services[interfaceType] = descriptor; // Keep most recent as default
        
        return this;
    }

    public IServiceCollection AddScoped<TInterface, TImplementation>()
        where TImplementation : class, TInterface
    {
        _services[typeof(TInterface)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Scoped);
        return this;
    }

    public IServiceProvider BuildServiceProvider() => new SimpleServiceProvider(_services, _multipleServices);
}

public enum ServiceLifetime { Singleton, Transient, Scoped }

public class ServiceDescriptor
{
    public Type ImplementationType { get; }
    public object Instance { get; }
    public ServiceLifetime Lifetime { get; }

    public ServiceDescriptor(Type implementationType, ServiceLifetime lifetime)
    {
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }

    public ServiceDescriptor(object instance)
    {
        Instance = instance;
        Lifetime = ServiceLifetime.Singleton;
    }
}

public class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, ServiceDescriptor> _services;
    private readonly Dictionary<Type, List<ServiceDescriptor>> _multipleServices;
    private readonly Dictionary<Type, object> _singletonInstances = new();

    public SimpleServiceProvider(Dictionary<Type, ServiceDescriptor> services, 
        Dictionary<Type, List<ServiceDescriptor>> multipleServices)
    {
        _services = services;
        _multipleServices = multipleServices;
    }

    public T GetService<T>() => (T)GetService(typeof(T));

    public object GetService(Type serviceType)
    {
        if (!_services.TryGetValue(serviceType, out var descriptor))
            return null;

        if (descriptor.Instance != null)
            return descriptor.Instance;

        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            if (_singletonInstances.TryGetValue(serviceType, out var singleton))
                return singleton;

            singleton = Activator.CreateInstance(descriptor.ImplementationType);
            _singletonInstances[serviceType] = singleton;
            return singleton;
        }

        return Activator.CreateInstance(descriptor.ImplementationType);
    }

    public IEnumerable<T> GetServices<T>()
    {
        var serviceType = typeof(T);
        if (!_multipleServices.TryGetValue(serviceType, out var descriptors))
            yield break;

        foreach (var descriptor in descriptors)
        {
            if (descriptor.Instance != null)
                yield return (T)descriptor.Instance;
            else
                yield return (T)Activator.CreateInstance(descriptor.ImplementationType);
        }
    }
}