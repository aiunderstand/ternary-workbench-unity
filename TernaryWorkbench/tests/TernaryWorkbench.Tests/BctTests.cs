using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for BCT (Binary-Coded Ternary) encoding using the BSD-PNX scheme:
///   "10" = + (plus trit), "01" = - (minus trit), "11" = 0 (zero trit), "00" = illegal.
/// BCT is only available when the target radix is Base3Balanced.
/// </summary>
public class BctTests
{
    private static readonly OutputOptions BctOpts = new(BctEncoding: true);
    private static readonly OutputOptions BctLsdOpts = new(LsdFirst: true, BctEncoding: true);

    // -----------------------------------------------------------------------
    // Single-trit encoding
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0,  "11")]   // 0 → "0" in balanced ternary → "11"
    [InlineData(1,  "10")]   // 1 → "+" → "10"
    [InlineData(-1, "01")]   // -1 → "-" → "01"
    public void BctEncoding_SingleTrit(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Balanced, BctOpts);
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // Multi-trit encoding
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(4,  "1010")]        // "++" → "10"+"10"
    [InlineData(-4, "0101")]        // "--" → "01"+"01"
    [InlineData(13, "101010")]      // "+++" → "10"+"10"+"10"
    [InlineData(-13,"010101")]      // "---" → "01"+"01"+"01"
    [InlineData(3,  "1011")]        // "+0" → "10"+"11"
    [InlineData(2,  "1001")]        // "+-" → "10"+"01"
    [InlineData(42, "1001010111")]  // "+---0" → "10"+"01"+"01"+"01"+"11"
    [InlineData(-42,"0110101011")]  // "-+++0" → "01"+"10"+"10"+"10"+"11"
    public void BctEncoding_MultiTrit(long input, string expected)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Balanced, BctOpts);
        result.Should().Be(expected);
    }

    [Fact]
    public void BctEncoding_LsdFirst_42()
    {
        // 42 → balanced "+---0" → BCT MSD "10 01 01 01 11"
        // LSD-first → pairs reversed: "11 01 01 01 10" = "1101010110"
        var result = RadixConverter.Convert("42", Radix.Base10, Radix.Base3Balanced, BctLsdOpts);
        result.Should().Be("1101010110");
    }

    [Fact]
    public void BctEncoding_LsdFirst_Minus1()
    {
        // -1 → balanced "-" → BCT "01"
        // LSD-first of single pair: "01" (only one pair, reversing is same)
        var result = RadixConverter.Convert("-1", Radix.Base10, Radix.Base3Balanced, BctLsdOpts);
        result.Should().Be("01");
    }

    // -----------------------------------------------------------------------
    // SupportsBct: only Base3Balanced
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(Radix.Base3Balanced, true)]
    [InlineData(Radix.Base3Unbalanced, false)]
    [InlineData(Radix.Base3Signed2C, false)]
    [InlineData(Radix.Base3Signed3C, false)]
    [InlineData(Radix.Base2Unsigned, false)]
    [InlineData(Radix.Base10, false)]
    [InlineData(Radix.Base9Unbalanced, false)]
    public void SupportsBct_OnlyBalancedTernary(Radix radix, bool expected)
    {
        radix.SupportsBct().Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // BCT output length is always even (2 bits per trit)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(-364)]
    [InlineData(364)]
    public void BctEncoding_OutputLengthIsEven(long input)
    {
        var result = RadixConverter.Convert(input.ToString(), Radix.Base10, Radix.Base3Balanced, BctOpts);
        (result.Length % 2).Should().Be(0, because: "each trit encodes to exactly 2 bits");
    }
}
