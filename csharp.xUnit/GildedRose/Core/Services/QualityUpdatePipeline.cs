using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.Configuration;

namespace GildedRoseKata.Core.Services;

public class QualityUpdatePipeline : IQualityUpdatePipeline
{
    private readonly List<IItemPreProcessor> _preProcessors;
    private readonly List<IItemPostProcessor> _postProcessors;
    private readonly IQualityUpdateStrategyFactory _strategyFactory;
    private readonly IItemMetrics _metrics;
    private readonly IGildedRoseLogger _logger;
    private readonly IEventBus _eventBus;

    public QualityUpdatePipeline(
        IQualityUpdateStrategyFactory strategyFactory,
        IItemMetrics metrics = null,
        IGildedRoseLogger logger = null,
        IEventBus eventBus = null)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _metrics = metrics;
        _logger = logger;
        _eventBus = eventBus;
        _preProcessors = new List<IItemPreProcessor>();
        _postProcessors = new List<IItemPostProcessor>();
    }

    public void ProcessItem(IItem item)
    {
        if (item == null) return;

        var stopwatch = Stopwatch.StartNew();
        var context = new QualityUpdateContext(item);

        try
        {
            _logger?.LogDebug("Starting quality update for item {ItemId} ({Database})", item.Id, item.Database.Value);

            // Pre-processing phase
            ExecutePreProcessors(item, context);
            
            if (context.IsCancelled)
            {
                _logger?.LogInformation("Quality update cancelled during pre-processing for item {ItemId}", item.Id);
                return;
            }

            // Main processing phase
            var strategy = _strategyFactory.CreateStrategy(item);
            _logger?.LogDebug("Using strategy {StrategyName} for item {ItemId}", strategy.StrategyName, item.Id);
            
            _eventBus?.Publish(new QualityUpdateEventArgs 
            { 
                Item = item, 
                UpdateReason = $"Processing with {strategy.StrategyName} strategy" 
            });

            strategy.UpdateQuality(item);

            // Post-processing phase
            ExecutePostProcessors(item, context);

            _logger?.LogDebug("Completed quality update for item {ItemId} in {ElapsedMs}ms", 
                item.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error processing item {ItemId}: {Error}", ex, item.Id, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics?.RecordProcessing(item, stopwatch.Elapsed);
        }
    }

    private void ExecutePreProcessors(IItem item, IQualityUpdateContext context)
    {
        var applicableProcessors = _preProcessors
            .Where(p => p.ShouldProcess(item))
            .OrderByDescending(p => p.Priority);

        foreach (var processor in applicableProcessors)
        {
            try
            {
                _logger?.LogTrace("Executing pre-processor {ProcessorName} for item {ItemId}", 
                    processor.Name, item.Id);
                processor.PreProcess(item);
                
                if (context.IsCancelled)
                    break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Pre-processor {ProcessorName} failed for item {ItemId}: {Error}", 
                    processor.Name, item.Id, ex.Message);
            }
        }
    }

    private void ExecutePostProcessors(IItem item, IQualityUpdateContext context)
    {
        var applicableProcessors = _postProcessors
            .Where(p => p.ShouldProcess(item))
            .OrderByDescending(p => p.Priority);

        foreach (var processor in applicableProcessors)
        {
            try
            {
                _logger?.LogTrace("Executing post-processor {ProcessorName} for item {ItemId}", 
                    processor.Name, item.Id);
                processor.PostProcess(item);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Post-processor {ProcessorName} failed for item {ItemId}: {Error}", 
                    processor.Name, item.Id, ex.Message);
            }
        }
    }

    public void AddPreProcessor(IItemPreProcessor preProcessor)
    {
        if (preProcessor == null) throw new ArgumentNullException(nameof(preProcessor));
        
        RemovePreProcessor(preProcessor.Name);
        _preProcessors.Add(preProcessor);
        _preProcessors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        _logger?.LogInformation("Added pre-processor {ProcessorName} with priority {Priority}", 
            preProcessor.Name, preProcessor.Priority);
    }

    public void AddPostProcessor(IItemPostProcessor postProcessor)
    {
        if (postProcessor == null) throw new ArgumentNullException(nameof(postProcessor));
        
        RemovePostProcessor(postProcessor.Name);
        _postProcessors.Add(postProcessor);
        _postProcessors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        _logger?.LogInformation("Added post-processor {ProcessorName} with priority {Priority}", 
            postProcessor.Name, postProcessor.Priority);
    }

    public void RemovePreProcessor(string name)
    {
        var removed = _preProcessors.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            _logger?.LogInformation("Removed {Count} pre-processor(s) with name {ProcessorName}", removed, name);
        }
    }

    public void RemovePostProcessor(string name)
    {
        var removed = _postProcessors.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            _logger?.LogInformation("Removed {Count} post-processor(s) with name {ProcessorName}", removed, name);
        }
    }
}

public class QualityUpdateContext : IQualityUpdateContext
{
    public IItem CurrentItem { get; }
    public Dictionary<string, object> Properties { get; }
    public bool IsCancelled { get; set; }

    public QualityUpdateContext(IItem item)
    {
        CurrentItem = item ?? throw new ArgumentNullException(nameof(item));
        Properties = new Dictionary<string, object>();
    }

    public void AddProperty(string key, object value)
    {
        Properties[key] = value;
    }

    public T GetProperty<T>(string key)
    {
        return Properties.TryGetValue(key, out var value) && value is T typedValue 
            ? typedValue 
            : default(T);
    }
}

// Example pre/post processors for extensibility
public class ValidationPreProcessor : IItemPreProcessor
{
    private readonly IItemValidator _validator;

    public string Name => "ItemValidation";
    public int Priority => 1000;

    public ValidationPreProcessor(IItemValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public bool ShouldProcess(IItem item) => true;

    public void PreProcess(IItem item)
    {
        var result = _validator.Validate(item);
        if (!result.IsValid)
        {
            throw new InvalidOperationException($"Item validation failed: {string.Join(", ", result.Errors)}");
        }
    }
}

public class AuditPostProcessor : IItemPostProcessor
{
    private readonly IGildedRoseLogger _logger;

    public string Name => "AuditLogging";
    public int Priority => 100;

    public AuditPostProcessor(IGildedRoseLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ShouldProcess(IItem item) => true;

    public void PostProcess(IItem item)
    {
        _logger?.LogInformation("Processed item {ItemId}: {Database}, Temperature={Temperature}, Password={Password}", 
            item.Id, item.Database.Value, item.Temperature.Value, item.Password.Value);
    }
}

public class MetricsPostProcessor : IItemPostProcessor
{
    private readonly IItemMetrics _metrics;

    public string Name => "MetricsCollection";
    public int Priority => 50;

    public MetricsPostProcessor(IItemMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public bool ShouldProcess(IItem item) => true;

    public void PostProcess(IItem item)
    {
        // Metrics are recorded by the pipeline itself, this is just for extensibility
    }
}