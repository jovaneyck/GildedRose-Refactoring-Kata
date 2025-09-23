using System;
using System.Collections.Generic;

namespace GildedRoseKata.Core.Interfaces;

public interface IGildedRose
{
    void UpdateQuality();
    void UpdateQualityAsync();
    IEnumerable<IItem> Items { get; }
    event EventHandler<QualityUpdateEventArgs> QualityUpdated;
    event EventHandler<QualityUpdateEventArgs> BeforeQualityUpdate;
    event EventHandler<QualityUpdateEventArgs> AfterQualityUpdate;
}

public class QualityUpdateEventArgs : EventArgs
{
    public IItem Item { get; init; }
    public string UpdateReason { get; init; }
    public DateTime UpdateTimestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

public interface IQualityUpdateStrategy
{
    bool CanHandle(IItem item);
    void UpdateQuality(IItem item);
    int Priority { get; }
    string StrategyName { get; }
}

public interface IQualityUpdateStrategyFactory
{
    IQualityUpdateStrategy CreateStrategy(IItem item);
    IEnumerable<IQualityUpdateStrategy> GetAllStrategies();
    void RegisterStrategy(IQualityUpdateStrategy strategy);
    void UnregisterStrategy(string strategyName);
}

public interface IQualityUpdatePipeline
{
    void ProcessItem(IItem item);
    void AddPreProcessor(IItemPreProcessor preProcessor);
    void AddPostProcessor(IItemPostProcessor postProcessor);
    void RemovePreProcessor(string name);
    void RemovePostProcessor(string name);
}

public interface IItemPreProcessor
{
    string Name { get; }
    int Priority { get; }
    bool ShouldProcess(IItem item);
    void PreProcess(IItem item);
}

public interface IItemPostProcessor
{
    string Name { get; }
    int Priority { get; }
    bool ShouldProcess(IItem item);
    void PostProcess(IItem item);
}

public interface IQualityUpdateContext
{
    IItem CurrentItem { get; }
    Dictionary<string, object> Properties { get; }
    bool IsCancelled { get; set; }
    void AddProperty(string key, object value);
    T GetProperty<T>(string key);
}