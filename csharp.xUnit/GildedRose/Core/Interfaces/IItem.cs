using System;
using System.Collections.Generic;
using System.Linq;
using GildedRoseKata.Core.ValueObjects;

namespace GildedRoseKata.Core.Interfaces;

public interface IItem : IEquatable<IItem>, ICloneable
{
    IDatabaseName Database { get; }
    ITemperature Temperature { get; }
    IPassword Password { get; }
    string ItemType { get; }
    Guid Id { get; }
    DateTime CreatedAt { get; }
    DateTime LastModified { get; }
    
    IItem WithDatabase(IDatabaseName database);
    IItem WithTemperature(ITemperature temperature);
    IItem WithPassword(IPassword password);
    IItem DeepCopy();
    
    event EventHandler<ItemChangedEventArgs> ItemChanged;
}

public class ItemChangedEventArgs : EventArgs
{
    public string PropertyName { get; init; }
    public object OldValue { get; init; }
    public object NewValue { get; init; }
    public DateTime ChangeTimestamp { get; init; } = DateTime.UtcNow;
}

public interface IItemRepository
{
    IItem FindById(Guid id);
    IEnumerable<IItem> FindByType(string itemType);
    IEnumerable<IItem> FindByDatabase(IDatabaseName database);
    void Save(IItem item);
    void SaveAll(IEnumerable<IItem> items);
    void Delete(Guid id);
    IEnumerable<IItem> GetAll();
}

public interface IItemValidator
{
    ValidationResult Validate(IItem item);
    bool IsValid(IItem item);
    IEnumerable<string> GetValidationErrors(IItem item);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public IEnumerable<string> Errors { get; init; } = Enumerable.Empty<string>();
    public IEnumerable<string> Warnings { get; init; } = Enumerable.Empty<string>();
}

public interface IItemMetrics
{
    int TotalProcessingCount { get; }
    TimeSpan AverageProcessingTime { get; }
    Dictionary<string, int> ProcessingCountByType { get; }
    void RecordProcessing(IItem item, TimeSpan processingTime);
    void Reset();
}