using System;
using System.Collections.Generic;
using System.Linq;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.ValueObjects;
using GildedRoseKata.Core.Configuration;

namespace GildedRoseKata.Core.Services;

// Default implementations for core services
public class DefaultItemValidator : IItemValidator
{
    public ValidationResult Validate(IItem item)
    {
        var errors = GetValidationErrors(item).ToList();
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public bool IsValid(IItem item) => !GetValidationErrors(item).Any();

    public IEnumerable<string> GetValidationErrors(IItem item)
    {
        if (item == null)
            yield return "Item cannot be null";
        
        if (item?.Database == null)
            yield return "Database cannot be null";
        
        if (item?.Temperature == null)
            yield return "Temperature cannot be null";
        
        if (item?.Password == null)
            yield return "Password cannot be null";
    }
}

public class InMemoryItemRepository : IItemRepository
{
    private readonly Dictionary<Guid, IItem> _items = new();

    public IItem FindById(Guid id) => _items.TryGetValue(id, out var item) ? item : null;

    public IEnumerable<IItem> FindByType(string itemType) =>
        _items.Values.Where(i => i.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IItem> FindByDatabase(IDatabaseName database) =>
        _items.Values.Where(i => i.Database.Equals(database));

    public void Save(IItem item)
    {
        if (item != null)
            _items[item.Id] = item;
    }

    public void SaveAll(IEnumerable<IItem> items)
    {
        foreach (var item in items ?? Enumerable.Empty<IItem>())
            Save(item);
    }

    public void Delete(Guid id) => _items.Remove(id);

    public IEnumerable<IItem> GetAll() => _items.Values.ToList();
}

public class ItemMetricsService : IItemMetrics
{
    private int _totalProcessingCount;
    private TimeSpan _totalProcessingTime;
    private readonly Dictionary<string, int> _processingCountByType = new();

    public int TotalProcessingCount => _totalProcessingCount;
    public TimeSpan AverageProcessingTime => _totalProcessingCount > 0 
        ? new TimeSpan(_totalProcessingTime.Ticks / _totalProcessingCount) 
        : TimeSpan.Zero;
    public Dictionary<string, int> ProcessingCountByType => new(_processingCountByType);

    public void RecordProcessing(IItem item, TimeSpan processingTime)
    {
        _totalProcessingCount++;
        _totalProcessingTime = _totalProcessingTime.Add(processingTime);
        
        var itemType = item?.ItemType ?? "Unknown";
        _processingCountByType[itemType] = _processingCountByType.GetValueOrDefault(itemType, 0) + 1;
    }

    public void Reset()
    {
        _totalProcessingCount = 0;
        _totalProcessingTime = TimeSpan.Zero;
        _processingCountByType.Clear();
    }
}

public interface IGildedRoseLogger
{
    void LogTrace(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception exception = null, params object[] args);
    void LogCritical(string message, Exception exception = null, params object[] args);
}

public class GildedRoseLogger : IGildedRoseLogger
{
    private readonly LoggingSettings _settings;

    public GildedRoseLogger(IGildedRoseConfiguration configuration)
    {
        _settings = configuration?.LoggingSettings ?? new LoggingSettings();
    }

    public void LogTrace(string message, params object[] args) => Log(LogLevel.Trace, message, null, args);
    public void LogDebug(string message, params object[] args) => Log(LogLevel.Debug, message, null, args);
    public void LogInformation(string message, params object[] args) => Log(LogLevel.Information, message, null, args);
    public void LogWarning(string message, params object[] args) => Log(LogLevel.Warning, message, null, args);
    public void LogError(string message, Exception exception = null, params object[] args) => Log(LogLevel.Error, message, exception, args);
    public void LogCritical(string message, Exception exception = null, params object[] args) => Log(LogLevel.Critical, message, exception, args);

    private void Log(LogLevel level, string message, Exception exception, params object[] args)
    {
        if (!_settings.EnableLogging || level < _settings.MinimumLogLevel)
            return;

        var formattedMessage = args?.Length > 0 ? string.Format(message, args) : message;
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {formattedMessage}";
        
        if (exception != null)
            logEntry += $"\nException: {exception}";

        Console.WriteLine(logEntry);
    }
}

public class NullLogger : IGildedRoseLogger
{
    public void LogTrace(string message, params object[] args) { }
    public void LogDebug(string message, params object[] args) { }
    public void LogInformation(string message, params object[] args) { }
    public void LogWarning(string message, params object[] args) { }
    public void LogError(string message, Exception exception = null, params object[] args) { }
    public void LogCritical(string message, Exception exception = null, params object[] args) { }
}

// Simple event system for extensibility
public interface IEventBus
{
    void Publish<T>(T eventData) where T : class;
    void Subscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action<T> handler) where T : class;
}

public interface IEventSubscriptionManager
{
    void Subscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action<T> handler) where T : class;
    IEnumerable<Action<T>> GetHandlers<T>() where T : class;
}

public class EventBus : IEventBus
{
    private readonly IEventSubscriptionManager _subscriptionManager;

    public EventBus(IEventSubscriptionManager subscriptionManager)
    {
        _subscriptionManager = subscriptionManager;
    }

    public void Publish<T>(T eventData) where T : class
    {
        var handlers = _subscriptionManager.GetHandlers<T>();
        foreach (var handler in handlers)
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                // Log error but don't stop other handlers
                Console.WriteLine($"Error in event handler: {ex.Message}");
            }
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : class
    {
        _subscriptionManager.Subscribe(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        _subscriptionManager.Unsubscribe(handler);
    }
}

public class EventSubscriptionManager : IEventSubscriptionManager
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();
        
        _handlers[eventType].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
            handlers.Remove(handler);
    }

    public IEnumerable<Action<T>> GetHandlers<T>() where T : class
    {
        var eventType = typeof(T);
        return _handlers.TryGetValue(eventType, out var handlers)
            ? handlers.Cast<Action<T>>()
            : Enumerable.Empty<Action<T>>();
    }
}

// Plugin system interfaces
public interface IPluginManager
{
    void LoadPlugins();
    void RegisterPlugin(IPlugin plugin);
    void UnregisterPlugin(string pluginName);
    IEnumerable<IPlugin> GetLoadedPlugins();
}

public interface IPluginLoader
{
    IEnumerable<IPlugin> LoadFromDirectory(string directory);
    IPlugin LoadFromAssembly(string assemblyPath);
}

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    void Initialize(IServiceProvider serviceProvider);
    void Shutdown();
}

public class PluginManager : IPluginManager
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly IPluginLoader _loader;
    private readonly IGildedRoseConfiguration _configuration;

    public PluginManager(IPluginLoader loader, IGildedRoseConfiguration configuration)
    {
        _loader = loader;
        _configuration = configuration;
    }

    public void LoadPlugins()
    {
        if (!_configuration.ExtensibilitySettings.EnablePluginSystem)
            return;

        var plugins = _loader.LoadFromDirectory(_configuration.ExtensibilitySettings.PluginDirectory);
        foreach (var plugin in plugins)
        {
            RegisterPlugin(plugin);
        }
    }

    public void RegisterPlugin(IPlugin plugin)
    {
        if (plugin != null && !_plugins.ContainsKey(plugin.Name))
        {
            _plugins[plugin.Name] = plugin;
        }
    }

    public void UnregisterPlugin(string pluginName)
    {
        if (_plugins.TryGetValue(pluginName, out var plugin))
        {
            plugin.Shutdown();
            _plugins.Remove(pluginName);
        }
    }

    public IEnumerable<IPlugin> GetLoadedPlugins() => _plugins.Values;
}

public class DynamicPluginLoader : IPluginLoader
{
    public IEnumerable<IPlugin> LoadFromDirectory(string directory)
    {
        // Simplified implementation - in real scenario would load from assemblies
        return Enumerable.Empty<IPlugin>();
    }

    public IPlugin LoadFromAssembly(string assemblyPath)
    {
        // Simplified implementation - in real scenario would load from assembly
        return null;
    }
}