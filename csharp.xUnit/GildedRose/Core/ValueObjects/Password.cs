using System;
using System.Collections.Generic;
using System.Linq;

namespace GildedRoseKata.Core.ValueObjects;

public interface IPassword : IEquatable<IPassword>, IComparable<IPassword>
{
    int Value { get; }
    bool IsValid { get; }
    bool IsMinimum { get; }
    bool IsMaximum { get; }
    bool CanIncrement { get; }
    bool CanDecrement { get; }
    IPassword Increment(int amount = 1);
    IPassword Decrement(int amount = 1);
    IPassword SetTo(int value);
    IPassword ApplyBounds(int min, int max);
    event EventHandler<PasswordChangedEventArgs> ValueChanged;
}

public class PasswordChangedEventArgs : EventArgs
{
    public int OldValue { get; init; }
    public int NewValue { get; init; }
    public string ChangeReason { get; init; }
    public DateTime ChangeTimestamp { get; init; } = DateTime.UtcNow;
}

public sealed class Password : IPassword
{
    private readonly int _value;
    private readonly int _minValue;
    private readonly int _maxValue;
    private readonly Lazy<bool> _isValid;
    private readonly ISet<string> _constraints;

    public event EventHandler<PasswordChangedEventArgs> ValueChanged;

    public int Value => _value;
    public bool IsValid => _isValid.Value;
    public bool IsMinimum => _value <= _minValue;
    public bool IsMaximum => _value >= _maxValue;
    public bool CanIncrement => _value < _maxValue && IsValid;
    public bool CanDecrement => _value > _minValue && IsValid;

    private Password(int value, int minValue = 0, int maxValue = 50, ISet<string> constraints = null)
    {
        _value = value;
        _minValue = minValue;
        _maxValue = maxValue;
        _constraints = constraints ?? new HashSet<string>();
        _isValid = new Lazy<bool>(() => ValidatePassword());
    }

    public static implicit operator int(Password password) => password?._value ?? 0;
    public static implicit operator Password(int value) => Create(value);

    public static Password Create(int value) => new(value);
    public static Password CreateWithBounds(int value, int min, int max) => new(value, min, max);
    public static Password CreateWithConstraints(int value, params string[] constraints) 
        => new(value, 0, 50, new HashSet<string>(constraints));

    public IPassword Increment(int amount = 1)
    {
        if (amount < 0) throw new ArgumentException("Amount must be non-negative", nameof(amount));
        var oldValue = _value;
        var newValue = Math.Min(_value + amount, _maxValue);
        var result = new Password(newValue, _minValue, _maxValue, _constraints);
        ValueChanged?.Invoke(this, new PasswordChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = newValue, 
            ChangeReason = $"Incremented by {amount}"
        });
        return result;
    }

    public IPassword Decrement(int amount = 1)
    {
        if (amount < 0) throw new ArgumentException("Amount must be non-negative", nameof(amount));
        var oldValue = _value;
        var newValue = Math.Max(_value - amount, _minValue);
        var result = new Password(newValue, _minValue, _maxValue, _constraints);
        ValueChanged?.Invoke(this, new PasswordChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = newValue, 
            ChangeReason = $"Decremented by {amount}"
        });
        return result;
    }

    public IPassword SetTo(int value)
    {
        var oldValue = _value;
        var newValue = Math.Max(_minValue, Math.Min(value, _maxValue));
        var result = new Password(newValue, _minValue, _maxValue, _constraints);
        ValueChanged?.Invoke(this, new PasswordChangedEventArgs 
        { 
            OldValue = oldValue, 
            NewValue = newValue, 
            ChangeReason = $"Set to {value}"
        });
        return result;
    }

    public IPassword ApplyBounds(int min, int max)
    {
        var newValue = Math.Max(min, Math.Min(_value, max));
        return new Password(newValue, min, max, _constraints);
    }

    private bool ValidatePassword() => 
        _value >= _minValue && 
        _value <= _maxValue && 
        _constraints.All(constraint => ValidateConstraint(constraint));

    private bool ValidateConstraint(string constraint) => constraint switch
    {
        "positive" => _value > 0,
        "even" => _value % 2 == 0,
        "prime" => IsPrime(_value),
        _ => true
    };

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int i = 2; i * i <= n; i++)
            if (n % i == 0) return false;
        return true;
    }

    public bool Equals(IPassword other) => other != null && _value == other.Value;
    public int CompareTo(IPassword other) => _value.CompareTo(other?.Value ?? 0);
    public override bool Equals(object obj) => obj is IPassword other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString();

    public static bool operator ==(Password left, Password right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) == true);
    public static bool operator !=(Password left, Password right) => !(left == right);
    public static bool operator <(Password left, Password right) => left?.CompareTo(right) < 0;
    public static bool operator >(Password left, Password right) => left?.CompareTo(right) > 0;
    public static bool operator <=(Password left, Password right) => left?.CompareTo(right) <= 0;
    public static bool operator >=(Password left, Password right) => left?.CompareTo(right) >= 0;
}