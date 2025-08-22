namespace StringBuffer;

// Contains features limited to net6.0, like ISpanFormattable

public partial class StringBuffer
{
    /// <summary>
    /// Appends an <see cref="ISpanFormattable"/> to the end of the buffer.
    /// </summary>
    /// <param name="spanFormattable">The <see cref="ISpanFormattable"/> to append.</param>
    /// <param name="format">The format string to use when formatting the <see cref="ISpanFormattable"/>. If not provided, the default format is used.</param>
    /// <param name="formatProvider">An <see cref="IFormatProvider"/> to use for formatting. If not provided, the current culture is used.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Append(ISpanFormattable spanFormattable, ReadOnlySpan<char> format = default, IFormatProvider formatProvider = null)
    {
        // Try with the current remaining space first
        if (TryWriteSpanFormattable(format))
        {
            return;
        }

        // Following the implementation specification of ISpanFormattable, a return of false means there wasn't enough space
        // Any other failures should throw instead
        // So we expand the buffer and try again
        Grow();
        if (!TryWriteSpanFormattable(format))
        {
            throw new InvalidOperationException("Failed to write ISpanFormattable after expanding the buffer once. Something might be wrong with its implementation.");
        }
        // If we reach here, it means the spanFormattable was successfully written

        bool TryWriteSpanFormattable(ReadOnlySpan<char> format)
        {
            if (spanFormattable.TryFormat(GetWritableSpan(), out var written, format, formatProvider))
            {
                Expand(written);
                return false;
            }

            return true;
        }
    }
}
