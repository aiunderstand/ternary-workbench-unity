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
    // Decimal → Base-9
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
    // Decimal → Base-27
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
    // Decimal → Base-81
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, "0")]
    [InlineData(80, ";")]   // last symbol
    [InlineData(81, "10")]
    public void Decimal_To_Base81(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base81Unbalanced);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // LSD-first output
    // -----------------------------------------------------------------------

    [Fact]
    public void LsdFirst_ReversesDigits()
    {
        var opts = new OutputOptions(LsdFirst: true);
        // 42 in balanced ternary MSD = "+---0", LSD = "0---+"
        var result = RadixConverter.Convert("42", Radix.Base10, Radix.Base3Balanced, opts);
        result.Should().Be("0---+");
    }

    [Fact]
    public void LsdFirst_NegativeValue_PreservesSign()
    {
        // -42 in balanced ternary MSD = "-+++0", LSD = "0+++-"
        var opts = new OutputOptions(LsdFirst: true);
        var result = RadixConverter.Convert("-42", Radix.Base10, Radix.Base3Balanced, opts);
        // Balanced ternary has no explicit leading '-'; the sign is embedded
        result.Should().Be("0+++-");
    }

    // -----------------------------------------------------------------------
    // BCD encoding
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("42", "011002")]  // 4→"011", 2→"002"
    [InlineData("10", "001000")]  // 1→"001", 0→"000"
    [InlineData("9",  "100")]     // 9→"100"
    public void BcdEncoding_Base3(string input, string expected)
    {
        var opts = new OutputOptions(BcdEncoding: true);
        var result = RadixConverter.Convert(input, Radix.Base10, Radix.Base3Unbalanced, opts);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("42", "0402")]  // 4→"04", 2→"02"
    [InlineData("9",  "10")]    // 9→"10" (base9)
    public void BcdEncoding_Base9(string input, string expected)
    {
        var opts = new OutputOptions(BcdEncoding: true);
        var result = RadixConverter.Convert(input, Radix.Base10, Radix.Base9Unbalanced, opts);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("42", "42")]  // each digit is its own base-27 char (0-9 map to themselves)
    public void BcdEncoding_Base27(string input, string expected)
    {
        var opts = new OutputOptions(BcdEncoding: true);
        var result = RadixConverter.Convert(input, Radix.Base10, Radix.Base27Unbalanced, opts);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Round-trip: Base2Signed2C
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
    // Symbol alphabet sanity
    // -----------------------------------------------------------------------

    [Fact]
    public void Base81Alphabet_HasExactly81Characters()
    {
        Symbols.Base81Alphabet.Length.Should().Be(81);
    }

    [Fact]
    public void Base81Alphabet_HasNoDuplicates()
    {
        Symbols.Base81Alphabet.Distinct().Count().Should().Be(81);
    }

    [Fact]
    public void Base27Alphabet_HasExactly27Characters()
    {
        Symbols.Base27Alphabet.Length.Should().Be(27);
    }
}
