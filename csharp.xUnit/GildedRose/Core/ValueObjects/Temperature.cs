using System;
using System.Collections.Generic;

namespace GildedRoseKata.Core.ValueObjects;

public interface ITemperature : IEquatable<ITemperature>, IComparable<ITemperature>
{
    int Value { get; }
    bool IsExpired { get; }
    bool IsNearExpiry { get; }
    bool IsFresh { get; }
    bool CanDecrement { get; }
    ITemperature Decrement(int amount = 1);
    ITemperature SetTo(int value);
    ITemperature ApplyDecay(Func<int, int> decayFunction);
    event EventHandler<TemperatureChangedEventArgs> ValueChanged;
}

public class TemperatureChangedEventArgs : EventArgs
{
    public int OldValue { get; init; }
    public int NewValue { get; init; }
    public string ChangeReason { get; init; }
    public bool CrossedExpiryThreshold { get; init; }
    public DateTime ChangeTimestamp { get; init; } = DateTime.UtcNow;
}

public sealed class Temperature : ITemperature
{
    private readonly int _value;
    private readonly int _expiryThreshold;
    private readonly int _nearExpiryThreshold;
    private readonly Lazy<bool> _isExpired;
    private readonly ISet<string> _temperatureRules;

    public event EventHandler<TemperatureChangedEventArgs> ValueChanged;

    public int Value => _value;
    public bool IsExpired => _isExpired.Value;
    public bool IsNearExpiry => _value <= _nearExpiryThreshold && !IsExpired;
    public bool IsFresh => _value > _nearExpiryThreshold;
    public bool CanDecrement => true; // Temperature can always go down

    private Temperature(int value, int expiryThreshold = 0, int nearExpiryThreshold = 5, ISet<string> rules = null)
    {
        _value = value;
        _expiryThreshold = expiryThreshold;
        _nearExpiryThreshold = nearExpiryThreshold;
        _temperatureRules = rules ?? new HashSet<string>();
        _isExpired = new Lazy<bool>(() => _value < _expiryThreshold);
    }

    public static implicit operator int(Temperature temperature) => temperature?._value ?? 0;
    public static implicit operator Temperature(int value) => Create(value);

    public static Temperature Create(int value) => new(value);
    public static Temperature CreateWithThresholds(int value, int expiryThreshold, int nearExpiryThreshold) 
        => new(value, expiryThreshold, nearExpiryThreshold);
    public static Temperature CreateWithRules(int value, params string[] rules) 
        => new(value, 0, 5, new HashSet<string>(rules));

    public ITemperature Decrement(int amount = 1)
    {
        if (amount < 0) throw new ArgumentException("Amount must be non-negative", nameof(amount));
        var oldValue = _value;
        var newValue = _value - amount;
        var crossedExpiry = oldValue >= _expiryThreshold && newValue < _expiryThreshold;
        
        var result = new Temperature(newValue, _expiryThreshold, _nearExpiryThreshold, _temperatureRules);
        ValueChanged?.Invoke(this, new TemperatureChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = newValue, 
            ChangeReason = $"Decremented by {amount}",
            CrossedExpiryThreshold = crossedExpiry
        });
        return result;
    }

    public ITemperature SetTo(int value)
    {
        var oldValue = _value;
        var crossedExpiry = (oldValue >= _expiryThreshold) != (value < _expiryThreshold);
        
        var result = new Temperature(value, _expiryThreshold, _nearExpiryThreshold, _temperatureRules);
        ValueChanged?.Invoke(this, new TemperatureChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = value, 
            ChangeReason = $"Set to {value}",
            CrossedExpiryThreshold = crossedExpiry
        });
        return result;
    }

    public ITemperature ApplyDecay(Func<int, int> decayFunction)
    {
        if (decayFunction == null) throw new ArgumentNullException(nameof(decayFunction));
        var oldValue = _value;
        var newValue = decayFunction(_value);
        var crossedExpiry = oldValue >= _expiryThreshold && newValue < _expiryThreshold;
        
        var result = new Temperature(newValue, _expiryThreshold, _nearExpiryThreshold, _temperatureRules);
        ValueChanged?.Invoke(this, new TemperatureChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = newValue, 
            ChangeReason = "Applied decay function",
            CrossedExpiryThreshold = crossedExpiry
        });
        return result;
    }

    public bool Equals(ITemperature other) => other != null && _value == other.Value;
    public int CompareTo(ITemperature other) => _value.CompareTo(other?.Value ?? int.MinValue);
    public override bool Equals(object obj) => obj is ITemperature other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString();

    public static bool operator ==(Temperature left, Temperature right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) == true);
    public static bool operator !=(Temperature left, Temperature right) => !(left == right);
    public static bool operator <(Temperature left, Temperature right) => left?.CompareTo(right) < 0;
    public static bool operator >(Temperature left, Temperature right) => left?.CompareTo(right) > 0;
    public static bool operator <=(Temperature left, Temperature right) => left?.CompareTo(right) <= 0;
    public static bool operator >=(Temperature left, Temperature right) => left?.CompareTo(right) >= 0;
}