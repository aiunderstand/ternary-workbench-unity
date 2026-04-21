using System.Text;
using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="CharTu8Codec"/> encoding (UTF-8 → charT_u8 ternary).
/// </summary>
public class CharTu8CodecEncodeTests
{
    // -------------------------------------------------------------------------
    // Basic round-trips (encode then decode)
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_EmptyString_ReturnsEmptyResult()
    {
        var result = CharTu8Codec.Encode("");
        result.EncodedText.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Encode_NullString_ReturnsEmptyResult()
    {
        var result = CharTu8Codec.Encode(null);
        result.EncodedText.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Hello")]
    [InlineData("Hello, World!")]
    [InlineData("abc 123")]
    [InlineData("\x00")]    // NUL
    [InlineData("\t\n\r")]  // control chars
    public void Encode_AsciiString_RoundTrips(string input)
    {
        var encoded = CharTu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty();
        var decoded = CharTu8Codec.Decode(encoded.EncodedText);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be(input);
    }

    [Theory]
    [InlineData("~")]    // CP 126 — first 2-tryte
    [InlineData("\x7F")] // DEL — second 2-tryte
    public void Encode_2ByteAsciiChars_RoundTrips(string input)
    {
        var encoded = CharTu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty();
        // Each symbol should occupy 2 trytes (2 × 6 = 12 chars + 1 space)
        encoded.EncodedText.Trim().Should().HaveLength(13, "2-tryte = 2×6 chars + 1 space");
        var decoded = CharTu8Codec.Decode(encoded.EncodedText);
        decoded.DecodedText.Should().Be(input);
    }

    // -------------------------------------------------------------------------
    // 1-tryte encoding
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_AllAscii0To125_UsesSingleTryte()
    {
        for (int cp = 0; cp <= 125; cp++)
        {
            string s = char.ConvertFromUtf32(cp);
            var encoded = CharTu8Codec.Encode(s);
            encoded.Errors.Should().BeEmpty();
            // 1-tryte = exactly 6 chars with no space
            encoded.EncodedText.Should().HaveLength(6,
                because: $"CP {cp} must encode to a single 6-trit tryte");
        }
    }

    [Fact]
    public void Encode_AllAscii0To125_ProducesUniquePatterns()
    {
        var patterns = new HashSet<string>();
        for (int cp = 0; cp <= 125; cp++)
        {
            string s = char.ConvertFromUtf32(cp);
            var encoded = CharTu8Codec.Encode(s);
            patterns.Add(encoded.EncodedText).Should().BeTrue(
                because: $"CP {cp} produced a duplicate pattern");
        }
        patterns.Should().HaveCount(126);
    }

    // -------------------------------------------------------------------------
    // Range boundaries
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(125)] // last 1-tryte
    [InlineData(126)] // first 2-tryte
    [InlineData(CharTu8StandardTable.MaxCp2Tryte)]     // last 2-tryte
    [InlineData(CharTu8StandardTable.MinCp3Tryte)]     // first 3-tryte
    [InlineData(CharTu8StandardTable.MaxCp3Tryte)]     // last 3-tryte
    [InlineData(CharTu8StandardTable.MinCp4Tryte)]     // first 4-tryte
    public void Encode_BoundaryCodePoints_RoundTrip(int cp)
    {
        if (!Rune.IsValid(cp)) return; // skip surrogate/invalid
        string input = new Rune(cp).ToString();
        var encoded = CharTu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty(because: $"CP {cp} should encode cleanly");
        var decoded = CharTu8Codec.Decode(encoded.EncodedText);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be(input);
    }

    // -------------------------------------------------------------------------
    // Multi-character strings
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_MultiChar_SymbolsOnSeparateLines()
    {
        var result = CharTu8Codec.Encode("AB");
        var lines = result.EncodedText.Split('\n');
        lines.Should().HaveCount(2, "each character is on its own line");
    }

    [Fact]
    public void Encode_UnicodeEmojiAndBmp_RoundTrips()
    {
        // U+1F600 😀 is above BMP, U+00E9 é is BMP
        string input = "\u00E9\U0001F600";
        var encoded = CharTu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty();
        var decoded = CharTu8Codec.Decode(encoded.EncodedText);
        decoded.DecodedText.Should().Be(input);
    }

    // -------------------------------------------------------------------------
    // Encoded tryte structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_1TryteSymbol_HasCorrectLeadTrit()
    {
        var result = CharTu8Codec.Encode("A"); // CP 65
        result.EncodedText[0].Should().Be('+', "all 1-tryte patterns start with '+'");
    }

    [Fact]
    public void Encode_2TryteSymbol_HasPlusMinusLead()
    {
        var result = CharTu8Codec.Encode("~"); // CP 126, first 2-tryte
        var trytes = result.EncodedText.Trim().Split(' ');
        trytes.Should().HaveCount(2);
        trytes[0][0].Should().Be('+');
        trytes[0][1].Should().Be('-'); // Lead2 prefix
        trytes[1][0].Should().Be('-'); // Final continuation
    }

    [Fact]
    public void Encode_3TryteSymbol_HasCorrectMarkers()
    {
        // CP MinCp3Tryte → 3-tryte
        string input = new Rune(CharTu8StandardTable.MinCp3Tryte).ToString();
        var result = CharTu8Codec.Encode(input);
        var trytes = result.EncodedText.Trim().Split(' ');
        trytes.Should().HaveCount(3);
        trytes[0].StartsWith("++").Should().BeTrue();
        trytes[0][2].Should().Be('-'); // Lead3 marker
        trytes[2][0].Should().Be('-'); // Final continuation
    }
}

/// <summary>
/// Tests for <see cref="CharTu8Codec"/> decoding (charT_u8 ternary → UTF-8).
/// </summary>
public class CharTu8CodecDecodeTests
{
    private static string EncodeClean(string s) => CharTu8Codec.Encode(s).EncodedText;

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("A")]
    [InlineData("Hello, World!")]
    [InlineData("~")]
    [InlineData("\u00E9")]  // U+00E9 é
    public void Decode_ValidTernary_ReturnsCorrectString(string expected)
    {
        string ternary = EncodeClean(expected);
        var result = CharTu8Codec.Decode(ternary);
        result.Errors.Should().BeEmpty();
        result.DecodedText.Should().Be(expected);
    }

    [Fact]
    public void Decode_EmptyInput_ReturnsEmpty()
    {
        var result = CharTu8Codec.Decode("");
        result.DecodedText.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Self-synchronization
    // -------------------------------------------------------------------------

    [Fact]
    public void Decode_CorruptMiddleTryte_RecoversSurroundingChars()
    {
        // Encode "A?B" where ? is some mid-stream corruption
        string encodedA = EncodeClean("A"); // 1-tryte
        string encodedB = EncodeClean("B"); // 1-tryte
        // A valid single-tryte for a continuation would corrupt the stream
        // Insert a lone Final continuation tryte (starts with '-') between A and B
        string corrupt = encodedA + "\n------\n" + encodedB;
        var result = CharTu8Codec.Decode(corrupt);
        // Should report an error for the stray continuation, but decode A and B
        result.Errors.Should().NotBeEmpty();
        result.DecodedText.Should().Contain("A");
        result.DecodedText.Should().Contain("B");
    }

    // -------------------------------------------------------------------------
    // Full round-trip of all 126 single-tryte code points
    // -------------------------------------------------------------------------

    [Fact]
    public void RoundTrip_AllSingleTryteCodePoints()
    {
        for (int cp = 0; cp <= 125; cp++)
        {
            string s = char.ConvertFromUtf32(cp);
            string ternary = EncodeClean(s);
            var result = CharTu8Codec.Decode(ternary);
            result.Errors.Should().BeEmpty(because: $"CP {cp} should decode cleanly");
            result.DecodedText.Should().Be(s);
        }
    }
}
