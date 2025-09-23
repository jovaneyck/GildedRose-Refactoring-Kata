using System;
using System.Collections.Generic;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.ValueObjects;

namespace GildedRoseKata.Core.Factories;

public class EnhancedItem : IItem
{
    private IDatabaseName _database;
    private ITemperature _temperature;
    private IPassword _password;
    private string _itemType;
    private Guid _id;
    private readonly DateTime _createdAt;
    private DateTime _lastModified;
    private readonly Dictionary<string, object> _metadata;

    public event EventHandler<ItemChangedEventArgs> ItemChanged;

    public IDatabaseName Database => _database;
    public ITemperature Temperature => _temperature;
    public IPassword Password => _password;
    public string ItemType => _itemType;
    public Guid Id => _id;
    public DateTime CreatedAt => _createdAt;
    public DateTime LastModified => _lastModified;

    public EnhancedItem(IDatabaseName database, ITemperature temperature, IPassword password)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _itemType = DetermineItemType(database);
        _id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
        _lastModified = _createdAt;
        _metadata = new Dictionary<string, object>();
        
        SubscribeToValueObjectEvents();
    }

    private void SubscribeToValueObjectEvents()
    {
        if (_database != null)
            _database.ValueChanged += (s, e) => OnPropertyChanged(nameof(Database), e.OldValue, e.NewValue);
        
        if (_temperature != null)
            _temperature.ValueChanged += (s, e) => OnPropertyChanged(nameof(Temperature), e.OldValue, e.NewValue);
        
        if (_password != null)
            _password.ValueChanged += (s, e) => OnPropertyChanged(nameof(Password), e.OldValue, e.NewValue);
    }

    private string DetermineItemType(IDatabaseName database)
    {
        return database?.Value switch
        {
            "Aged Brie" => "Legendary",
            "Sulfuras, Hand of Ragnaros" => "Artifact",
            var name when name?.Contains("Backstage passes") == true => "Event",
            var name when name?.Contains("Conjured") == true => "Magical",
            _ => "Standard"
        };
    }

    public IItem WithDatabase(IDatabaseName database)
    {
        var oldValue = _database;
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged(nameof(Database), oldValue, database);
        return this;
    }

    public IItem WithTemperature(ITemperature temperature)
    {
        var oldValue = _temperature;
        _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged(nameof(Temperature), oldValue, temperature);
        return this;
    }

    public IItem WithPassword(IPassword password)
    {
        var oldValue = _password;
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged(nameof(Password), oldValue, password);
        return this;
    }

    public IItem DeepCopy()
    {
        var copy = new EnhancedItem(
            DatabaseName.Create(_database.Value),
            Core.ValueObjects.Temperature.Create(_temperature.Value),
            Core.ValueObjects.Password.Create(_password.Value)
        );
        
        copy._itemType = _itemType;
        copy._id = _id; // Keep same ID for deep copy
        
        foreach (var metadata in _metadata)
        {
            copy._metadata[metadata.Key] = metadata.Value;
        }
        
        return copy;
    }

    public void SetMetadata(string key, object value)
    {
        var oldValue = _metadata.TryGetValue(key, out var existing) ? existing : null;
        _metadata[key] = value;
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged($"Metadata.{key}", oldValue, value);
    }

    public T GetMetadata<T>(string key)
    {
        return _metadata.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default(T);
    }

    public void SetId(Guid id)
    {
        var oldValue = _id;
        _id = id;
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged(nameof(Id), oldValue, id);
    }

    public void SetItemType(string itemType)
    {
        var oldValue = _itemType;
        _itemType = itemType;
        _lastModified = DateTime.UtcNow;
        OnPropertyChanged(nameof(ItemType), oldValue, itemType);
    }

    private void OnPropertyChanged(string propertyName, object oldValue, object newValue)
    {
        ItemChanged?.Invoke(this, new ItemChangedEventArgs
        {
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    public bool Equals(IItem other)
    {
        return other != null &&
               _id == other.Id &&
               _database.Equals(other.Database) &&
               _temperature.Equals(other.Temperature) &&
               _password.Equals(other.Password);
    }

    public object Clone() => DeepCopy();

    public override bool Equals(object obj) => obj is IItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_id, _database, _temperature, _password);

    public override string ToString() => $"{_database.Value}, {_temperature.Value}, {_password.Value}";
}