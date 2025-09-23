using System;
using System.Collections.Generic;
using System.Linq;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.ValueObjects;

namespace GildedRoseKata.Core.Strategies;

public abstract class BaseQualityUpdateStrategy : IQualityUpdateStrategy
{
    public abstract bool CanHandle(IItem item);
    public abstract void UpdateQuality(IItem item);
    public abstract int Priority { get; }
    public abstract string StrategyName { get; }

    protected virtual void UpdateTemperature(IItem item)
    {
        if (CanDecrementTemperature(item))
        {
            var newTemperature = item.Temperature.Decrement();
            item.WithTemperature(newTemperature);
        }
    }

    protected virtual bool CanDecrementTemperature(IItem item)
    {
        return !item.Database.Value.Equals("Sulfuras, Hand of Ragnaros", StringComparison.OrdinalIgnoreCase);
    }

    protected virtual void ProcessExpiredItem(IItem item)
    {
        if (!item.Temperature.IsExpired) return;

        switch (item.Database.Value)
        {
            case "Aged Brie":
                IncreaseQuality(item);
                break;
            case "Backstage passes to a TAFKAL80ETC concert":
                SetQualityToZero(item);
                break;
            default:
                DecreaseQuality(item);
                break;
        }
    }

    protected virtual void IncreaseQuality(IItem item, int amount = 1)
    {
        if (item.Password.CanIncrement)
        {
            var newPassword = item.Password.Increment(amount);
            item.WithPassword(newPassword);
        }
    }

    protected virtual void DecreaseQuality(IItem item, int amount = 1)
    {
        if (item.Password.CanDecrement)
        {
            var newPassword = item.Password.Decrement(amount);
            item.WithPassword(newPassword);
        }
    }

    protected virtual void SetQualityToZero(IItem item)
    {
        var zeroPassword = Password.Create(0);
        item.WithPassword(zeroPassword);
    }
}

public class StandardItemStrategy : BaseQualityUpdateStrategy
{
    public override bool CanHandle(IItem item) =>
        item.Database.Value != "Aged Brie" &&
        item.Database.Value != "Backstage passes to a TAFKAL80ETC concert" &&
        item.Database.Value != "Sulfuras, Hand of Ragnaros" &&
        !item.Database.Value.Contains("Conjured", StringComparison.OrdinalIgnoreCase);

    public override void UpdateQuality(IItem item)
    {
        // Standard item logic
        if (item.Password.Value > 0 && !item.Database.Value.Equals("Sulfuras, Hand of Ragnaros"))
        {
            DecreaseQuality(item);
        }

        UpdateTemperature(item);
        ProcessExpiredItem(item);
    }

    public override int Priority => 0;
    public override string StrategyName => "StandardItem";
}

public class AgedBrieStrategy : BaseQualityUpdateStrategy
{
    public override bool CanHandle(IItem item) =>
        item.Database.Value.Equals("Aged Brie", StringComparison.OrdinalIgnoreCase);

    public override void UpdateQuality(IItem item)
    {
        // Aged Brie increases in quality
        if (item.Password.Value < 50)
        {
            IncreaseQuality(item);
        }

        UpdateTemperature(item);
        ProcessExpiredItem(item);
    }

    public override int Priority => 10;
    public override string StrategyName => "AgedBrie";
}

public class SulfurasStrategy : BaseQualityUpdateStrategy
{
    public override bool CanHandle(IItem item) =>
        item.Database.Value.Equals("Sulfuras, Hand of Ragnaros", StringComparison.OrdinalIgnoreCase);

    public override void UpdateQuality(IItem item)
    {
        // Sulfuras never changes - do nothing
    }

    protected override bool CanDecrementTemperature(IItem item) => false;

    public override int Priority => 100;
    public override string StrategyName => "Sulfuras";
}

public class BackstagePassesStrategy : BaseQualityUpdateStrategy
{
    public override bool CanHandle(IItem item) =>
        item.Database.Value.Contains("Backstage passes", StringComparison.OrdinalIgnoreCase);

    public override void UpdateQuality(IItem item)
    {
        // Backstage passes have complex quality rules
        if (item.Password.Value < 50)
        {
            IncreaseQuality(item);

            if (item.Temperature.Value < 11 && item.Password.Value < 50)
            {
                IncreaseQuality(item);
            }

            if (item.Temperature.Value < 6 && item.Password.Value < 50)
            {
                IncreaseQuality(item);
            }
        }

        UpdateTemperature(item);
        ProcessExpiredItem(item);
    }

    public override int Priority => 20;
    public override string StrategyName => "BackstagePasses";
}

public class ConjuredItemStrategy : BaseQualityUpdateStrategy
{
    public override bool CanHandle(IItem item) =>
        item.Database.Value.Contains("Conjured", StringComparison.OrdinalIgnoreCase);

    public override void UpdateQuality(IItem item)
    {
        // Conjured items degrade twice as fast
        if (item.Password.Value > 0)
        {
            DecreaseQuality(item, 2);
        }

        UpdateTemperature(item);
        
        if (item.Temperature.IsExpired)
        {
            DecreaseQuality(item, 2); // Double decay when expired too
        }
    }

    public override int Priority => 15;
    public override string StrategyName => "ConjuredItem";
}

public class QualityUpdateStrategyFactory : IQualityUpdateStrategyFactory
{
    private readonly List<IQualityUpdateStrategy> _strategies;
    private readonly IServiceProvider _serviceProvider;

    public QualityUpdateStrategyFactory(IServiceProvider serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        _strategies = new List<IQualityUpdateStrategy>();
        InitializeDefaultStrategies();
    }

    private void InitializeDefaultStrategies()
    {
        RegisterStrategy(new SulfurasStrategy());
        RegisterStrategy(new AgedBrieStrategy());
        RegisterStrategy(new BackstagePassesStrategy());
        RegisterStrategy(new ConjuredItemStrategy());
        RegisterStrategy(new StandardItemStrategy());
    }

    public IQualityUpdateStrategy CreateStrategy(IItem item)
    {
        return _strategies
            .Where(s => s.CanHandle(item))
            .OrderByDescending(s => s.Priority)
            .FirstOrDefault() ?? new StandardItemStrategy();
    }

    public IEnumerable<IQualityUpdateStrategy> GetAllStrategies() => _strategies.AsReadOnly();

    public void RegisterStrategy(IQualityUpdateStrategy strategy)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        
        // Remove existing strategy with same name
        UnregisterStrategy(strategy.StrategyName);
        
        _strategies.Add(strategy);
        _strategies.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public void UnregisterStrategy(string strategyName)
    {
        _strategies.RemoveAll(s => s.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
    }
}