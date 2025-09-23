using System;
using System.Collections.Generic;

namespace GildedRoseKata.Core.Configuration;

public interface IGildedRoseConfiguration
{
    QualitySettings QualitySettings { get; }
    TemperatureSettings TemperatureSettings { get; }
    ProcessingSettings ProcessingSettings { get; }
    LoggingSettings LoggingSettings { get; }
    ExtensibilitySettings ExtensibilitySettings { get; }
    Dictionary<string, object> CustomSettings { get; }
    
    T GetCustomSetting<T>(string key, T defaultValue = default);
    void SetCustomSetting<T>(string key, T value);
}

public class QualitySettings
{
    public int MinQuality { get; init; } = 0;
    public int MaxQuality { get; init; } = 50;
    public int DefaultQuality { get; init; } = 0;
    public bool EnforceQualityBounds { get; init; } = true;
    public Dictionary<string, int> ItemTypeQualityMultipliers { get; init; } = new();
}

public class TemperatureSettings
{
    public int DefaultTemperature { get; init; } = 0;
    public int ExpiryThreshold { get; init; } = 0;
    public int NearExpiryThreshold { get; init; } = 5;
    public bool AllowNegativeTemperature { get; init; } = true;
    public Dictionary<string, int> ItemTypeTemperatureModifiers { get; init; } = new();
}

public class ProcessingSettings
{
    public bool EnableAsyncProcessing { get; init; } = false;
    public bool EnableParallelProcessing { get; init; } = false;
    public int MaxConcurrencyLevel { get; init; } = Environment.ProcessorCount;
    public TimeSpan ProcessingTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public bool EnableRetryLogic { get; init; } = true;
    public int MaxRetryAttempts { get; init; } = 3;
}

public class LoggingSettings
{
    public bool EnableLogging { get; init; } = true;
    public bool LogItemChanges { get; init; } = false;
    public bool LogPerformanceMetrics { get; init; } = false;
    public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;
    public string LogFilePath { get; init; } = "gilded-rose.log";
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public class ExtensibilitySettings
{
    public bool EnablePluginSystem { get; init; } = false;
    public string PluginDirectory { get; init; } = "plugins";
    public bool EnableEventHooks { get; init; } = true;
    public bool EnableCustomStrategies { get; init; } = true;
    public List<string> AllowedPluginTypes { get; init; } = new();
    public Dictionary<string, object> PluginConfiguration { get; init; } = new();
}

public class GildedRoseConfiguration : IGildedRoseConfiguration
{
    public QualitySettings QualitySettings { get; init; } = new();
    public TemperatureSettings TemperatureSettings { get; init; } = new();
    public ProcessingSettings ProcessingSettings { get; init; } = new();
    public LoggingSettings LoggingSettings { get; init; } = new();
    public ExtensibilitySettings ExtensibilitySettings { get; init; } = new();
    public Dictionary<string, object> CustomSettings { get; init; } = new();

    public T GetCustomSetting<T>(string key, T defaultValue = default)
    {
        return CustomSettings.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue 
            : defaultValue;
    }

    public void SetCustomSetting<T>(string key, T value)
    {
        CustomSettings[key] = value;
    }

    public static GildedRoseConfiguration CreateDefault() => new();

    public static GildedRoseConfiguration CreateHighPerformance() => new()
    {
        ProcessingSettings = new ProcessingSettings
        {
            EnableAsyncProcessing = true,
            EnableParallelProcessing = true,
            MaxConcurrencyLevel = Environment.ProcessorCount * 2
        },
        LoggingSettings = new LoggingSettings
        {
            EnableLogging = false,
            LogItemChanges = false,
            LogPerformanceMetrics = false
        }
    };

    public static GildedRoseConfiguration CreateDevelopment() => new()
    {
        LoggingSettings = new LoggingSettings
        {
            EnableLogging = true,
            LogItemChanges = true,
            LogPerformanceMetrics = true,
            MinimumLogLevel = LogLevel.Debug
        },
        ExtensibilitySettings = new ExtensibilitySettings
        {
            EnablePluginSystem = true,
            EnableEventHooks = true,
            EnableCustomStrategies = true
        }
    };
}