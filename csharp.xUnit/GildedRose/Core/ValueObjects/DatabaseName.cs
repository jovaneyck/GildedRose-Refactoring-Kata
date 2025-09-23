using System;
using System.Collections.Generic;
using System.Linq;

namespace GildedRoseKata.Core.ValueObjects;

public interface IDatabaseName : IEquatable<IDatabaseName>, IComparable<IDatabaseName>
{
    string Value { get; }
    int Length { get; }
    bool IsEmpty { get; }
    bool IsValidForProcessing { get; }
    IDatabaseName Transform(Func<string, string> transformer);
    bool MatchesPattern(string pattern);
    event EventHandler<DatabaseNameChangedEventArgs> ValueChanged;
}

public class DatabaseNameChangedEventArgs : EventArgs
{
    public string OldValue { get; init; }
    public string NewValue { get; init; }
    public DateTime ChangeTimestamp { get; init; } = DateTime.UtcNow;
}

public sealed class DatabaseName : IDatabaseName
{
    private readonly string _value;
    private readonly Lazy<int> _hashCode;
    private readonly ISet<string> _validationRules;
    
    public event EventHandler<DatabaseNameChangedEventArgs> ValueChanged;

    public string Value => _value;
    public int Length => _value?.Length ?? 0;
    public bool IsEmpty => string.IsNullOrWhiteSpace(_value);
    public bool IsValidForProcessing => !IsEmpty && _validationRules.All(rule => ValidateRule(rule));

    private DatabaseName(string value, ISet<string> validationRules = null)
    {
        _value = value ?? string.Empty;
        _validationRules = validationRules ?? new HashSet<string>();
        _hashCode = new Lazy<int>(() => StringComparer.OrdinalIgnoreCase.GetHashCode(_value));
    }

    public static implicit operator string(DatabaseName databaseName) => databaseName?._value ?? string.Empty;
    public static implicit operator DatabaseName(string value) => Create(value);

    public static DatabaseName Create(string value) => new(value);
    public static DatabaseName CreateWithValidation(string value, params string[] validationRules) 
        => new(value, new HashSet<string>(validationRules));

    public IDatabaseName Transform(Func<string, string> transformer)
    {
        if (transformer == null) throw new ArgumentNullException(nameof(transformer));
        var oldValue = _value;
        var newValue = transformer(_value);
        var result = new DatabaseName(newValue, _validationRules);
        ValueChanged?.Invoke(this, new DatabaseNameChangedEventArgs { OldValue = oldValue, NewValue = newValue });
        return result;
    }

    public bool MatchesPattern(string pattern) => 
        !string.IsNullOrEmpty(pattern) && _value?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true;

    private bool ValidateRule(string rule) => rule switch
    {
        "non-empty" => !IsEmpty,
        "ascii-only" => _value.All(c => c < 128),
        _ => true
    };

    public bool Equals(IDatabaseName other) => 
        other != null && string.Equals(_value, other.Value, StringComparison.OrdinalIgnoreCase);

    public int CompareTo(IDatabaseName other) => 
        string.Compare(_value, other?.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object obj) => obj is IDatabaseName other && Equals(other);
    public override int GetHashCode() => _hashCode.Value;
    public override string ToString() => _value ?? string.Empty;

    public static bool operator ==(DatabaseName left, DatabaseName right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) == true);
    public static bool operator !=(DatabaseName left, DatabaseName right) => !(left == right);
}