using System.Text.RegularExpressions;

namespace StringBuffer;

// Contains fallback implementations for the Regex methods compatible with netstandard2.0

public partial class StringBuffer
{
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void Replace(Regex regex, ReadOnlySpan<char> to)
    {
        // Allows use of the Span-based Regex APIs
        // Have to use the old APIs unfortunately
    }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="to">The replacement <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.</param>
    public void ReplaceAll(Regex regex, ReadOnlySpan<char> to) { }

    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void Replace(Regex regex, int bufferSize, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="bufferSize">The maximum length any single replacement will be. The first null character of the end of the supplied buffer marks the end of the replacement.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAll(Regex regex, int bufferSize, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces the first occurrence of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer.</param>
    public void ReplaceExact(Regex regex, int length, StringBufferWriter writeReplacementAction) { }
    /// <summary>
    /// Replaces all occurrences of a <see cref="Regex"/> match in the buffer with a replacement action. The length of that replacement is fixed to the specified length.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/> to match against the buffer.</param>
    /// <param name="length">The exact length of the replacement content.</param>
    /// <param name="writeReplacementAction">A <see cref="StringBufferWriter"/> that writes the replacement content to the buffer. The method must not assume that the buffer will be reused for subsequent replacements.</param>
    public void ReplaceAllExact(Regex regex, int length, StringBufferWriter writeReplacementAction) { }
}
