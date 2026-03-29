using System.Runtime.CompilerServices;

namespace uk.osric.sim.terrain;

public class Torus<T> {

    public int Size { get; }
    private readonly T[] _data;

    public Torus(int size) {
        if (size <= 0) {
            throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than zero");
        }

        if (!IsPowerOfTwo(size)) {
            throw new ArgumentException("size must be a power of two", nameof(size));
        }

        Size = size;
        _data = new T[Size * Size];
    }

    public Torus(T[] data, int size) {
        if (size <= 0) {
            throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than zero");
        }

        if (!IsPowerOfTwo(size)) {
            throw new ArgumentException("size must be a power of two", nameof(size));
        }

        if (data.Length != size * size) {
            throw new ArgumentException($"data length must equal size * size ({data.Length} != {size} * {size})");
        }

        Size = size;
        _data = data;
    }

    public T this[int x, int y] {
        get => _data[Index(x, y, Size)];
        set => _data[Index(x, y, Size)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Index(int x, int y, int wrap) {
        // wrap is the torus dimension: must be a power of two
        // mask = wrap - 1
        uint mask = (uint)(wrap - 1);

        // cast to uint BEFORE masking to get correct wrap for negatives
        uint ux = (uint)x & mask;
        uint uy = (uint)y & mask;

        // multiply in uint to avoid sign-extension surprises
        return (int)(uy * (uint)wrap + ux);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPowerOfTwo(int value) {
        return (value & (value - 1)) == 0;
    }

}