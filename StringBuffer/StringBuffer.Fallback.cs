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
        using var matchBuffer = regex.CreateMatchBuffer();
    }

    /// <summary>
    /// Replaces the first occurrence of a <see cref="PcreRegex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void Replace(PcreRegex regex, int bufferSize, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces all occurrences of a <see cref="PcreRegex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAll(PcreRegex regex, int bufferSize, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="PcreRegex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void ReplaceExact(PcreRegex regex, int length, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces all occurrences of a <see cref="PcreRegex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="PcreRegex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAllExact(PcreRegex regex, int length, StringBufferWriter writeReplacementAction) { }
}

internal static class Extensions
{
    public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, PcreRefMatch match) => span.Slice(match.Index, match.Length);
    public static Span<T> Slice<T>(this Span<T> span, PcreRefMatch match) => span.Slice(match.Index, match.Length);
}