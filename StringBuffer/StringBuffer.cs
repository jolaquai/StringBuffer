using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace StringBuffer;

/// <summary>
/// Represents a method that generates a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> for a given match <see cref="ReadOnlySpan{T}"/>.
/// </summary>
/// <param name="match">The matched <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
/// <returns>The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</returns>
public delegate ReadOnlySpan<char> StringBufferReplacementFactory(ReadOnlySpan<char> match);
/// <summary>
/// Represents a method that writes a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to a given buffer.
/// </summary>
/// <param name="buffer">The buffer to write the replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to.</param>
/// <param name="match">The matched <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
public delegate void StringBufferWriter(Span<char> buffer, ReadOnlySpan<char> match);

/// <summary>
/// Represents a custom builder for creating <see langword="string"/>s with a mutable, directly accessible buffer and a versatile API for manipulating the contents.
/// </summary>
/// <remarks>
/// This type is not thread-safe. Concurrent use will result in corrupted data. Access to instances of this type must be synchronized.
/// </remarks>
public sealed partial class StringBuffer
{
    /// <summary>
    /// Gets the maximum capacity of a single <see cref="StringBuffer"/>.
    /// </summary>
    public const int MaxCapacity = int.MaxValue;
    private const int DefaultCapacity = 256;

    private char[] buffer;
    private int length;

    /// <summary>
    /// Gets a <see cref="Span{T}"/> over the used portion of the buffer (not including unused space).
    /// </summary>
    public Span<char> Span => buffer.AsSpan(0, length);
    /// <summary>
    /// Gets or sets the <see langword="char"/> at the specified index in the used portion of the buffer.
    /// </summary>
    /// <param name="index">The index of the <see langword="char"/> to get or set.</param>
    /// <returns>The <see langword="char"/> at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> resolves to an offset that is outside the bounds of the used portion of the buffer.</exception>
    public char this[Index index]
    {
        get
        {
            var offset = index.GetOffset(length);
            if (offset < 0 || offset >= length)
            {
                throw new IndexOutOfRangeException($"Index ({offset}) must be within the bounds of the used portion of the buffer.");
            }
            return buffer[offset];
        }
        set
        {
            var offset = index.GetOffset(length);
            if (offset < 0 || offset >= length)
            {
                throw new IndexOutOfRangeException($"Index ({offset}) must be within the bounds of the used portion of the buffer.");
            }
            buffer[offset] = value;
        }
    }
    /// <summary>
    /// Gets a <see cref="Span{T}"/> that can be used to write further content to the buffer.
    /// </summary>
    /// <param name="minimumSize">A minimum size of the returned <see cref="Span{T}"/>. If unspecified or less than or equal to <c>0</c>, some non-zero-length <see cref="Span{T}"/> will be returned.</param>
    /// <returns>The writable <see cref="Span{T}"/> over the buffer.</returns>
    public Span<char> GetWritableSpan(int minimumSize = 0)
    {
        if (minimumSize <= 0)
        {
            return buffer.AsSpan(length);
        }
        if (minimumSize > buffer.Length - length)
        {
            GrowIfNeeded(length + minimumSize);
        }
        return buffer.AsSpan(length, buffer.Length - length);
    }

    /// <summary>
    /// Appends a single <see cref="char"/> to the end of the buffer.
    /// </summary>
    /// <param name="value">The <see cref="char"/> to append.</param>
    public void Append(char value)
    {
        GrowIfNeeded(length + 1);
        buffer[length++] = value;
    }
    /// <summary>
    /// Appends a <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to the end of the buffer.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to append.</param>
    public void Append(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return;
        }
        GrowIfNeeded(length + value.Length);
        // We just made sure this will fit
        value.CopyTo(GetWritableSpan());
        length += value.Length;
    }

    /// <summary>
    /// Finds the first index of a specified <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>The index of the first occurrence of <paramref name="value"/> in the buffer, or <c>-1</c> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(char value, int start = 0) => Span[start..].IndexOf(value);
    /// <summary>
    /// Finds the first index of a specified <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>The index of the first occurrence of <paramref name="value"/> in the buffer, or <c>-1</c> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(ReadOnlySpan<char> value, int start = 0) => Span[start..].IndexOf(value);
    /// <summary>
    /// Enumerates all indices of a specified <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of indices where <paramref name="value"/> occurs in the buffer.</returns>
    /// <remarks>
    /// The enumeration is not stable; enumeration always operates on the current contents of the buffer, so changes to its contents do not affect or interrupt enumeration.
    /// This is the cheaper alternative to <see cref="EnumerateIndicesOf(char, int)"/> if you own and solely control the buffer.
    /// </remarks>
    public IEnumerable<int> EnumerateIndicesOfUnsafe(char value, int start = 0)
    {
        var index = start;
        while ((index = IndexOf(value, index)) != -1)
        {
            yield return index;
            index++;
        }
    }
    /// <summary>
    /// Enumerates all indices of a specified <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of indices where <paramref name="value"/> occurs in the buffer.</returns>
    /// <remarks>
    /// The enumeration is guaranteed to be stable; if the underlying buffer is modified during enumeration, an <see cref="InvalidOperationException"/> is thrown.
    /// Conversely, each enumerator advancement becomes slightly more expensive.
    /// </remarks>
    public IEnumerable<int> EnumerateIndicesOf(char value, int start = 0)
    {
        var index = start;
        var bufHash = XxHash32.Hash(MemoryMarshal.Cast<char, byte>(Span));
        while ((index = IndexOf(value, index)) != -1)
        {
            if (bufHash != XxHash32.Hash(MemoryMarshal.Cast<char, byte>(Span)))
            {
                throw new InvalidOperationException("The buffer was modified during enumeration.");
            }
            yield return index;
            index++;
        }
    }
    /// <summary>
    /// Enumerates all indices of a specified <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of indices where <paramref name="value"/> occurs in the buffer.</returns>
    /// <remarks>
    /// The enumeration is not stable; enumeration always operates on the current contents of the buffer, so changes to its contents do not affect or interrupt enumeration.
    /// This is the cheaper alternative to <see cref="EnumerateIndicesOf(ReadOnlySpan{char}, int)"/> if you own and solely control the buffer.
    /// </remarks>
    public IEnumerable<int> EnumerateIndicesOfUnsafe(ReadOnlySpan<char> value, int start = 0)
    {
        var index = start;
        while ((index = IndexOf(value, index)) != -1)
        {
            yield return index;
            index++;
        }
    }
    /// <summary>
    /// Enumerates all indices of a specified <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> in the buffer, starting from the specified index.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to find.</param>
    /// <param name="start">At which index in the buffer to start searching.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of indices where <paramref name="value"/> occurs in the buffer.</returns>
    /// <remarks>
    /// The enumeration is guaranteed to be stable; if the underlying buffer is modified during enumeration, an <see cref="InvalidOperationException"/> is thrown.
    /// Conversely, each enumerator advancement becomes slightly more expensive.
    /// </remarks>
    public IEnumerable<int> EnumerateIndicesOf(ReadOnlySpan<char> value, int start = 0)
    {
        var index = start;
        var bufHash = XxHash32.Hash(MemoryMarshal.Cast<char, byte>(Span));
        while ((index = IndexOf(value, index)) != -1)
        {
            if (bufHash != XxHash32.Hash(MemoryMarshal.Cast<char, byte>(Span)))
            {
                throw new InvalidOperationException("The buffer was modified during enumeration.");
            }
            yield return index;
            index++;
        }
    }

    /// <summary>
    /// Implements core logic for replacement operations at a specific index in the buffer.
    /// </summary>
    private void ReplaceCore(int index, int len, ReadOnlySpan<char> to)
    {
        if (to.Length == 0)
        {
            // Copy everything after index + len TO the index
            var remaining = Span[(index + len)..];
            remaining.CopyTo(Span[index..]);
            // Reduce length
            length -= len;
        }
        else if (to.Length < len)
        {
            // Also easy, copy everything after index + len to index + to.Length
            var remaining = Span[(index + len)..];
            remaining.CopyTo(Span[(index + to.Length)..]);
            // Copy the new content to the index
            to.CopyTo(Span[index..]);
            // Reduce length
            length -= (len - to.Length);
        }
        else if (to.Length == len)
        {
            // Just copy over the existing content
            to.CopyTo(Span[index..]);
        }
        else
        {
            // We need to grow the buffer
            GrowIfNeeded(length + (to.Length - len));
            // Copy everything after index + len to index + to.Length
            var remaining = Span[(index + len)..];
            remaining.CopyTo(Span[(index + to.Length)..]);
            // Copy the new content to the index
            to.CopyTo(Span[index..]);
            // Increase length
            length += (to.Length - len);
        }
    }
    /// <summary>
    /// Implements core logic for replacement operations at multiple indices in the buffer with the same new content.
    /// </summary>
    private void ReplaceCore(ReadOnlySpan<int> indices, int len, ReadOnlySpan<char> to)
    {
        var lengthDiff = to.Length - len;
        var totalLengthChange = lengthDiff * indices.Length;

        if (lengthDiff > 0)
        {
            GrowIfNeeded(length + totalLengthChange);

            // Work backwards to avoid overwriting
            for (var i = indices.Length - 1; i >= 0; i--)
            {
                var srcStart = indices[i] + len;
                var dstStart = srcStart + (lengthDiff * (i + 1));
                var copyLen = (i == indices.Length - 1) ? length - srcStart : indices[i + 1] - srcStart;

                buffer.AsSpan(srcStart, copyLen).CopyTo(buffer.AsSpan(dstStart, copyLen));
                to.CopyTo(buffer.AsSpan(indices[i] + (lengthDiff * i), to.Length));
            }
        }
        else
        {
            // Work forwards, compacting as we go
            var writePos = indices[0];

            for (var i = 0; i < indices.Length; i++)
            {
                to.CopyTo(buffer.AsSpan(writePos, to.Length));
                writePos += to.Length;
                var readPos = indices[i] + len;

                var nextIndex = (i + 1 < indices.Length) ? indices[i + 1] : length;
                var copyLen = nextIndex - readPos;

                buffer.AsSpan(readPos, copyLen).CopyTo(buffer.AsSpan(writePos, copyLen));
                writePos += copyLen;
            }
        }

        length += totalLengthChange;
    }

    /// <summary>
    /// Replaces the first occurrence of a <see cref="char"/> in the buffer with another <see cref="char"/>.
    /// </summary>
    /// <param name="from">The <see cref="char"/> to find.</param>
    /// <param name="to">The <see cref="char"/> to replace with.</param>
    public void Replace(char from, char to)
    {
        var index = IndexOf(from);
        if (index != -1)
        {
            buffer[index] = to;
        }
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="char"/> in the buffer with another <see cref="char"/>.
    /// </summary>
    /// <param name="from">The <see cref="char"/> to find.</param>
    /// <param name="to">The <see cref="char"/> to replace with.</param>
    public void ReplaceAll(char from, char to)
    {
        if (from == to)
        {
            return;
        }
        var index = IndexOf(from);
        while (index != -1)
        {
            buffer[index] = to;
            index = IndexOf(from, index + 1);
        }
    }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> in the buffer with another <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="from">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to find.</param>
    /// <param name="to">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to replace with.</param>
    public void Replace(ReadOnlySpan<char> from, ReadOnlySpan<char> to)
    {
        if (from.Length == 0)
        {
            throw new ArgumentException("The 'from' span must not be empty.", nameof(from));
        }
        if (from.Overlaps(to, out var offset) && offset == 0)
        {
            return;
        }
        if (from.SequenceEqual(to))
        {
            return;
        }

        var fromIdx = IndexOf(from);
        if (fromIdx == -1)
        {
            return;
        }

        ReplaceCore(fromIdx, from.Length, to);
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> in the buffer with another <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="from">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to find.</param>
    /// <param name="to">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to replace with.</param>
    public void ReplaceAll(ReadOnlySpan<char> from, ReadOnlySpan<char> to)
    {
        if (from.Length == 0)
        {
            throw new ArgumentException("The 'from' span must not be empty.", nameof(from));
        }
        if (from.Overlaps(to, out var offset) && offset == 0)
        {
            return;
        }
        if (from.SequenceEqual(to))
        {
            return;
        }

        var pool = ArrayPool<int>.Shared;
        var indices = pool.Rent(256);
        try
        {
            var i = 0;
            foreach (var idx in EnumerateIndicesOfUnsafe(from))
            {
                if (i >= indices.Length)
                {
                    var newIndices = pool.Rent(indices.Length << 1);
                    indices.AsSpan(0, i).CopyTo(newIndices);
                    pool.Return(indices);
                    indices = newIndices;
                }
                indices[i++] = idx;
            }

            ReplaceCore(indices.AsSpan(0, i), from.Length, to);
        }
        finally
        {
            pool.Return(indices);
        }
    }

    /// <summary>
    /// Initializes a new <see cref="StringBuffer"/> with the default capacity of 256.
    /// </summary>
    public StringBuffer() : this(default, DefaultCapacity) { }
    /// <summary>
    /// Initializes a new <see cref="StringBuffer"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the buffer's backing array.</param>
    public StringBuffer(int capacity) : this(default, capacity) { }
    /// <summary>
    /// Initializes a new <see cref="StringBuffer"/> with the specified initial content.
    /// </summary>
    /// <param name="initialContent">A <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> that will be copied into the buffer.</param>
    public StringBuffer(ReadOnlySpan<char> initialContent) : this(initialContent, initialContent.Length) { }
    /// <summary>
    /// Initializes a new <see cref="StringBuffer"/> with the specified initial content and capacity.
    /// </summary>
    /// <param name="initialContent">The initial content to copy into the buffer.</param>
    /// <param name="capacity">The initial capacity of the buffer's backing array. Must not be less than the length of <paramref name="initialContent"/>.</param>
    public StringBuffer(ReadOnlySpan<char> initialContent, int capacity)
    {
        if (capacity < initialContent.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must not be less than the length of the initial content.");
        }

        capacity = initialContent.Length < DefaultCapacity ? DefaultCapacity : initialContent.Length;
        buffer = new char[capacity];
        length = initialContent.Length;

        if (initialContent.Length > 0)
        {
            initialContent.CopyTo(buffer);
        }
    }
    /// <summary>
    /// Initializes a new <see cref="StringBuffer"/> as an independent copy of another <see cref="StringBuffer"/>.
    /// </summary>
    /// <param name="other">The <see cref="StringBuffer"/> to copy from.</param>
    public StringBuffer(StringBuffer other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }
        var span = other.Span;
        buffer = new char[span.Length];
        length = span.Length;
        // More efficient than non-generic Array.Copy plus constrained to the occupied length
        other.Span.CopyTo(Span);
    }

    /// <summary>
    /// Grows the buffer to ensure it can accommodate at least the specified capacity.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative or exceeds <see cref="MaxCapacity"/>.</exception>
    public void EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be non-negative and less than or equal to MaxCapacity.");
        }
        GrowIfNeeded(capacity);
    }
    /// <summary>
    /// Clears the contents of the buffer.
    /// </summary>
    public void Clear()
    {
        length = 0;
        Array.Clear(buffer, 0, buffer.Length);
    }

    #region Implementation details
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfNeeded(int requiredCapacity)
    {
        while (requiredCapacity > buffer.Length)
        {
            Grow(requiredCapacity);
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int requiredCapacity) => Array.Resize(ref buffer, Helpers.NextPowerOf2(requiredCapacity));
    #endregion

    /// <summary>
    /// Creates a <see langword="string"/> from the current contents of the buffer.
    /// If a <see cref="ReadOnlySpan{T}"/> would suffice, use <see cref="Span"/> instead.
    /// </summary>
    /// <returns>The <see langword="string"/> representation of the current buffer contents.</returns>
    public override string ToString() => Span.ToString();
}

/// <summary>
/// [Experimental] Unsafe sibling implementation of <see cref="StringBuffer"/> that utilizes unmanaged memory to alleviate GC pressure for very large buffers.
/// </summary>
internal sealed class UnsafeStringBuffer;