using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Boundary tests at exact powers of 2 (n=1..128) and powers of 3 (m=1..81).
/// These values span up to 3^81 ≈ 4.4×10^38 and 2^128 ≈ 3.4×10^38 — BigInteger required.
/// </summary>
public class BoundaryTests
{
    // =========================================================================
    // Powers of 3
    // =========================================================================

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_BalancedTernary_PositiveBoundary(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string expected = "+" + new string('0', n);
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

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_BalancedTernary_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string bal = RadixConverter.Format(value, Radix.Base3Balanced);
        BigInteger parsed = RadixConverter.Parse(bal, Radix.Base3Balanced);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through balanced ternary");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_TernaryUnbalanced_Boundary(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string expected = "1" + new string('0', n);
        string result = RadixConverter.Format(value, Radix.Base3Unbalanced);
        result.Should().Be(expected, because: $"3^{n} in ternary is '1' followed by {n} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Base9_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string result = RadixConverter.Format(value, Radix.Base9Unbalanced);
        BigInteger parsed = RadixConverter.Parse(result, Radix.Base9Unbalanced);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through base-9");
    }

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
    public void Power3_Ternary3C_Positive_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string enc = RadixConverter.Format(value, Radix.Base3Signed3C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base3Signed3C);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through ternary 3's complement");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Ternary3C_Negative_RoundTrip(int n)
    {
        BigInteger value = -BigInteger.Pow(3, n);
        string enc = RadixConverter.Format(value, Radix.Base3Signed3C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base3Signed3C);
        parsed.Should().Be(value, because: $"-3^{n} should round-trip through ternary 3's complement");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Ternary3C_Negative_StartsWithOne(int n)
    {
        // Our format always produces "10...0" for -3^n
        BigInteger value = -BigInteger.Pow(3, n);
        string enc = RadixConverter.Format(value, Radix.Base3Signed3C);
        enc.Should().Be("1" + new string('0', n),
            because: $"-3^{n} in ternary 3's complement is '1' followed by {n} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Ternary2C_Positive_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(3, n);
        string enc = RadixConverter.Format(value, Radix.Base3Signed2C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base3Signed2C);
        parsed.Should().Be(value, because: $"3^{n} should round-trip through ternary 2's complement");
    }

    [Theory]
    [MemberData(nameof(PowersOf3_1_to_81))]
    public void Power3_Ternary2C_Negative_RoundTrip(int n)
    {
        BigInteger value = -BigInteger.Pow(3, n);
        string enc = RadixConverter.Format(value, Radix.Base3Signed2C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base3Signed2C);
        parsed.Should().Be(value, because: $"-3^{n} should round-trip through ternary 2's complement");
    }

    // =========================================================================
    // Powers of 2
    // =========================================================================

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_BinaryUnsigned_IsLeadingOne(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string result = RadixConverter.Format(value, Radix.Base2Unsigned);
        result.Should().Be("1" + new string('0', n),
            because: $"2^{n} in binary unsigned is '1' followed by {n} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_BinarySigned2C_Positive_Form(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string result = RadixConverter.Format(value, Radix.Base2Signed2C);
        result.Should().Be("01" + new string('0', n),
            because: $"2^{n} in two's complement is sign bit '0', '1', then {n} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_Binary1C_Positive_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string enc = RadixConverter.Format(value, Radix.Base2Signed1C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base2Signed1C);
        parsed.Should().Be(value, because: $"2^{n} should round-trip through binary 1's complement");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_Binary1C_Negative_RoundTrip(int n)
    {
        BigInteger value = -BigInteger.Pow(2, n);
        string enc = RadixConverter.Format(value, Radix.Base2Signed1C);
        BigInteger parsed = RadixConverter.Parse(enc, Radix.Base2Signed1C);
        parsed.Should().Be(value, because: $"-2^{n} should round-trip through binary 1's complement");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_Hex_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string hex = RadixConverter.Format(value, Radix.Base16Unsigned);
        BigInteger parsed = RadixConverter.Parse(hex, Radix.Base16Unsigned);
        parsed.Should().Be(value, because: $"2^{n} should round-trip through hexadecimal");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_4_to_128_step4))]
    public void Power2_Hex_MultiplesOf4_IsLeadingOne(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string hex = RadixConverter.Format(value, Radix.Base16Unsigned);
        int k = n / 4;
        hex.Should().Be("1" + new string('0', k),
            because: $"2^{n} = 16^{k} in hex is '1' followed by {k} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_Octal_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string oct = RadixConverter.Format(value, Radix.Base8Unsigned);
        BigInteger parsed = RadixConverter.Parse(oct, Radix.Base8Unsigned);
        parsed.Should().Be(value, because: $"2^{n} should round-trip through octal");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_3_to_126_step3))]
    public void Power2_Octal_MultiplesOf3_IsLeadingOne(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string oct = RadixConverter.Format(value, Radix.Base8Unsigned);
        int k = n / 3;
        oct.Should().Be("1" + new string('0', k),
            because: $"2^{n} = 8^{k} in octal is '1' followed by {k} zeros");
    }

    [Theory]
    [MemberData(nameof(PowersOf2_1_to_128))]
    public void Power2_Base64_RoundTrip(int n)
    {
        BigInteger value = BigInteger.Pow(2, n);
        string b64 = RadixConverter.Format(value, Radix.Base64Rfc4648);
        BigInteger parsed = RadixConverter.Parse(b64, Radix.Base64Rfc4648);
        parsed.Should().Be(value, because: $"2^{n} should round-trip through base-64");
    }

    // =========================================================================
    // Test data
    // =========================================================================

    public static TheoryData<int> PowersOf3_1_to_81()
    {
        var data = new TheoryData<int>();
        for (int n = 1; n <= 81; n++) data.Add(n);
        return data;
    }

    public static TheoryData<int> PowersOf2_1_to_128()
    {
        var data = new TheoryData<int>();
        for (int n = 1; n <= 128; n++) data.Add(n);
        return data;
    }

    public static TheoryData<int> PowersOf2_4_to_128_step4()
    {
        var data = new TheoryData<int>();
        for (int n = 4; n <= 128; n += 4) data.Add(n);
        return data;
    }

    public static TheoryData<int> PowersOf2_3_to_126_step3()
    {
        var data = new TheoryData<int>();
        for (int n = 3; n <= 126; n += 3) data.Add(n);
        return data;
    }
}
