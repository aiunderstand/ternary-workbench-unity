using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Boundary tests: ±3^n for n=1..81 converted to/from balanced ternary and decimal.
/// These values span up to 3^81 ≈ 4.4×10^38 — BigInteger required.
/// </summary>
public class BoundaryTests
{
    // -----------------------------------------------------------------------
    // 3^n in balanced ternary is always "+0…0" (n zeros)
    // -(3^n) is always "-0…0"
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_BalancedTernary_PositiveBoundary(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string expected = "+" + new string('0', n);   // e.g. n=3 → "+000"
        string result = RadixConverter.Format(value, Radix.Base3Balanced);
        result.Should().Be(expected, because: $"3^{n} in balanced ternary is '+' followed by {n} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_BalancedTernary_NegativeBoundary(int n)
    {
        BigInteger value = -BigInteger.Pow(3, n);
        string expected = "-" + new string('0', n);
        string result = RadixConverter.Format(value, Radix.Base3Balanced);
        result.Should().Be(expected, because: $"-3^{n} in balanced ternary is '-' followed by {n} zeros");
    }

    // -----------------------------------------------------------------------
    // Round-trip: balanced ternary boundary → decimal → balanced ternary
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_BalancedTernary_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string bal = RadixConverter.Format(value, Radix.Base3Balanced);
        BigInteger parsed = RadixConverter.Parse(bal, Radix.Base3Balanced);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through balanced ternary");
    }

    // -----------------------------------------------------------------------
    // 3^n in unbalanced ternary is always "10…0" (n zeros)
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_TernaryUnbalanced_Boundary(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string expected = "1" + new string('0', n);
        string result = RadixConverter.Format(value, Radix.Base3Unbalanced);
        result.Should().Be(expected, because: $"3^{n} in ternary is '1' followed by {n} zeros");
    }

    // -----------------------------------------------------------------------
    // 3^n in base-9: since 9 = 3^2, 3^n in base-9 depends on parity of n
    // 3^(2k) = 9^k → "10…0" with k zeros
    // 3^(2k+1) = 3·9^k → leading digit 3
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Base9_Boundary(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string result = RadixConverter.Format(value, Radix.Base9Unbalanced);
        // Parse back and compare numerically
        BigInteger parsed = RadixConverter.Parse(result, Radix.Base9Unbalanced);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through base-9");
    }

    // -----------------------------------------------------------------------
    // 3^n in base-27 and base-81 round-trip
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Base27_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string b27 = RadixConverter.Format(value, Radix.Base27Unbalanced);
        BigInteger parsed = RadixConverter.Parse(b27, Radix.Base27Unbalanced);
        parsed.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Base81_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string b81 = RadixConverter.Format(value, Radix.Base81Unbalanced);
        BigInteger parsed = RadixConverter.Parse(b81, Radix.Base81Unbalanced);
        parsed.Should().Be(value);
    }

    // -----------------------------------------------------------------------
    // Test data: n = 1..81
    // -----------------------------------------------------------------------

    public static TheoryData<int> PowersOf3_1_to_81()
    {
        var data = new TheoryData<int>();
        for (int n = 1; n <= 81; n++)
            data.Add(n);
        return data;
    }
}
