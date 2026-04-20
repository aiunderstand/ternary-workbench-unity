using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Exhaustive round-trip tests for all values with ≤6 trits.
/// 3^6 = 729 values: unbalanced 0..728, balanced −364..364.
/// </summary>
public class RoundTripTests
{
    private const int MaxTrits = 6;

    // 3^6 = 729
    private static readonly int Pow3_6 = (int)Math.Pow(3, MaxTrits);

    // -----------------------------------------------------------------------
    // Ternary unbalanced: 0 .. 3^6-1
    // -----------------------------------------------------------------------

    [Fact]
    public void TernaryUnbalanced_ExhaustiveRoundTrip_ViaDecimal()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string ter = RadixConverter.Format(new BigInteger(v), Radix.Base3Unbalanced);
            string back = RadixConverter.Convert(ter, Radix.Base3Unbalanced, Radix.Base10);
            back.Should().Be(v.ToString(), because: $"ternary '{ter}' should parse back to {v}");
        }
    }

    [Fact]
    public void TernaryUnbalanced_ExhaustiveRoundTrip_FromDecimal()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string ter = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base3Unbalanced);
            string dec = RadixConverter.Convert(ter, Radix.Base3Unbalanced, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"{v} → ternary '{ter}' → decimal should be {v}");
        }
    }

    // -----------------------------------------------------------------------
    // Balanced ternary: −364 .. +364  (range of 6 trits)
    // -----------------------------------------------------------------------

    private static readonly int BalancedMax = (Pow3_6 - 1) / 2;  // 364

    [Fact]
    public void BalancedTernary_ExhaustiveRoundTrip_ViaDecimal()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string bal = RadixConverter.Format(new BigInteger(v), Radix.Base3Balanced);
            string back = RadixConverter.Convert(bal, Radix.Base3Balanced, Radix.Base10);
            back.Should().Be(v.ToString(), because: $"balanced '{bal}' should parse back to {v}");
        }
    }

    [Fact]
    public void BalancedTernary_ExhaustiveRoundTrip_FromDecimal()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string bal = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base3Balanced);
            string dec = RadixConverter.Convert(bal, Radix.Base3Balanced, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"{v} → balanced '{bal}' → decimal should be {v}");
        }
    }

    // -----------------------------------------------------------------------
    // Base-9 round-trip: 0 .. 3^6-1
    // -----------------------------------------------------------------------

    [Fact]
    public void Base9_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string b9 = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base9Unbalanced);
            string dec = RadixConverter.Convert(b9, Radix.Base9Unbalanced, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    // -----------------------------------------------------------------------
    // Base-27 round-trip: 0 .. 3^6-1
    // -----------------------------------------------------------------------

    [Fact]
    public void Base27_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string b27 = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base27Unbalanced);
            string dec = RadixConverter.Convert(b27, Radix.Base27Unbalanced, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    // -----------------------------------------------------------------------
    // Base-81 round-trip: 0 .. 3^6-1
    // -----------------------------------------------------------------------

    [Fact]
    public void Base81_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string b81 = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base81Unbalanced);
            string dec = RadixConverter.Convert(b81, Radix.Base81Unbalanced, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    // -----------------------------------------------------------------------
    // Binary unsigned round-trip: 0 .. 3^6-1
    // -----------------------------------------------------------------------

    [Fact]
    public void BinaryUnsigned_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string bin = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base2Unsigned);
            string dec = RadixConverter.Convert(bin, Radix.Base2Unsigned, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    // -----------------------------------------------------------------------
    // Binary two's complement round-trip: −364 .. +364
    // -----------------------------------------------------------------------

    [Fact]
    public void BinarySigned2C_ExhaustiveRoundTrip()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string bin = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base2Signed2C);
            string dec = RadixConverter.Convert(bin, Radix.Base2Signed2C, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"two's complement '{bin}' should round-trip to {v}");
        }
    }
}
