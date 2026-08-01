using System.Diagnostics;

namespace WindowsCpuBar;

internal sealed class GpuMonitor : IDisposable
{
    private const string CategoryName = "GPU Engine";
    private const string CounterName = "Utilization Percentage";
    private const string EngineTypeToken = "engtype_3D";

    private readonly Dictionary<string, PerformanceCounter> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _instanceAdapters = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private bool _initFailed;
    private DateTime _lastCounterUpdateTime = DateTime.MinValue;
    private static readonly TimeSpan CounterUpdateInterval = TimeSpan.FromMilliseconds(2000);

    public bool IsAvailable { get; private set; }

    public double UsagePercent { get; private set; }

    public void Sample()
    {
        if (!_initialized)
        {
            TryInitialize();
        }

        if (!IsAvailable)
        {
            UsagePercent = 0;
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastCounterUpdateTime >= CounterUpdateInterval)
        {
            UpdateCounters();
            _lastCounterUpdateTime = now;
        }
        UsagePercent = ReadUtilization();
    }

    private void UpdateCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory(CategoryName);
            var currentInstances = category.GetInstanceNames();

            var activeInstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var instance in currentInstances)
            {
                if (instance.Contains(EngineTypeToken, StringComparison.OrdinalIgnoreCase))
                {
                    activeInstances.Add(instance);
                }
            }

            // Remove instances that are no longer available.
            var toRemove = new List<string>();
            foreach (var key in _counters.Keys)
            {
                if (!activeInstances.Contains(key))
                {
                    toRemove.Add(key);
                }
            }

            foreach (var key in toRemove)
            {
                if (_counters.Remove(key, out var counter))
                {
                    counter.Dispose();
                }
                _instanceAdapters.Remove(key);
            }

            // Add newly discovered instances.
            foreach (var instance in activeInstances)
            {
                if (!_counters.ContainsKey(instance))
                {
                    try
                    {
                        var counter = new PerformanceCounter(CategoryName, CounterName, instance, readOnly: true);
                        counter.NextValue(); // Establish the measurement baseline; the initial value is zero.
                        
                        _counters[instance] = counter;
                        _instanceAdapters[instance] = ExtractAdapterKey(instance);
                    }
                    catch
                    {
                        // Ignore instances that disappear while being added.
                    }
                }
            }
        }
        catch
        {
            // Retry during the next update interval if an error occurs.
        }
    }

    private double ReadUtilization()
    {
        if (_counters.Count == 0)
        {
            return 0;
        }

        var adapterSums = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in _counters)
        {
            var instanceName = pair.Key;
            var counter = pair.Value;

            float value;
            try
            {
                value = counter.NextValue();
            }
            catch
            {
                continue;
            }

            if (!_instanceAdapters.TryGetValue(instanceName, out var adapterKey))
            {
                adapterKey = instanceName;
            }

            if (adapterSums.TryGetValue(adapterKey, out var current))
            {
                adapterSums[adapterKey] = current + value;
            }
            else
            {
                adapterSums[adapterKey] = value;
            }
        }

        if (adapterSums.Count == 0)
        {
            return 0;
        }

        var overall = adapterSums.Values.Max();
        return Math.Clamp(overall, 0, 100);
    }

    private void TryInitialize()
    {
        _initialized = true;

        if (_initFailed)
        {
            return;
        }

        try
        {
            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                IsAvailable = false;
                _initFailed = true;
                return;
            }

            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            _initFailed = true;
        }
    }

    private static string ExtractAdapterKey(string instanceName)
    {
        const string luidPrefix = "luid_";
        var luidIndex = instanceName.IndexOf(luidPrefix, StringComparison.OrdinalIgnoreCase);
        if (luidIndex < 0)
        {
            return instanceName;
        }

        var start = luidIndex + luidPrefix.Length;
        var physIndex = instanceName.IndexOf("_phys", start, StringComparison.OrdinalIgnoreCase);
        if (physIndex > start)
        {
            return instanceName[start..physIndex];
        }

        var firstUnderscore = instanceName.IndexOf('_', start);
        if (firstUnderscore > 0)
        {
            var secondUnderscore = instanceName.IndexOf('_', firstUnderscore + 1);
            if (secondUnderscore > 0)
            {
                return instanceName[start..secondUnderscore];
            }
        }

        return instanceName[start..];
    }

    public void Dispose()
    {
        DisposeCounters();
    }

    private void DisposeCounters()
    {
        foreach (var counter in _counters.Values)
        {
            counter.Dispose();
        }

        _counters.Clear();
        _instanceAdapters.Clear();
    }
}
