using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GildedRoseKata.Core.Interfaces;
using GildedRoseKata.Core.Configuration;

namespace GildedRoseKata.Core.Services;

public class EnhancedGildedRose : IGildedRose
{
    private readonly IList<IItem> _items;
    private readonly IQualityUpdatePipeline _pipeline;
    private readonly IGildedRoseConfiguration _configuration;
    private readonly IGildedRoseLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly IItemMetrics _metrics;

    public event EventHandler<QualityUpdateEventArgs> QualityUpdated;
    public event EventHandler<QualityUpdateEventArgs> BeforeQualityUpdate;
    public event EventHandler<QualityUpdateEventArgs> AfterQualityUpdate;

    public IEnumerable<IItem> Items => _items.AsReadOnly();

    public EnhancedGildedRose(
        IList<IItem> items,
        IQualityUpdatePipeline pipeline,
        IGildedRoseConfiguration configuration = null,
        IGildedRoseLogger logger = null,
        IEventBus eventBus = null,
        IItemMetrics metrics = null)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _configuration = configuration ?? GildedRoseConfiguration.CreateDefault();
        _logger = logger;
        _eventBus = eventBus;
        _metrics = metrics;

        InitializeEventHandlers();
    }

    private void InitializeEventHandlers()
    {
        if (_eventBus != null)
        {
            _eventBus.Subscribe<QualityUpdateEventArgs>(OnQualityUpdateEvent);
        }

        foreach (var item in _items)
        {
            item.ItemChanged += OnItemChanged;
        }
    }

    private void OnQualityUpdateEvent(QualityUpdateEventArgs eventArgs)
    {
        QualityUpdated?.Invoke(this, eventArgs);
    }

    private void OnItemChanged(object sender, ItemChangedEventArgs e)
    {
        _logger?.LogTrace("Item {ItemId} property {PropertyName} changed from {OldValue} to {NewValue}",
            ((IItem)sender).Id, e.PropertyName, e.OldValue, e.NewValue);
    }

    public void UpdateQuality()
    {
        _logger?.LogInformation("Starting quality update for {ItemCount} items", _items.Count);

        var startTime = DateTime.UtcNow;
        var processedCount = 0;
        var errorCount = 0;

        try
        {
            if (_configuration.ProcessingSettings.EnableParallelProcessing)
            {
                ProcessItemsInParallel();
            }
            else
            {
                ProcessItemsSequentially();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Critical error during quality update: {Error}", ex, ex.Message);
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            _logger?.LogInformation("Quality update completed in {Duration}ms. Processed: {ProcessedCount}, Errors: {ErrorCount}",
                duration.TotalMilliseconds, processedCount, errorCount);
        }

        void ProcessItemsSequentially()
        {
            foreach (var item in _items)
            {
                ProcessSingleItem(item, ref processedCount, ref errorCount);
            }
        }

        void ProcessItemsInParallel()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _configuration.ProcessingSettings.MaxConcurrencyLevel
            };

            Parallel.ForEach(_items, parallelOptions, item =>
            {
                ProcessSingleItem(item, ref processedCount, ref errorCount);
            });
        }
    }

    private void ProcessSingleItem(IItem item, ref int processedCount, ref int errorCount)
    {
        try
        {
            var beforeEventArgs = new QualityUpdateEventArgs
            {
                Item = item,
                UpdateReason = "Starting quality update"
            };
            BeforeQualityUpdate?.Invoke(this, beforeEventArgs);

            _pipeline.ProcessItem(item);

            var afterEventArgs = new QualityUpdateEventArgs
            {
                Item = item,
                UpdateReason = "Quality update completed"
            };
            AfterQualityUpdate?.Invoke(this, afterEventArgs);

            System.Threading.Interlocked.Increment(ref processedCount);
        }
        catch (Exception ex)
        {
            System.Threading.Interlocked.Increment(ref errorCount);
            _logger?.LogError("Error processing item {ItemId}: {Error}", ex, item?.Id.ToString(), ex.Message);

            if (!_configuration.ProcessingSettings.EnableRetryLogic)
                throw;

            // Retry logic
            for (int retry = 1; retry <= _configuration.ProcessingSettings.MaxRetryAttempts; retry++)
            {
                try
                {
                    _logger?.LogInformation("Retrying item {ItemId}, attempt {RetryAttempt}", item?.Id, retry);
                    _pipeline.ProcessItem(item);
                    System.Threading.Interlocked.Increment(ref processedCount);
                    return;
                }
                catch (Exception retryEx)
                {
                    _logger?.LogWarning("Retry {RetryAttempt} failed for item {ItemId}: {Error}",
                        retry, item?.Id, retryEx.Message);
                    
                    if (retry == _configuration.ProcessingSettings.MaxRetryAttempts)
                    {
                        _logger?.LogError("All retry attempts exhausted for item {ItemId}", null, item?.Id.ToString());
                        throw;
                    }
                }
            }
        }
    }

    public async void UpdateQualityAsync()
    {
        if (!_configuration.ProcessingSettings.EnableAsyncProcessing)
        {
            UpdateQuality();
            return;
        }

        _logger?.LogInformation("Starting asynchronous quality update for {ItemCount} items", _items.Count);

        await Task.Run(() =>
        {
            try
            {
                UpdateQuality();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error in asynchronous quality update: {Error}", ex, ex.Message);
                throw;
            }
        });
    }

    // Legacy adapter methods to maintain compatibility
    public static EnhancedGildedRose CreateFromLegacyItems(IList<Item> legacyItems,
        IServiceProvider serviceProvider = null)
    {
        var items = legacyItems?.Cast<IItem>().ToList() ?? new List<IItem>();
        
        // Create minimal pipeline if no service provider
        if (serviceProvider == null)
        {
            var factory = new Strategies.QualityUpdateStrategyFactory();
            var pipeline = new QualityUpdatePipeline(factory);
            return new EnhancedGildedRose(items, pipeline);
        }

        // Use dependency injection (simplified)
        var pipelineFromDI = serviceProvider.GetService(typeof(IQualityUpdatePipeline)) as IQualityUpdatePipeline;
        var configFromDI = serviceProvider.GetService(typeof(IGildedRoseConfiguration)) as IGildedRoseConfiguration;
        var loggerFromDI = serviceProvider.GetService(typeof(IGildedRoseLogger)) as IGildedRoseLogger;
        var eventBusFromDI = serviceProvider.GetService(typeof(IEventBus)) as IEventBus;
        var metricsFromDI = serviceProvider.GetService(typeof(IItemMetrics)) as IItemMetrics;

        return new EnhancedGildedRose(items, pipelineFromDI, configFromDI, loggerFromDI, eventBusFromDI, metricsFromDI);
    }
}