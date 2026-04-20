using System.Numerics;
using FluentAssertions;
using TernaryWorkbench.Core;
using Xunit;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Exhaustive cross-radix round-trip tests: for every ordered pair of distinct radices,
/// convert all values in the 6-trit range (−364..364 for signed, 0..728 for unsigned)
/// through the pair and back to decimal, verifying the value is preserved.
///
/// Unsigned radices (cannot represent negatives):
///   Base2Unsigned, Base8Unsigned, Base16Unsigned, Base64Rfc4648,
///   Base3Unbalanced, Base9Unbalanced, Base27Unbalanced.
/// Signed radices (represent any integer):
///   Base2Signed1C, Base2Signed2C, Base3Signed2C, Base3Signed3C, Base3Balanced, Base10.
/// </summary>
public class CrossRadixTests
{
    private static readonly int Pow3_6 = (int)Math.Pow(3, 6);   // 729
    private static readonly int BalancedMax = (Pow3_6 - 1) / 2; // 364

    private static bool IsUnsignedRadix(Radix r) => r switch
    {
        Radix.Base2Unsigned or Radix.Base8Unsigned or Radix.Base16Unsigned or
        Radix.Base64Rfc4648 or Radix.Base3Unbalanced or Radix.Base9Unbalanced or
        Radix.Base27Unbalanced => true,
        _ => false
    };

    public static TheoryData<Radix, Radix> AllDirectedPairs()
    {
        var data = new TheoryData<Radix, Radix>();
        foreach (var r1 in Enum.GetValues<Radix>())
            foreach (var r2 in Enum.GetValues<Radix>())
                if (r1 != r2)
                    data.Add(r1, r2);
        return data;
    }

    /// <summary>
    /// For the (from, to) radix pair, convert every value in the 6-trit range:
    ///   decimal → from → to → decimal
    /// and verify the final decimal equals the original.
    /// Values outside either radix's range (e.g. negative for unsigned) are skipped.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllDirectedPairs))]
    public void CrossRadix_6Trit_Exhaustive(Radix from, Radix to)
    {
        bool fromUnsigned = IsUnsignedRadix(from);
        bool toUnsigned   = IsUnsignedRadix(to);

        int minV = fromUnsigned ? 0 : -BalancedMax;

        for (int v = minV; v <= Pow3_6 - 1; v++)
        {
            // Skip negative values that the destination radix cannot encode
            if (toUnsigned && v < 0)
                continue;

            string fromStr = RadixConverter.Convert(v.ToString(), Radix.Base10, from);
            string toStr   = RadixConverter.Convert(fromStr, from, to);
            string backDec = RadixConverter.Convert(toStr, to, Radix.Base10);

            backDec.Should().Be(v.ToString(),
                because: $"{from} → {to} round-trip failed for value {v} " +
                         $"(encoded as '{fromStr}' in {from}, then '{toStr}' in {to})");
        }
    }
}
