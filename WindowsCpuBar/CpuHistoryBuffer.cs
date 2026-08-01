namespace WindowsCpuBar;

internal sealed class CpuHistoryBuffer
{
    private readonly double[] _samples;
    private int _writeIndex;
    private int _count;

    public CpuHistoryBuffer(int capacity)
    {
        Capacity = Math.Max(2, capacity);
        _samples = new double[Capacity];
    }

    public int Capacity { get; }

    public int Count => _count;

    public void Clear()
    {
        _writeIndex = 0;
        _count = 0;
    }

    public void Add(double percent)
    {
        _samples[_writeIndex] = Math.Clamp(percent, 0, 100);
        _writeIndex = (_writeIndex + 1) % _samples.Length;
        if (_count < _samples.Length)
        {
            _count++;
        }
    }

    public double GetOrdered(int index)
    {
        if (index < 0 || index >= _count)
        {
            return 0;
        }

        var start = (_writeIndex - _count + _samples.Length) % _samples.Length;
        return _samples[(start + index) % _samples.Length];
    }
}
