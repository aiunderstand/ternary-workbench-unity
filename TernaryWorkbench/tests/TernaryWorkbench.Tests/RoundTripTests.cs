using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Exhaustive per-radix round-trips for all values within the 6-trit range.
/// 3^6 = 729 unsigned values (0..728); balanced/signed range: −364..364.
/// Each radix is tested independently via decimal as ground truth.
/// </summary>
public class RoundTripTests
{
    private const int MaxTrits = 6;
    private static readonly int Pow3_6 = (int)Math.Pow(3, MaxTrits);   // 729
    private static readonly int BalancedMax = (Pow3_6 - 1) / 2;        // 364

    // -----------------------------------------------------------------------
    // Unsigned radices: 0 .. 728
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

    [Fact]
    public void Octal_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string oct = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base8Unsigned);
            string dec = RadixConverter.Convert(oct, Radix.Base8Unsigned, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    [Fact]
    public void Hex_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string hex = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base16Unsigned);
            string dec = RadixConverter.Convert(hex, Radix.Base16Unsigned, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

    [Fact]
    public void Base64_ExhaustiveRoundTrip()
    {
        for (int v = 0; v < Pow3_6; v++)
        {
            string b64 = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base64Rfc4648);
            string dec = RadixConverter.Convert(b64, Radix.Base64Rfc4648, Radix.Base10);
            dec.Should().Be(v.ToString());
        }
    }

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
    // Signed radices: −364 .. +364
    // -----------------------------------------------------------------------

    [Fact]
    public void Binary1C_ExhaustiveRoundTrip()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string bin = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base2Signed1C);
            string dec = RadixConverter.Convert(bin, Radix.Base2Signed1C, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"1's complement '{bin}' should round-trip to {v}");
        }
    }

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

    [Fact]
    public void Ternary2C_ExhaustiveRoundTrip()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string enc = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base3Signed2C);
            string dec = RadixConverter.Convert(enc, Radix.Base3Signed2C, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"ternary 2's complement '{enc}' should round-trip to {v}");
        }
    }

    [Fact]
    public void Ternary3C_ExhaustiveRoundTrip()
    {
        for (int v = -BalancedMax; v <= BalancedMax; v++)
        {
            string enc = RadixConverter.Convert(v.ToString(), Radix.Base10, Radix.Base3Signed3C);
            string dec = RadixConverter.Convert(enc, Radix.Base3Signed3C, Radix.Base10);
            dec.Should().Be(v.ToString(), because: $"ternary 3's complement '{enc}' should round-trip to {v}");
        }
    }
}

