using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.v3;

#if !NET7_0_OR_GREATER
using PCRE;
#endif

namespace StringBuffer.Tests;

public class StringBufferTests
{
    #region Constructor Tests
    [Fact]
    public void Constructor_Default_CreatesEmptyBuffer()
    {
        var sb = new StringBuffer();
        Assert.Equal(0, sb.Length);
        Assert.True(sb.Capacity >= 256); // Default capacity
        Assert.Equal(string.Empty, sb.ToString());
    }

    [Fact]
    public void Constructor_WithCapacity_CreatesBufferWithSpecifiedCapacity()
    {
        var sb = new StringBuffer(512);
        Assert.Equal(0, sb.Length);
        Assert.True(sb.Capacity >= 512);
    }

    [Fact]
    public void Constructor_WithInitialContent_CopiesContent()
    {
        var sb = new StringBuffer("Hello");
        Assert.Equal(5, sb.Length);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Constructor_WithInitialContentAndCapacity_CreatesBufferWithBoth()
    {
        var sb = new StringBuffer("Test".AsSpan(), 100);
        Assert.Equal(4, sb.Length);
        Assert.True(sb.Capacity >= 100);
        Assert.Equal("Test", sb.ToString());
    }

    [Fact]
    public void Constructor_WithCapacityLessThanContent_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StringBuffer("Hello".AsSpan(), 2));
    }

    [Fact]
    public void Constructor_Copy_CreatesIndependentCopy()
    {
        var original = new StringBuffer("Original");
        var copy = new StringBuffer(original);

        Assert.Equal("Original", copy.ToString());

        copy.Append(" Modified");
        Assert.Equal("Original", original.ToString());
        Assert.Equal("Original Modified", copy.ToString());
    }

    [Fact]
    public void Constructor_CopyNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => new StringBuffer((StringBuffer)null));
    }
    #endregion

    #region Append Tests
    [Fact]
    public void Append_Char_AddsToEnd()
    {
        var sb = new StringBuffer();
        sb.Append('A');
        sb.Append('B');
        Assert.Equal("AB", sb.ToString());
    }

    [Fact]
    public void Append_Span_AddsToEnd()
    {
        var sb = new StringBuffer();
        sb.Append("Hello".AsSpan());
        sb.Append(" World".AsSpan());
        Assert.Equal("Hello World", sb.ToString());
    }

    [Fact]
    public void Append_EmptySpan_DoesNothing()
    {
        var sb = new StringBuffer("Test");
        sb.Append([]);
        Assert.Equal("Test", sb.ToString());
    }

    [Fact]
    public void Append_CausesGrowth_ExpandsBuffer()
    {
        var sb = new StringBuffer(2);
        sb.Append("This is a longer string");
        Assert.Equal("This is a longer string", sb.ToString());
    }
    #endregion

    #region Indexer Tests
    [Fact]
    public void Indexer_Get_ReturnsCorrectChar()
    {
        var sb = new StringBuffer("Hello");
        Assert.Equal('H', sb[0]);
        Assert.Equal('o', sb[^1]);
        Assert.Equal('e', sb[1]);
    }

    [Fact]
    public void Indexer_Set_ModifiesChar()
    {
        var sb = new StringBuffer("Hello");
        sb[0] = 'J';
        sb[^1] = 'y';
        Assert.Equal("Jelly", sb.ToString());
    }

    [Fact]
    public void Indexer_OutOfBounds_ThrowsException()
    {
        var sb = new StringBuffer("Test");
        Assert.Throws<IndexOutOfRangeException>(() => sb[5]);
        Assert.Throws<IndexOutOfRangeException>(() => sb[^5]);
    }
    #endregion

    #region IndexOf Tests
    [Fact]
    public void IndexOf_Char_FindsFirstOccurrence()
    {
        var sb = new StringBuffer("Hello World");
        Assert.Equal(2, sb.IndexOf('l'));
        Assert.Equal(6, sb.IndexOf('W'));
        Assert.Equal(-1, sb.IndexOf('z'));
    }

    [Fact]
    public void IndexOf_CharWithStart_FindsFromPosition()
    {
        var sb = new StringBuffer("Hello World");
        Assert.Equal(3, sb.IndexOf('l', 3));
        Assert.Equal(9, sb.IndexOf('l', 4));
        Assert.Equal(-1, sb.IndexOf('H', 1));
    }

    [Fact]
    public void IndexOf_Span_FindsFirstOccurrence()
    {
        var sb = new StringBuffer("Hello World Hello");
        Assert.Equal(0, sb.IndexOf("Hello".AsSpan()));
        Assert.Equal(6, sb.IndexOf("World".AsSpan()));
        Assert.Equal(-1, sb.IndexOf("Goodbye".AsSpan()));
    }

    [Fact]
    public void IndexOf_SpanWithStart_FindsFromPosition()
    {
        var sb = new StringBuffer("Hello World Hello");
        Assert.Equal(12, sb.IndexOf("Hello".AsSpan(), 5));
        Assert.Equal(-1, sb.IndexOf("World".AsSpan(), 10));
    }
    #endregion

    #region EnumerateIndicesOf Tests
    [Fact]
    public void EnumerateIndicesOfUnsafe_Char_FindsAllOccurrences()
    {
        var sb = new StringBuffer("abcabcabc");
        var indices = sb.EnumerateIndicesOfUnsafe('a').ToList();
        Assert.Equal(new[] { 0, 3, 6 }, indices);
    }

    [Fact]
    public void EnumerateIndicesOf_Char_DetectsModification()
    {
        var sb = new StringBuffer("abcabc");
        var enumerator = sb.EnumerateIndicesOf('a').GetEnumerator();

        enumerator.MoveNext();
        Assert.Equal(0, enumerator.Current);

        sb.Append('x'); // Modify buffer

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void EnumerateIndicesOfUnsafe_Span_FindsAllOccurrences()
    {
        var sb = new StringBuffer("ababab");
        var indices = new List<int>();
        foreach (var idx in sb.EnumerateIndicesOfUnsafe("ab".AsSpan()))
        {
            indices.Add(idx);
        }
        Assert.Equal(new[] { 0, 2, 4 }, indices);
    }

    [Fact]
    public void EnumerateIndicesOf_Span_DetectsModification()
    {
        // Have to do it this way because the enumerator is a ref struct
#pragma warning disable IDE0022 // Use expression body for method
        Assert.Throws<InvalidOperationException>(() =>
        {
            var sb = new StringBuffer("ababab");
            var enumerator = sb.EnumerateIndicesOf("ab".AsSpan()).GetEnumerator();

            enumerator.MoveNext();
            Assert.Equal(0, enumerator.Current);

            sb.Append('x'); // Modify buffer
            return enumerator.MoveNext();
        });
#pragma warning restore IDE0022 // Use expression body for method
    }
    #endregion

    #region Replace Tests - Single Occurrence
    [Fact]
    public void Replace_CharToChar_ReplacesFirst()
    {
        var sb = new StringBuffer("Hello");
        sb.Replace('l', 'w');
        Assert.Equal("Hewlo", sb.ToString());
    }

    [Fact]
    public void Replace_SpanToSpan_SameLength()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace("World".AsSpan(), "Earth".AsSpan());
        Assert.Equal("Hello Earth", sb.ToString());
    }

    [Fact]
    public void Replace_SpanToSpan_Shorter()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace("World".AsSpan(), "You".AsSpan());
        Assert.Equal("Hello You", sb.ToString());
    }

    [Fact]
    public void Replace_SpanToSpan_Longer()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace("World".AsSpan(), "Universe".AsSpan());
        Assert.Equal("Hello Universe", sb.ToString());
    }

    [Fact]
    public void Replace_SpanToEmpty_RemovesText()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace("World".AsSpan(), []);
        Assert.Equal("Hello ", sb.ToString());
    }

    [Fact]
    public void Replace_Range_ReplacesSpecifiedRange()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace(6..11, "Earth".AsSpan());
        Assert.Equal("Hello Earth", sb.ToString());
    }

    [Fact]
    public void Replace_IndexLength_ReplacesSpecifiedRange()
    {
        var sb = new StringBuffer("Hello World");
        sb.Replace(0, 5, "Goodbye".AsSpan());
        Assert.Equal("Goodbye World", sb.ToString());
    }

    [Fact]
    public void Replace_InvalidRange_ThrowsException()
    {
        var sb = new StringBuffer("Test");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sb.Replace(-1, 2, "X".AsSpan()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sb.Replace(3, 5, "X".AsSpan()));
    }
    #endregion

    #region ReplaceAll Tests
    [Fact]
    public void ReplaceAll_CharToChar_ReplacesAll()
    {
        var sb = new StringBuffer("Hello");
        sb.ReplaceAll('l', 'w');
        Assert.Equal("Hewwo", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_SameChar_DoesNothing()
    {
        var sb = new StringBuffer("Hello");
        sb.ReplaceAll('l', 'l');
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_SpanToSpan_ReplacesAll()
    {
        var sb = new StringBuffer("abc abc abc");
        sb.ReplaceAll("abc".AsSpan(), "xyz".AsSpan());
        Assert.Equal("xyz xyz xyz", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_SpanToShorter_CompactsText()
    {
        var sb = new StringBuffer("Hello World Hello World");
        sb.ReplaceAll("World".AsSpan(), "W".AsSpan());
        Assert.Equal("Hello W Hello W", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_SpanToLonger_ExpandsText()
    {
        var sb = new StringBuffer("a b a b");
        sb.ReplaceAll("a".AsSpan(), "aaa".AsSpan());
        Assert.Equal("aaa b aaa b", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_EmptyFrom_ThrowsException()
    {
        var sb = new StringBuffer("Test");
        Assert.Throws<ArgumentException>(() =>
            sb.ReplaceAll([], "X".AsSpan()));
    }
    #endregion

    #region Remove Tests
    [Fact]
    public void Remove_Range_RemovesSpecifiedRange()
    {
        var sb = new StringBuffer("Hello World");
        sb.Remove(5..11);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Remove_IndexLength_RemovesSpecifiedRange()
    {
        var sb = new StringBuffer("Hello World");
        sb.Remove(0, 6);
        Assert.Equal("World", sb.ToString());
    }
    #endregion

    #region Trim Tests
    [Fact]
    public void Trim_Char_RemovesFromBothEnds()
    {
        var sb = new StringBuffer("***Hello***");
        sb.Trim('*');
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void TrimStart_Char_RemovesFromStart()
    {
        var sb = new StringBuffer("   Hello");
        sb.TrimStart(' ');
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void TrimEnd_Char_RemovesFromEnd()
    {
        var sb = new StringBuffer("Hello   ");
        sb.TrimEnd(' ');
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Trim_Span_RemovesAnyFromBothEnds()
    {
        var sb = new StringBuffer("*#*Hello*#*");
        sb.Trim("*#".AsSpan());
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void TrimSequence_RemovesExactSequence()
    {
        var sb = new StringBuffer("ababHelloabab");
        sb.TrimSequence("ab".AsSpan());
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void TrimSequenceStart_RemovesFromStart()
    {
        var sb = new StringBuffer("xyxyHello");
        sb.TrimSequenceStart("xy".AsSpan());
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void TrimSequenceEnd_RemovesFromEnd()
    {
        var sb = new StringBuffer("Helloxyxy");
        sb.TrimSequenceEnd("xy".AsSpan());
        Assert.Equal("Hello", sb.ToString());
    }
    #endregion

    #region Length Modification Tests
    [Fact]
    public void Truncate_ReducesLength()
    {
        var sb = new StringBuffer("Hello World");
        sb.Truncate(5);
        Assert.Equal(5, sb.Length);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Truncate_InvalidLength_ThrowsException()
    {
        var sb = new StringBuffer("Test");
        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Truncate(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Truncate(10));
    }

    [Fact]
    public void Trim_Count_RemovesFromEnd()
    {
        var sb = new StringBuffer("Hello World");
        sb.Trim(6);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Expand_IncreasesLength()
    {
        var sb = new StringBuffer("Hello");
        var span = sb.GetWritableSpan(10);
        " World".AsSpan().CopyTo(span);
        sb.Expand(6);
        Assert.Equal("Hello World", sb.ToString());
    }

    [Fact]
    public void GetWritableSpan_ReturnsWritableArea()
    {
        var sb = new StringBuffer("Hello");
        var span = sb.GetWritableSpan(10);
        Assert.True(span.Length >= 10);

        " World".AsSpan().CopyTo(span);
        sb.Expand(6);
        Assert.Equal("Hello World", sb.ToString());
    }

    [Fact]
    public void EnsureCapacity_GrowsBuffer()
    {
        var sb = new StringBuffer(10);
        sb.EnsureCapacity(100);
        Assert.True(sb.Capacity >= 100);
    }
    #endregion

    #region Clear Tests
    [Fact]
    public void Clear_ResetsLength()
    {
        var sb = new StringBuffer("Hello World");
        var capacity = sb.Capacity;
        sb.Clear();

        Assert.Equal(0, sb.Length);
        Assert.Equal(capacity, sb.Capacity);
        Assert.Equal(string.Empty, sb.ToString());
    }

    [Fact]
    public void Clear_WithWipe_ClearsContent()
    {
        var sb = new StringBuffer("Sensitive Data");
        sb.Clear(true);

        Assert.Equal(0, sb.Length);
        Assert.Equal(string.Empty, sb.ToString());
    }
    #endregion

    #region Properties Tests
    [Fact]
    public void Properties_CorrectValues()
    {
        var sb = new StringBuffer("Hello");

        Assert.Equal(5, sb.Length);
        Assert.True(sb.Capacity >= 5);
        Assert.Equal(sb.Capacity - 5, sb.FreeCapacity);

        sb.Append(" World");
        Assert.Equal(11, sb.Length);
        Assert.Equal(sb.Capacity - 11, sb.FreeCapacity);
    }

    [Fact]
    public void Span_ReturnsCurrentContent()
    {
        var sb = new StringBuffer("Hello");
        var span = sb.Span;

        Assert.Equal(5, span.Length);
        Assert.True(span.SequenceEqual("Hello".AsSpan()));

        // Modifying through span should affect buffer
        span[0] = 'J';
        Assert.Equal("Jello", sb.ToString());
    }
    #endregion

    #region Regex Tests
#if NET7_0_OR_GREATER
    [Fact]
    public void Replace_Regex_ReplacesFirst()
    {
        var sb = new StringBuffer("Hello 123 World 456");
        var regex = new Regex(@"\d+");
        sb.Replace(regex, "XXX".AsSpan());
        Assert.Equal("Hello XXX World 456", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_Regex_ReplacesAll()
    {
        var sb = new StringBuffer("Hello 123 World 456");
        var regex = new Regex(@"\d+");
        sb.ReplaceAll(regex, "XXX".AsSpan());
        Assert.Equal("Hello XXX World XXX", sb.ToString());
    }

    [Fact]
    public void Replace_RegexWithAction_UsesReplacementAction()
    {
        var sb = new StringBuffer("Hello 123 World");
        var regex = new Regex(@"\d+");
        sb.Replace(regex, 10, (buffer, match) =>
        {
            "NUM".AsSpan().CopyTo(buffer);
            buffer[3] = '\0';
        });
        Assert.Equal("Hello NUM World", sb.ToString());
    }

    [Fact]
    public void ReplaceExact_Regex_ReplacesWithExactLength()
    {
        var sb = new StringBuffer("Hello 123 World");
        var regex = new Regex(@"\d+");
        sb.ReplaceExact(regex, 5, (buffer, match) =>
        {
            "[NUM]".AsSpan().CopyTo(buffer);
        });
        Assert.Equal("Hello [NUM] World", sb.ToString());
    }
#else
    [Fact]
    public void Replace_PcreRegex_ReplacesFirst()
    {
        var sb = new StringBuffer("Hello 123 World 456");
        var regex = new PcreRegex(@"\d+");
        sb.Replace(regex, "XXX".AsSpan());
        Assert.Equal("Hello XXX World 456", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_PcreRegex_ReplacesAll()
    {
        var sb = new StringBuffer("Hello 123 World 456");
        var regex = new PcreRegex(@"\d+");
        sb.ReplaceAll(regex, "XXX".AsSpan());
        Assert.Equal("Hello XXX World XXX", sb.ToString());
    }
#endif
    #endregion

#if NET6_0_OR_GREATER
    #region ISpanFormattable Tests
    [Fact]
    public void Append_SpanFormattable_AppendsFormatted()
    {
        var sb = new StringBuffer("Value: ");
        sb.Append(123.456, "F2".AsSpan());
        Assert.Equal("Value: 123.46", sb.ToString());
    }

    [Fact]
    public void Append_SpanFormattableWithProvider_UsesProvider()
    {
        var sb = new StringBuffer("Date: ");
        var date = new DateTime(2024, 1, 15);
        sb.Append(date, "d".AsSpan(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("Date: 01/15/2024", sb.ToString());
    }
    #endregion
#endif

    #region Edge Cases and Stress Tests
    [Fact]
    public void LargeBuffer_HandlesCorrectly()
    {
        var sb = new StringBuffer();
        var largeString = new string('X', 10000);
        sb.Append(largeString.AsSpan());

        Assert.Equal(10000, sb.Length);
        Assert.Equal(largeString, sb.ToString());
    }

    [Fact]
    public void MultipleGrows_MaintainsContent()
    {
        var sb = new StringBuffer(2);
        for (var i = 0; i < 100; i++)
        {
            sb.Append($"Item{i} ".AsSpan());
        }

        Assert.Contains("Item0", sb.ToString());
        Assert.Contains("Item99", sb.ToString());
    }

    [Fact]
    public void ComplexOperationSequence_ProducesCorrectResult()
    {
        var sb = new StringBuffer("  Hello World  ");
        sb.Trim(' ');
        sb.Replace("World".AsSpan(), "Universe".AsSpan());
        sb.Append("!");
        sb.ReplaceAll('l', 'w');

        Assert.Equal("Hewwo Universe!", sb.ToString());
    }

    [Fact]
    public void ReplaceAll_OverlappingPatterns_HandlesCorrectly()
    {
        var sb = new StringBuffer("aaaa");
        var chars = new char[] { 'a', 'a' };
        sb.ReplaceAll(chars.AsSpan(), chars.AsSpan());
        Assert.Equal("aaaa", sb.ToString());
    }

    [Fact]
    public void EmptyBuffer_OperationsHandleGracefully()
    {
        var sb = new StringBuffer();

        sb.Trim(' ');
        sb.TrimStart('x');
        sb.TrimEnd('y');
        sb.Clear();

        Assert.Equal(-1, sb.IndexOf('a'));
        Assert.Equal(string.Empty, sb.ToString());
    }
    #endregion
}