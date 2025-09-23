# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build
```cmd
dotnet build GildedRose.sln -c Debug
```

### Run Tests

When making changes to the code, ALWAYS make sure the tests pass as a final step in every plan.

```cmd
dotnet test --no-restore
```

### Run Application
```cmd
GildedRose/bin/Debug/net8.0/GildedRose 10
```
(Replace `10` with desired number of days to simulate)

### Run code complexity checks

```cmd
scc --by-file --wide --sort complexity --no-duplicates
```
Anything below 10 is good, 10-20 is moderate, above 20 is considered too complex.

## Architecture

This is the Gilded Rose Refactoring Kata implemented in C# with .NET 8.0 and xUnit testing framework.

### Project Structure
- **GildedRose/** - Main application project containing the core business logic
- **GildedRoseTests/** - Test project using xUnit and Verify.Xunit for approval testing

### Core Components
- `Item.cs` - Simple data class with Name, SellIn, and Quality properties
- `GildedRose.cs` - Main business logic class with `UpdateQuality()` method that handles item aging rules
- `Program.cs` - Console application entry point that simulates multiple days of item updates

### Testing Approach
Uses approval testing with Verify.Xunit to capture the complete output of running the simulation for 30 days. The test `ApprovalTest.ThirtyDays()` verifies that the behavior remains consistent across changes.

### Refactoring Context
This is a legacy codebase designed for refactoring practice. The `UpdateQuality()` method contains complex nested conditionals that handle different item types with varying aging rules:
- Normal items decrease in quality over time
- "Aged Brie" increases in quality with age
- "Sulfuras" never changes
- "Backstage passes" have complex rules based on concert proximity
- "Conjured" items (mentioned but not fully implemented) should degrade twice as fast

When refactoring, preserve the existing behavior as verified by the approval tests.