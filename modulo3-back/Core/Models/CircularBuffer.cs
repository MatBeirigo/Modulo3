namespace Core.Models;

public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }
    }

    public List<T> GetAll()
    {
        lock (_lock)
        {
            var result = new List<T>(_count);
            var start = _count < _buffer.Length ? 0 : _head;
            for (int i = 0; i < _count; i++)
                result.Add(_buffer[(start + i) % _buffer.Length]);
            return result;
        }
    }
}