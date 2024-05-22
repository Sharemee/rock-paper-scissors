using CommunityToolkit.HighPerformance.Buffers;

namespace Core;

/// <summary>
/// Represents a (possibly rented) chunk of memory that is temporarily available to use
/// </summary>
public interface IMemory : IDisposable
{
    /// <summary>
    /// The originally requested length of the memory
    /// </summary>
    int Length { get; }

    /// <summary>
    /// The underlying byte array. Note that this will probably be longer than the requested length.
    /// Always use <see cref="Length"/> instead of the length property of the <see cref="Bytes"/>.
    /// Prefer to use <see cref="Span"/> or <see cref="Memory"/> if possible
    /// </summary>
    byte[] Bytes { get; }

    /// <summary>
    /// An instance of span wrapping the memory (for use in synchronous methods)
    /// </summary>
    Span<byte> Span { get; }

    /// <summary>
    /// An instance of memory wrapping the memory (for use in asynchronous methods)
    /// </summary>
    Memory<byte> Memory { get; }
}

public interface IMemoryProvider
{
    IMemory Provide(int length);
}

public class ArrayPoolMemoryProvider : IMemoryProvider
{
    public IMemory Provide(int length)
    {
        return new ArrayPoolMemory(MemoryOwner<byte>.Allocate(length));
    }
}

/// <summary>
/// Represents memory collected from an array pool
/// Once disposed, the array is returned to the pool
/// </summary>
public class ArrayPoolMemory : IMemory
{
    private readonly MemoryOwner<byte> _memoryOwner;

    public ArrayPoolMemory(MemoryOwner<byte> memoryOwner)
    {
        _memoryOwner = memoryOwner ?? throw new ArgumentNullException(nameof(memoryOwner));
        Bytes = _memoryOwner.DangerousGetArray().Array!;
    }

    ~ArrayPoolMemory() => Dispose();

    public int Length => _memoryOwner.Length;
    public byte[] Bytes { get; }
    public Span<byte> Span => _memoryOwner.Span;
    public Memory<byte> Memory => _memoryOwner.Memory;

    public void Dispose() => _memoryOwner.Dispose();
}
