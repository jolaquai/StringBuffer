using System.Text.RegularExpressions;

using PCRE;

namespace StringBuffer;

// Contains fallback implementations for the Regex methods for TargetFrameworks up to net7.0

public partial class StringBuffer
{
    /// <summary>
    /// Replaces the first occurrence of a <see cref="PcreRegex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void Replace(PcreRegex regex, ReadOnlySpan<char> to)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }

        var match = regex.Match(Span);
        ReplaceCore(match.Index, match.Length, to);
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="PcreRegex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void ReplaceAll(PcreRegex regex, ReadOnlySpan<char> to)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }

        using var matchBuffer = regex.CreateMatchBuffer();
        PcreRefMatch match;
        var start = 0;
        while ((match = matchBuffer.Match(Span, start)).Success)
        {
            ReplaceCore(match.Index, match.Length, to);
            start = match.Index + to.Length; // Move past the current match
        }
    }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void Replace(PcreRegex regex, int bufferSize, StringBufferWriter writeReplacementAction)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }
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

        if (writeReplacementAction is null)
        {
            throw new ArgumentNullException(nameof(writeReplacementAction), "Write replacement action cannot be null.");
        }

        Span<char> buffer = bufferSize <= SafeCharStackalloc ? stackalloc char[bufferSize] : new char[bufferSize];
        var match = regex.Match(Span);
        writeReplacementAction(buffer, Span.Slice(match));
        var endIdx = buffer.IndexOf('\0');
        var to = buffer;
        if (endIdx > -1)
        {
            to = buffer[..endIdx];
        }
        ReplaceCore(match.Index, match.Length, to);
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements or, consequently, retain any content from the previous iteration.</param>
    public void ReplaceAll(PcreRegex regex, int bufferSize, StringBufferWriter writeReplacementAction)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }
        if (bufferSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be non-negative.");
        }

        if (bufferSize == 0)
        {
            Replace(regex, default);
            return;
        }

        if (writeReplacementAction is null)
        {
            throw new ArgumentNullException(nameof(writeReplacementAction), "Write replacement action cannot be null.");
        }

        using var matchBuffer = regex.CreateMatchBuffer();
        Span<char> buffer = bufferSize <= SafeCharStackalloc ? stackalloc char[bufferSize] : new char[bufferSize];
        PcreRefMatch match;
        var start = 0;
        while ((match = matchBuffer.Match(Span, start)).Success)
        {
            writeReplacementAction(buffer, Span.Slice(match));
            var endIdx = buffer.IndexOf('\0');
            var to = buffer;
            if (endIdx > -1)
            {
                to = buffer[..endIdx];
            }
            ReplaceCore(match.Index, match.Length, to);
            start = match.Index + to.Length; // Move past the current match
        }
    }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void ReplaceExact(PcreRegex regex, int length, StringBufferWriter writeReplacementAction)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Replacement length must be non-negative.");
        }

        if (length == 0)
        {
            Replace(regex, default);
            return;
        }

        if (writeReplacementAction is null)
        {
            throw new ArgumentNullException(nameof(writeReplacementAction), "Write replacement action cannot be null.");
        }

        Span<char> buffer = length <= SafeCharStackalloc ? stackalloc char[length] : new char[length];
        var match = regex.Match(Span);

        writeReplacementAction(buffer, Span.Slice(match));
        ReplaceCore(match.Index, match.Length, buffer);
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAllExact(PcreRegex regex, int length, StringBufferWriter writeReplacementAction)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex), "Regex cannot be null.");
        }
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Replacement length must be non-negative.");
        }

        if (length == 0)
        {
            ReplaceAll(regex, default);
            return;
        }

        if (writeReplacementAction is null)
        {
            throw new ArgumentNullException(nameof(writeReplacementAction), "Write replacement action cannot be null.");
        }

        using var matchBuffer = regex.CreateMatchBuffer();
        Span<char> buffer = length <= SafeCharStackalloc ? stackalloc char[length] : new char[length];
        PcreRefMatch match;
        var start = 0;
        while ((match = matchBuffer.Match(Span, start)).Success)
        {
            writeReplacementAction(buffer, Span.Slice(match));
            ReplaceCore(match.Index, match.Length, buffer);
            start = match.Index + buffer.Length; // Move past the current match
        }
    }
}

internal static class Extensions
{
    public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, PcreRefMatch match) => span.Slice(match.Index, match.Length);
    public static Span<T> Slice<T>(this Span<T> span, PcreRefMatch match) => span.Slice(match.Index, match.Length);
}