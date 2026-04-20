using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>Unit tests for specific known input/output pairs.</summary>
public class RadixConverterTests
{
    // -----------------------------------------------------------------------
    // Decimal → Binary unsigned
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(5, "101")]
    [InlineData(255, "11111111")]
    public void Decimal_To_BinaryUnsigned(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base2Unsigned);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Binary 1's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "01")]
    [InlineData(2, "010")]
    [InlineData(3, "011")]
    [InlineData(-1, "10")]
    [InlineData(-2, "101")]
    [InlineData(-3, "100")]
    [InlineData(-4, "1011")]
    [InlineData(7, "0111")]
    [InlineData(-7, "1000")]
    public void Decimal_To_BinaryOneSC(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base2Signed1C);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Binary two's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "01")]
    [InlineData(-1, "11")]
    [InlineData(7, "0111")]
    [InlineData(-8, "11000")]
    [InlineData(-4, "1100")]
    public void Decimal_To_BinarySigned2C(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base2Signed2C);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Octal
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(7, "7")]
    [InlineData(8, "10")]
    [InlineData(64, "100")]
    [InlineData(255, "377")]
    [InlineData(512, "1000")]
    public void Decimal_To_Octal(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base8Unsigned);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Hexadecimal
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(15, "F")]
    [InlineData(16, "10")]
    [InlineData(255, "FF")]
    [InlineData(256, "100")]
    [InlineData(65535, "FFFF")]
    public void Decimal_To_Hex(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base16Unsigned);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Hex parse: case-insensitive
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("ff", "255")]
    [InlineData("FF", "255")]
    [InlineData("aB", "171")]
    public void Hex_ParseCaseInsensitive(string hex, string expectedDecimal)
    {
        var result = RadixConverter.Convert(hex, Radix.Base16Unsigned, Radix.Base10);
        result.Should().Be(expectedDecimal);
    }

    // -----------------------------------------------------------------------
    // Decimal → Base-64 (RFC 4648)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "A")]   // 'A' is digit 0 in RFC 4648
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "a")]
    [InlineData(51, "z")]
    [InlineData(52, "0")]
    [InlineData(61, "9")]
    [InlineData(62, "+")]
    [InlineData(63, "/")]
    [InlineData(64, "BA")]  // 64 = 1*64 + 0
    public void Decimal_To_Base64(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base64Rfc4648);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Balanced ternary
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "+")]
    [InlineData(-1, "-")]
    [InlineData(2, "+-")]
    [InlineData(-2, "-+")]
    [InlineData(3, "+0")]
    [InlineData(4, "++")]
    [InlineData(42, "+---0")]
    [InlineData(-42, "-+++0")]
    [InlineData(13, "+++")]
    [InlineData(-13, "---")]
    public void Decimal_To_BalancedTernary(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Balanced);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Ternary (unbalanced)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(2, "2")]
    [InlineData(3, "10")]
    [InlineData(9, "100")]
    [InlineData(42, "1120")]
    public void Decimal_To_TernaryUnbalanced(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Unbalanced);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Ternary 2's complement (diminished radix complement)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "01")]
    [InlineData(2, "02")]
    [InlineData(3, "010")]
    [InlineData(-1, "21")]   // flip "1" → "1", sign "2": 2+1="21"
    [InlineData(-2, "20")]   // flip "2" → "0", sign "2": "20"
    [InlineData(-3, "212")]  // flip "10" → "12" (each trit: 1→1, 0→2), sign "2": "212"
    [InlineData(-4, "211")]  // flip "11" → "11", sign "2": "211"
    public void Decimal_To_Ternary2C(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Signed2C);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Ternary 3's complement (radix complement)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "01")]
    [InlineData(2, "02")]
    [InlineData(3, "010")]
    [InlineData(-1, "1")]    // sign=1, L=0, rest=0
    [InlineData(-2, "11")]   // sign=1, L=1, rest=1
    [InlineData(-3, "10")]   // sign=1, L=1, rest=0 (3^1=3)
    [InlineData(-4, "112")]  // sign=1, L=2, rest=5="12"
    [InlineData(-9, "100")]  // sign=1, L=2, rest=0 (3^2=9)
    public void Decimal_To_Ternary3C(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Signed3C);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Nonary (base 9)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(8, "8")]
    [InlineData(9, "10")]
    [InlineData(80, "88")]
    [InlineData(81, "100")]
    public void Decimal_To_Base9(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base9Unbalanced);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Decimal → Base-27 (Heptavintimal)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(26, "Q")]
    [InlineData(27, "10")]
    [InlineData(53, "1Q")]
    public void Decimal_To_Base27(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base27Unbalanced);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // LSD-first output
    // -----------------------------------------------------------------------

    [Fact]
    public void LsdFirst_ReversesDigits()
    {
        var opts = new OutputOptions(LsdFirst: true);
        var result = RadixConverter.Convert("42", Radix.Base10, Radix.Base3Balanced, opts);
        result.Should().Be("0---+");
    }

    [Fact]
    public void LsdFirst_NegativeValue_PreservesSign()
    {
        var opts = new OutputOptions(LsdFirst: true);
        var result = RadixConverter.Convert("-42", Radix.Base10, Radix.Base3Balanced, opts);
        result.Should().Be("0+++-");
    }

    // -----------------------------------------------------------------------
    // Symbol alphabet sanity
    // -----------------------------------------------------------------------

    [Fact]
    public void Base64Alphabet_HasExactly64Characters()
    {
        Symbols.Base64Rfc4648Alphabet.Length.Should().Be(64);
    }

    [Fact]
    public void Base64Alphabet_HasNoDuplicates()
    {
        Symbols.Base64Rfc4648Alphabet.Distinct().Count().Should().Be(64);
    }

    [Fact]
    public void Base27Alphabet_HasExactly27Characters()
    {
        Symbols.Base27Alphabet.Length.Should().Be(27);
    }

    // -----------------------------------------------------------------------
    // Round-trip: Binary 1's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("01")]
    [InlineData("10")]
    [InlineData("011")]
    [InlineData("100")]
    [InlineData("0111")]
    [InlineData("1000")]
    public void Binary1C_RoundTrip(string bits)
    {
        var dec = RadixConverter.Convert(bits, Radix.Base2Signed1C, Radix.Base10);
        var back = RadixConverter.Convert(dec, Radix.Base10, Radix.Base2Signed1C);
        back.Should().Be(bits);
    }

    // -----------------------------------------------------------------------
    // Round-trip: Binary two's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("01")]
    [InlineData("11")]
    [InlineData("0111")]
    [InlineData("11000")]
    public void TwosComplement_RoundTrip(string bits)
    {
        var dec = RadixConverter.Convert(bits, Radix.Base2Signed2C, Radix.Base10);
        var back = RadixConverter.Convert(dec, Radix.Base10, Radix.Base2Signed2C);
        back.Should().Be(bits);
    }

    // -----------------------------------------------------------------------
    // Round-trip: Ternary 2's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("0")]
    [InlineData("01")]
    [InlineData("21")]
    [InlineData("020")]
    [InlineData("212")]
    public void Ternary2C_RoundTrip(string trits)
    {
        var dec = RadixConverter.Convert(trits, Radix.Base3Signed2C, Radix.Base10);
        var back = RadixConverter.Convert(dec, Radix.Base10, Radix.Base3Signed2C);
        back.Should().Be(trits);
    }

    // -----------------------------------------------------------------------
    // Round-trip: Ternary 3's complement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("0")]
    [InlineData("01")]
    [InlineData("1")]
    [InlineData("11")]
    [InlineData("010")]
    [InlineData("10")]
    [InlineData("112")]
    public void Ternary3C_RoundTrip(string trits)
    {
        var dec = RadixConverter.Convert(trits, Radix.Base3Signed3C, Radix.Base10);
        var back = RadixConverter.Convert(dec, Radix.Base10, Radix.Base3Signed3C);
        back.Should().Be(trits);
    }

    // -----------------------------------------------------------------------
    // Unsigned radix: negative input should throw
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(Radix.Base2Unsigned)]
    [InlineData(Radix.Base8Unsigned)]
    [InlineData(Radix.Base16Unsigned)]
    [InlineData(Radix.Base64Rfc4648)]
    [InlineData(Radix.Base3Unbalanced)]
    [InlineData(Radix.Base9Unbalanced)]
    [InlineData(Radix.Base27Unbalanced)]
    public void UnsignedRadix_NegativeInput_Throws(Radix radix)
    {
        var act = () => RadixConverter.Convert("-1", Radix.Base10, radix);
        act.Should().Throw<OverflowException>();
    }
}
