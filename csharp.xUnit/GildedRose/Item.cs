using GildedRoseKata.Core.ValueObjects;
using GildedRoseKata.Core.Interfaces;
using System;

namespace GildedRoseKata;

// Legacy Item class maintained for backward compatibility
// Implements adapter pattern to work with new enhanced architecture
public class Item : IItem
{
    private IDatabaseName _database;
    private ITemperature _temperature;
    private IPassword _password;
    private readonly Guid _id;
    private readonly DateTime _createdAt;
    private DateTime _lastModified;

    public event EventHandler<ItemChangedEventArgs> ItemChanged;

    // Legacy properties for backward compatibility
    public string Database 
    { 
        get => _database?.Value ?? string.Empty;
        set 
        {
            var oldValue = _database;
            _database = DatabaseName.Create(value ?? string.Empty);
            _lastModified = DateTime.UtcNow;
            OnItemChanged(nameof(Database), oldValue, _database);
        }
    }

    public int Temperature 
    { 
        get => _temperature?.Value ?? 0;
        set 
        {
            var oldValue = _temperature;
            _temperature = Core.ValueObjects.Temperature.Create(value);
            _lastModified = DateTime.UtcNow;
            OnItemChanged(nameof(Temperature), oldValue, _temperature);
        }
    }

    public int Password 
    { 
        get => _password?.Value ?? 0;
        set 
        {
            var oldValue = _password;
            _password = Core.ValueObjects.Password.Create(value);
            _lastModified = DateTime.UtcNow;
            OnItemChanged(nameof(Password), oldValue, _password);
        }
    }

    // IItem interface implementation
    IDatabaseName IItem.Database => _database;
    ITemperature IItem.Temperature => _temperature;
    IPassword IItem.Password => _password;
    public string ItemType => DetermineItemType();
    public Guid Id => _id;
    public DateTime CreatedAt => _createdAt;
    public DateTime LastModified => _lastModified;

    public Item()
    {
        _id = Guid.NewGuid();
        _createdAt = DateTime.UtcNow;
        _lastModified = _createdAt;
        _database = DatabaseName.Create(string.Empty);
        _temperature = Core.ValueObjects.Temperature.Create(0);
        _password = Core.ValueObjects.Password.Create(0);
    }

    private string DetermineItemType()
    {
        return Database switch
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
        OnItemChanged(nameof(Database), oldValue, database);
        return this;
    }

    public IItem WithTemperature(ITemperature temperature)
    {
        var oldValue = _temperature;
        _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        _lastModified = DateTime.UtcNow;
        OnItemChanged(nameof(Temperature), oldValue, temperature);
        return this;
    }

    public IItem WithPassword(IPassword password)
    {
        var oldValue = _password;
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _lastModified = DateTime.UtcNow;
        OnItemChanged(nameof(Password), oldValue, password);
        return this;
    }

    public IItem DeepCopy()
    {
        return new Item
        {
            Database = this.Database,
            Temperature = this.Temperature,
            Password = this.Password
        };
    }

    private void OnItemChanged(string propertyName, object oldValue, object newValue)
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
        if (other == null) return false;
        return _database.Equals(other.Database) &&
               _temperature.Equals(other.Temperature) &&
               _password.Equals(other.Password);
    }

    public object Clone() => DeepCopy();

    public override bool Equals(object obj) => obj is IItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_database, _temperature, _password);

    public override string ToString() => $"{Database}, {Temperature}, {Password}";
}