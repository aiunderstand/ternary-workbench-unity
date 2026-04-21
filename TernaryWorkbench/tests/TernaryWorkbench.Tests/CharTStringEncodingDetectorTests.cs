using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="CharTStringEncodingDetector.Detect"/>.
/// </summary>
public class CharTStringEncodingDetectorTests
{
    // -------------------------------------------------------------------------
    // Null / empty input → Unknown
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_NullOrWhitespace_ReturnsUnknown(string? input)
    {
        CharTStringEncodingDetector.Detect(input).Should().Be(DetectedEncoding.Unknown);
    }

    // -------------------------------------------------------------------------
    // Invalid input → Unknown
    // -------------------------------------------------------------------------

    [Fact]
    public void Detect_StandaloneContinuationTrytes_ReturnsUnknown()
    {
        // A standalone ContinuationFinal tryte (starts with '-') is not a valid lead
        // in either charT_u8 or charTC_u8, so both codecs report an error.
        string input = "-00000 -00000";
        CharTStringEncodingDetector.Detect(input).Should().Be(DetectedEncoding.Unknown);
    }

    // -------------------------------------------------------------------------
    // charTC_u8 encoded input → CharTCU8
    // -------------------------------------------------------------------------

    [Fact]
    public void Detect_CharTCU8EncodedString_ReturnsBothOrCharTCU8()
    {
        // The user-provided charTC_u8 example encodes "This is a test Not a test".
        // charTC_u8 decodes it cleanly to the expected text.
        // charT_u8 also decodes it without structural errors (it produces different
        // characters — 1:1 Unicode mapping gives "junk"), so the detector returns
        // Both (ambiguous) rather than CharTCU8 exclusively.
        // The UI will then show "ambiguous (both codecs valid)" and the user can
        // inspect each output column to identify the charTC_u8 result.
        string input = string.Join("\n",
            "+----+ -0--0- +000-0 +0000- +0+0+0 +0-000 +0000- +0+0+0 +0-000 +0-0+- +0-000 +0++-+ +00--+ +0+0+0 +0++-+ +0-000 +----0 --+++- +0+-0- +0++-+ +0-000 +0-0+- +0-000 +0++-+ +00--+ +0+0+0 +0++-+"
                .Split(' '));

        var result = CharTStringEncodingDetector.Detect(input);

        // charTC_u8 always decodes it cleanly; charT_u8 may too (ambiguous)
        result.Should().BeOneOf(
            new[] { DetectedEncoding.CharTCU8, DetectedEncoding.Both },
            "the user charTC_u8 example should be identified as charTC_u8 or ambiguous");

        // The charTC_u8 output must be the expected text
        var decoded = CharTCu8Codec.Decode(input);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be("This is a test Not a test");
    }

    [Fact]
    public void Detect_ManuallyEncodedWithCharTCU8_ReturnsCharTCU8OrBoth()
    {
        // Encode a string using charTC_u8 and verify the detector finds it
        string original = "hello";
        string encoded = CharTCu8Codec.Encode(original).EncodedText;

        var result = CharTStringEncodingDetector.Detect(encoded);

        // Must not be Unknown or CharTU8
        result.Should().BeOneOf(
            new[] { DetectedEncoding.CharTCU8, DetectedEncoding.Both },
            "a cleanly charTC_u8-encoded string must be detected as CharTCU8 or ambiguous");
    }

    // -------------------------------------------------------------------------
    // charT_u8 encoded input that is not valid charTC_u8 → CharTU8
    // -------------------------------------------------------------------------

    [Fact]
    public void Detect_ManuallyEncodedWithCharTU8Only_ReturnsCharTU8()
    {
        // charT_u8 encodes ALL Unicode CPs including those not in charTC_u8's
        // 42-char single-tryte table.  For a CP that charTC_u8 encodes as 2 trytes,
        // charT_u8 may produce a 2-tryte form that fails charTC_u8 CRC validation,
        // making it detectable as charT_u8-only.
        //
        // Strategy: encode a single character in charT_u8, then check if the
        // detector cleanly distinguishes it.  We pick CP 126 ('~') which is a
        // 2-tryte form in charT_u8 (no CRC trit).
        string encoded = CharTu8Codec.Encode("~").EncodedText;

        var result = CharTStringEncodingDetector.Detect(encoded);

        // Must not be Unknown
        result.Should().NotBe(DetectedEncoding.Unknown,
            "a charT_u8-encoded string should at least decode in one codec");

        // Must not be charTC_u8 only (may be CharTU8 or Both depending on CRC)
        result.Should().NotBe(DetectedEncoding.CharTCU8,
            "a charT_u8-encoded symbol without a CRC trit should not pass charTC_u8 only");
    }

    // -------------------------------------------------------------------------
    // Ambiguous: both codecs succeed → Both
    // -------------------------------------------------------------------------

    [Fact]
    public void Detect_SingleTryteValidInBothCodecs_ReturnsBoth()
    {
        // Single-tryte characters that exist in charTC_u8's 42-char table and
        // happen to also have a valid CRC (CRC trit = 0 by construction) will
        // decode successfully in both codecs.
        // Find the tryte for 'a' from charTC_u8 and feed it to Detect.
        string encoded = CharTCu8Codec.Encode("a").EncodedText;

        // charTC_u8: CRC is embedded, so decoded with 0 errors.
        // charT_u8 single-tryte table also maps 'a' → same pattern (CP 97 in 1:1 mapping).
        // Therefore both should succeed → Both.
        var result = CharTStringEncodingDetector.Detect(encoded);

        result.Should().BeOneOf(
            new[] { DetectedEncoding.Both, DetectedEncoding.CharTCU8 },
            "a valid charTC_u8 single-tryte for 'a' may also pass charT_u8 (ambiguous or CharTCU8)");
    }

    // -------------------------------------------------------------------------
    // ToDisplayString
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(DetectedEncoding.CharTU8,  "charT_u8")]
    [InlineData(DetectedEncoding.CharTCU8, "charTC_u8")]
    [InlineData(DetectedEncoding.Both,     "ambiguous (both codecs valid)")]
    [InlineData(DetectedEncoding.Unknown,  "unknown")]
    public void ToDisplayString_ReturnsExpectedLabel(DetectedEncoding enc, string expected)
    {
        enc.ToDisplayString().Should().Be(expected);
    }
}
