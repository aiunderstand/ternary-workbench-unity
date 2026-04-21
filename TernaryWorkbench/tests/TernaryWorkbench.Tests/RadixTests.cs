using FluentAssertions;
using TernaryWorkbench.Core;

namespace TernaryWorkbench.Tests;

/// <summary>Tests for <see cref="Radix"/> extension methods.</summary>
public class RadixTests
{
    // -------------------------------------------------------------------------
    // FixedLengthOptions — ternary radices
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(Radix.Base3Unbalanced)]
    [InlineData(Radix.Base3Signed2C)]
    [InlineData(Radix.Base3Signed3C)]
    [InlineData(Radix.Base3Balanced)]
    [InlineData(Radix.Base3BsdPnx)]
    public void TernaryFixedLengthOptions_Contains32(Radix radix)
    {
        radix.FixedLengthOptions().Should().Contain(32);
    }

    [Theory]
    [InlineData(Radix.Base3Unbalanced)]
    [InlineData(Radix.Base3Signed2C)]
    [InlineData(Radix.Base3Signed3C)]
    [InlineData(Radix.Base3Balanced)]
    [InlineData(Radix.Base3BsdPnx)]
    public void TernaryFixedLengthOptions_IsSortedAscending(Radix radix)
    {
        int[] options = radix.FixedLengthOptions();
        options.Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(Radix.Base3Unbalanced)]
    [InlineData(Radix.Base3Signed2C)]
    [InlineData(Radix.Base3Signed3C)]
    [InlineData(Radix.Base3Balanced)]
    [InlineData(Radix.Base3BsdPnx)]
    public void TernaryFixedLengthOptions_ContainsExpectedValues(Radix radix)
    {
        int[] options = radix.FixedLengthOptions();
        options.Should().BeEquivalentTo(new[] { 3, 6, 9, 12, 24, 27, 32, 48, 81 });
    }

    // -------------------------------------------------------------------------
    // FixedLengthOptions — binary radices still include 32 (unchanged)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(Radix.Base2Unsigned)]
    [InlineData(Radix.Base2Signed1C)]
    [InlineData(Radix.Base2Signed2C)]
    public void BinaryFixedLengthOptions_StillContains32(Radix radix)
    {
        radix.FixedLengthOptions().Should().Contain(32);
    }

    // -------------------------------------------------------------------------
    // FixedLengthOptions — non-fixed radices return empty array
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(Radix.Base10)]
    [InlineData(Radix.Base8Unsigned)]
    [InlineData(Radix.Base16Unsigned)]
    [InlineData(Radix.Base64Rfc4648)]
    [InlineData(Radix.Base9Unbalanced)]
    [InlineData(Radix.Base27Unbalanced)]
    public void NonFixedRadices_FixedLengthOptions_ReturnsEmpty(Radix radix)
    {
        radix.FixedLengthOptions().Should().BeEmpty();
    }
}
