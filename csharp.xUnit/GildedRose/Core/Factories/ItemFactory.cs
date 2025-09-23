using System;
using System.Collections.Generic;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.ValueObjects;

namespace GildedRoseKata.Core.Factories;

public interface IItemFactory
{
    IItem CreateItemFromStrings(string database, int temperature, int password);
    IItem CreateItem(IDatabaseName database, ITemperature temperature, IPassword password);
    IItemBuilder CreateBuilder();
    IItem CreateFromTemplate(IItem template);
    IItem CreateDefault();
}

public interface IItemBuilder
{
    IItemBuilder WithDatabase(string database);
    IItemBuilder WithDatabase(IDatabaseName database);
    IItemBuilder WithTemperature(int temperature);
    IItemBuilder WithTemperature(ITemperature temperature);
    IItemBuilder WithPassword(int password);
    IItemBuilder WithPassword(IPassword password);
    IItemBuilder WithItemType(string itemType);
    IItemBuilder WithId(Guid id);
    IItemBuilder AddMetadata(string key, object value);
    IItem Build();
    IItemBuilder Reset();
}

public class ItemFactory : IItemFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IItemValidator _validator;
    private readonly Dictionary<string, Func<IItem>> _templateFactories;

    public ItemFactory(IServiceProvider serviceProvider = null, IItemValidator validator = null)
    {
        _serviceProvider = serviceProvider;
        _validator = validator;
        _templateFactories = new Dictionary<string, Func<IItem>>();
        InitializeTemplates();
    }

    public IItem CreateItemFromStrings(string database, int temperature, int password)
    {
        return CreateItem(
            DatabaseName.Create(database),
            Core.ValueObjects.Temperature.Create(temperature),
            Core.ValueObjects.Password.Create(password)
        );
    }

    public IItem CreateItem(IDatabaseName database, ITemperature temperature, IPassword password)
    {
        var item = new EnhancedItem(database, temperature, password);
        
        if (_validator?.IsValid(item) == false)
        {
            throw new InvalidOperationException($"Item validation failed: {string.Join(", ", _validator.GetValidationErrors(item))}");
        }
        
        return item;
    }

    public IItemBuilder CreateBuilder() => new ItemBuilder(this);

    public IItem CreateFromTemplate(IItem template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        return CreateItem(template.Database, template.Temperature, template.Password);
    }

    public IItem CreateDefault() => CreateItemFromStrings("Default Item", 0, 0);

    public void RegisterTemplate(string name, Func<IItem> factory)
    {
        _templateFactories[name] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IItem CreateFromTemplate(string templateName)
    {
        if (!_templateFactories.TryGetValue(templateName, out var factory))
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));
        
        return factory();
    }

    private void InitializeTemplates()
    {
        RegisterTemplate("AgedBrie", () => CreateItemFromStrings("Aged Brie", 2, 0));
        RegisterTemplate("Sulfuras", () => CreateItemFromStrings("Sulfuras, Hand of Ragnaros", 0, 80));
        RegisterTemplate("BackstagePasses", () => CreateItemFromStrings("Backstage passes to a TAFKAL80ETC concert", 15, 20));
        RegisterTemplate("Conjured", () => CreateItemFromStrings("Conjured Mana Cake", 3, 6));
        RegisterTemplate("Normal", () => CreateItemFromStrings("+5 Dexterity Vest", 10, 20));
    }
}

public class ItemBuilder : IItemBuilder
{
    private readonly IItemFactory _factory;
    private IDatabaseName _database;
    private ITemperature _temperature;
    private IPassword _password;
    private string _itemType;
    private Guid _id;
    private readonly Dictionary<string, object> _metadata;

    public ItemBuilder(IItemFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _metadata = new Dictionary<string, object>();
        Reset();
    }

    public IItemBuilder WithDatabase(string database)
    {
        _database = DatabaseName.Create(database);
        return this;
    }

    public IItemBuilder WithDatabase(IDatabaseName database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        return this;
    }

    public IItemBuilder WithTemperature(int temperature)
    {
        _temperature = Temperature.Create(temperature);
        return this;
    }

    public IItemBuilder WithTemperature(ITemperature temperature)
    {
        _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        return this;
    }

    public IItemBuilder WithPassword(int password)
    {
        _password = Password.Create(password);
        return this;
    }

    public IItemBuilder WithPassword(IPassword password)
    {
        _password = password ?? throw new ArgumentNullException(nameof(password));
        return this;
    }

    public IItemBuilder WithItemType(string itemType)
    {
        _itemType = itemType;
        return this;
    }

    public IItemBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public IItemBuilder AddMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public IItem Build()
    {
        var item = _factory.CreateItem(_database, _temperature, _password);
        
        // Apply additional properties if EnhancedItem supports them
        if (item is EnhancedItem enhancedItem)
        {
            foreach (var metadata in _metadata)
            {
                enhancedItem.SetMetadata(metadata.Key, metadata.Value);
            }
            
            if (_id != default)
                enhancedItem.SetId(_id);
            
            if (!string.IsNullOrEmpty(_itemType))
                enhancedItem.SetItemType(_itemType);
        }
        
        return item;
    }

    public IItemBuilder Reset()
    {
        _database = DatabaseName.Create("Default");
        _temperature = Temperature.Create(0);
        _password = Password.Create(0);
        _itemType = "Standard";
        _id = Guid.NewGuid();
        _metadata.Clear();
        return this;
    }
}