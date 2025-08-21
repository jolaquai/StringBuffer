namespace StringBuffer;

// Contains features limited to net6.0, like ISpanFormattable

public partial class StringBuffer
{
    public void Append(ISpanFormattable spanFormattable, ReadOnlySpan<char> format = default, IFormatProvider formatProvider = null)
    {
        // Try with the current remaining space first
        if (TryWriteSpanFormattable(spanFormattable, format, formatProvider))
        {
            return;
        }

        // Following the implementation specification of ISpanFormattable, a return of false means there wasn't enough space
        // Any other failures should throw instead
        // So we expand the buffer and try again
        Grow();
        if (!TryWriteSpanFormattable(spanFormattable, format, formatProvider))
        {
            throw new InvalidOperationException("Failed to write ISpanFormattable after expanding the buffer once. Something might be wrong with its implementation.");
        }
        // If we reach here, it means the spanFormattable was successfully written

        bool TryWriteSpanFormattable(ISpanFormattable spanFormattable, ReadOnlySpan<char> format, IFormatProvider formatProvider)
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
