using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="CharTCu8Codec"/> — encoding, decoding, CRC invariant.
/// </summary>
public class CharTCu8CodecTests
{
    private static System.Text.Rune MakeRune(int cp) => new System.Text.Rune(cp);

    // -------------------------------------------------------------------------
    // 42-char single-tryte round-trips
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]   // NUL
    [InlineData(9)]   // TAB
    [InlineData(10)]  // LF
    [InlineData(13)]  // CR
    [InlineData(32)]  // SPC
    [InlineData('a')]
    [InlineData('z')]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('.')]
    public void Encode_SingleTryteChar_RoundTrips(int unicodeCP)
    {
        string input = char.ConvertFromUtf32(unicodeCP);
        var encoded = CharTCu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty();
        encoded.EncodedText.Should().HaveLength(6, $"CP {unicodeCP} must map to a single 6-trit tryte");

        var decoded = CharTCu8Codec.Decode(encoded.EncodedText);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be(input);
    }

    [Fact]
    public void Encode_All42SingleTryteChars_RoundTrip()
    {
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
        {
            int cp = entry.UnicodeCodePoint ?? -1;
            if (cp < 0) continue;
            string input = char.ConvertFromUtf32(cp);
            var encoded = CharTCu8Codec.Encode(input);
            encoded.Errors.Should().BeEmpty(because: $"CP {cp} (charTC #{entry.CodePoint}) should encode cleanly");
            encoded.EncodedText.Should().HaveLength(6);

            var decoded = CharTCu8Codec.Decode(encoded.EncodedText);
            decoded.Errors.Should().BeEmpty();
            decoded.DecodedText.Should().Be(input);
        }
    }

    // -------------------------------------------------------------------------
    // CRC invariant: balanced sum ≡ 0 (mod 3) for every encoded symbol
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_SingleTryteChars_AllHaveZeroModSum()
    {
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
        {
            var tryte = Tryte.Parse(entry.TrytePattern);
            int sum = tryte.BalancedSum;
            (((sum % 3) + 3) % 3).Should().Be(0,
                because: $"charTC CP {entry.CodePoint} '{entry.TrytePattern}' sum={sum} must be ≡ 0 (mod 3)");
        }
    }

    [Theory]
    [InlineData('A')]      // multi-tryte char
    [InlineData('~')]      // CP 126
    [InlineData('\u00E9')] // é  BMP
    public void Encode_MultiTryte_AllTrytesHaveZeroModSum(char c)
    {
        var encoded = CharTCu8Codec.Encode(c.ToString());
        encoded.Errors.Should().BeEmpty();

        // Split symbol into trytes; each line is one symbol (one or more space-separated trytes)
        foreach (var line in encoded.EncodedText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trytes = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int totalSum = trytes.Sum(t => Tryte.Parse(t).BalancedSum);
            (((totalSum % 3) + 3) % 3).Should().Be(0,
                because: $"symbol '{c}' encoded as '{line}' total balanced sum must be ≡ 0 (mod 3)");
        }
    }

    // -------------------------------------------------------------------------
    // CRC error detection
    // -------------------------------------------------------------------------

    [Fact]
    public void Decode_SingleTritMutation_TriggersCrcError()
    {
        // Encode 'a' — a 1-tryte charTC symbol with known CRC
        var encoded = CharTCu8Codec.Encode("a");
        string pattern = encoded.EncodedText.Trim();

        // Flip the last trit (CRC trit at position 5)
        char lastTrit = pattern[5];
        char corrupted = lastTrit == '+' ? '-' : '+';
        string mutated = pattern[..5] + corrupted;

        var decoded = CharTCu8Codec.Decode(mutated);
        decoded.Errors.Should().NotBeEmpty("a mutated CRC trit must be detected");
        decoded.Errors.Should().Contain(e => e.Message.Contains("CRC"),
            because: "error should mention CRC");
    }

    [Fact]
    public void Decode_MultiTryte_CrcErrorOnMutation()
    {
        // Encode 'A' (CP 65, multi-tryte in charTC_u8)
        var encoded = CharTCu8Codec.Encode("A");
        string line = encoded.EncodedText.Trim();
        var trytes = line.Split(' ').ToList();

        // Mutate the last trit of the lead tryte
        string lead = trytes[0];
        char flipped = lead[5] == '+' ? '-' : '+';
        trytes[0] = lead[..5] + flipped;

        string mutated = string.Join(" ", trytes);
        var decoded = CharTCu8Codec.Decode(mutated);
        decoded.Errors.Should().NotBeEmpty();
    }

    // -------------------------------------------------------------------------
    // Multi-character strings
    // -------------------------------------------------------------------------

    [Fact]
    public void Encode_MultiChar_AllCharsRoundTrip()
    {
        string input = "hello world 123.";
        var encoded = CharTCu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty();
        var decoded = CharTCu8Codec.Decode(encoded.EncodedText);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be(input);
    }

    [Fact]
    public void Encode_EmptyString_ReturnsEmpty()
    {
        var r = CharTCu8Codec.Encode("");
        r.EncodedText.Should().BeEmpty();
        r.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Encode_NullString_ReturnsEmpty()
    {
        var r = CharTCu8Codec.Encode(null);
        r.EncodedText.Should().BeEmpty();
        r.Errors.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Range boundary round-trips
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(CharTCu8StandardTable.MaxCp2Tryte)]    // last 2-tryte CP
    [InlineData(CharTCu8StandardTable.MinCp3Tryte)]    // first 3-tryte CP
    [InlineData(CharTCu8StandardTable.MaxCp3Tryte)]    // last 3-tryte CP
    [InlineData(CharTCu8StandardTable.MinCp4Tryte)]    // first 4-tryte CP
    public void Encode_BoundaryCPs_RoundTrip(int cp)
    {
        if (!System.Text.Rune.IsValid(cp)) return;
        string input = System.Text.Rune.GetRuneAt(char.ConvertFromUtf32(cp), 0).ToString();
        var encoded = CharTCu8Codec.Encode(input);
        encoded.Errors.Should().BeEmpty(because: $"CP {cp} boundary should encode cleanly");
        var decoded = CharTCu8Codec.Decode(encoded.EncodedText);
        decoded.Errors.Should().BeEmpty();
        decoded.DecodedText.Should().Be(input);
    }
}
