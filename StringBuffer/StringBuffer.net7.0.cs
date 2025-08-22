using System.Buffers;
using System.Text.RegularExpressions;

namespace StringBuffer;

// Contains features limited to net7.0, like Span-based implementations for the Regex methods

public partial class StringBuffer
{
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void Replace(Regex regex, ReadOnlySpan<char> to)
    {
        ArgumentNullException.ThrowIfNull(regex);

        foreach (var vm in regex.EnumerateMatches(Span))
        {
            ReplaceCore(vm.Index, vm.Length, to);
            break;
        }
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void ReplaceAll(Regex regex, ReadOnlySpan<char> to)
    {
        ArgumentNullException.ThrowIfNull(regex);

        var currentEnumerator = regex.EnumerateMatches(Span);
        foreach (var vm in currentEnumerator)
        {
            // There is unfortunately no easier way to do this since each match may vary in length.
            ReplaceCore(vm.Index, vm.Length, to);
            if (to.Length != vm.Length)
            {
                // If the replacement length is different, we need a new enumerator
                currentEnumerator = regex.EnumerateMatches(Span, vm.Index + to.Length);
            }
        }
    }

    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void Replace(Regex regex, int bufferSize, StringBufferWriter writeReplacementAction)
    {
        ArgumentNullException.ThrowIfNull(regex);
        if (bufferSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be non-negative.");
        }

        // Specifying length == 0 is... weird, but we allow it for consistency
        if (bufferSize == 0)
        {
            Replace(regex, default);
            return;
        }

        Span<char> buffer = bufferSize <= SafeCharStackalloc ? stackalloc char[bufferSize] : new char[bufferSize];
        foreach (var vm in regex.EnumerateMatches(Span))
        {
            writeReplacementAction(buffer, Span.Slice(vm));
            var endIdx = buffer.IndexOf('\0');
            var to = buffer;
            if (endIdx > -1)
            {
                to = buffer[..endIdx];
            }
            ReplaceCore(vm.Index, vm.Length, to);
            break;
        }
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements or, consequently, retain any content from the previous iteration.</param>
    public void ReplaceAll(Regex regex, int bufferSize, StringBufferWriter writeReplacementAction)
    {
        ArgumentNullException.ThrowIfNull(regex);
        if (bufferSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be non-negative.");
        }

        if (bufferSize == 0)
        {
            Replace(regex, default);
            return;
        }

        Span<char> buffer = bufferSize <= SafeCharStackalloc ? stackalloc char[bufferSize] : new char[bufferSize];
        var currentEnumerator = regex.EnumerateMatches(Span);
        foreach (var vm in currentEnumerator)
        {
            writeReplacementAction(buffer, Span.Slice(vm));
            var endIdx = buffer.IndexOf('\0');
            var to = buffer;
            if (endIdx > -1)
            {
                to = buffer[..endIdx];
            }
            ReplaceCore(vm.Index, vm.Length, to);
            if (buffer.Length != vm.Length)
            {
                // If the replacement length is different, we need a new enumerator
                currentEnumerator = regex.EnumerateMatches(Span, vm.Index + to.Length);
            }
        }
    }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void ReplaceExact(Regex regex, int length, StringBufferWriter writeReplacementAction)
    {
        ArgumentNullException.ThrowIfNull(regex);
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        }

        if (length == 0)
        {
            Replace(regex, default);
            return;
        }

        Span<char> buffer = length <= SafeCharStackalloc ? stackalloc char[length] : new char[length];
        foreach (var vm in regex.EnumerateMatches(Span))
        {
            writeReplacementAction(buffer, Span.Slice(vm));
            ReplaceCore(vm.Index, vm.Length, buffer);
            break;
        }
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAllExact(Regex regex, int length, StringBufferWriter writeReplacementAction)
    {
        ArgumentNullException.ThrowIfNull(regex);
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        }

        if (length == 0)
        {
            ReplaceAll(regex, default);
            return;
        }

        Span<char> buffer = length <= SafeCharStackalloc ? stackalloc char[length] : new char[length];
        var currentEnumerator = regex.EnumerateMatches(Span);
        foreach (var vm in currentEnumerator)
        {
            writeReplacementAction(buffer, Span.Slice(vm));
            ReplaceCore(vm.Index, vm.Length, buffer);
            if (buffer.Length != vm.Length)
            {
                // If the replacement length is different, we need a new enumerator
                currentEnumerator = regex.EnumerateMatches(Span, vm.Index + buffer.Length);
            }
        }
    }
}

internal static class Extensions
{
    public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, ValueMatch vm) => span.Slice(vm.Index, vm.Length);
    public static Span<T> Slice<T>(this Span<T> span, ValueMatch vm) => span.Slice(vm.Index, vm.Length);
}