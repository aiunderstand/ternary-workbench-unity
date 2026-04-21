using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>Tests for <see cref="CharTu8StandardTable"/>.</summary>
public class CharTu8StandardTableTests
{
    [Fact]
    public void SingleTryteTable_Has126Entries()
        => CharTu8StandardTable.SingleTryteTable.Count.Should().Be(126);

    [Fact]
    public void SingleTryteTable_NoDuplicatePatterns()
    {
        var patterns = CharTu8StandardTable.SingleTryteTable.Select(e => e.TrytePattern).ToList();
        patterns.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SingleTryteTable_NoDuplicateCodePoints()
    {
        var cps = CharTu8StandardTable.SingleTryteTable.Select(e => e.CodePoint).ToList();
        cps.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SingleTryteTable_CodePointsAre0To125()
    {
        var cps = CharTu8StandardTable.SingleTryteTable.Select(e => e.CodePoint).OrderBy(x => x).ToList();
        cps.Should().BeEquivalentTo(Enumerable.Range(0, 126));
    }

    [Fact]
    public void SingleTryteTable_AllPatternsAreSixChars()
    {
        foreach (var entry in CharTu8StandardTable.SingleTryteTable)
            entry.TrytePattern.Should().HaveLength(6,
                because: $"CP {entry.CodePoint} pattern '{entry.TrytePattern}' should be 6 trits");
    }

    [Fact]
    public void SingleTryteTable_AllPatternsStartWithPlus()
    {
        foreach (var entry in CharTu8StandardTable.SingleTryteTable)
            entry.TrytePattern[0].Should().Be('+',
                because: $"CP {entry.CodePoint} must start with '+'");
    }

    [Fact]
    public void SingleTryteTable_NoPatternsAreLeadPrefixes()
    {
        foreach (var entry in CharTu8StandardTable.SingleTryteTable)
        {
            var tryte = Tryte.Parse(entry.TrytePattern);
            tryte.GetRole().Should().Be(TryteRole.SingleTryte,
                because: $"CP {entry.CodePoint} pattern '{entry.TrytePattern}' should have SingleTryte role");
        }
    }

    [Fact]
    public void SingleTryteTable_AllAscii0To125HaveCorrectUnicodeCodePoint()
    {
        foreach (var entry in CharTu8StandardTable.SingleTryteTable)
        {
            entry.UnicodeCodePoint.Should().Be(entry.CodePoint,
                because: "charT_u8 is a 1:1 Unicode mapping");
        }
    }

    [Fact]
    public void SingleTryteTable_Tilde126AndDel127_NotPresent()
    {
        var cps = CharTu8StandardTable.SingleTryteTable.Select(e => e.UnicodeCodePoint).ToHashSet();
        cps.Should().NotContain(126, "~ (U+007E) must use 2-tryte encoding");
        cps.Should().NotContain(127, "DEL (U+007F) must use 2-tryte encoding");
    }
}

/// <summary>Tests for <see cref="CharTCu8StandardTable"/>.</summary>
public class CharTCu8StandardTableTests
{
    [Fact]
    public void SingleTryteTable_Has42Entries()
        => CharTCu8StandardTable.SingleTryteTable.Count.Should().Be(42);

    [Fact]
    public void SingleTryteTable_NoDuplicatePatterns()
    {
        var patterns = CharTCu8StandardTable.SingleTryteTable.Select(e => e.TrytePattern).ToList();
        patterns.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SingleTryteTable_AllPatternsAreSixChars()
    {
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
            entry.TrytePattern.Should().HaveLength(6);
    }

    [Fact]
    public void SingleTryteTable_AllPatternsStartWithPlus()
    {
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
            entry.TrytePattern[0].Should().Be('+');
    }

    [Fact]
    public void SingleTryteTable_AllPatternsHaveSingleTryteRole()
    {
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
        {
            var tryte = Tryte.Parse(entry.TrytePattern);
            tryte.GetRole().Should().Be(TryteRole.SingleTryte,
                because: $"charTC CP {entry.CodePoint} pattern '{entry.TrytePattern}' must be SingleTryte role");
        }
    }

    [Fact]
    public void SingleTryteTable_EachTryteHasCrcZeroSum()
    {
        // For a 1-tryte charTC_u8 symbol, the balanced sum of ALL 6 trits ≡ 0 (mod 3)
        foreach (var entry in CharTCu8StandardTable.SingleTryteTable)
        {
            var tryte = Tryte.Parse(entry.TrytePattern);
            int sum = tryte.BalancedSum;
            (((sum % 3) + 3) % 3).Should().Be(0,
                because: $"charTC CP {entry.CodePoint} '{entry.TrytePattern}' must have balanced sum ≡ 0 (mod 3), got {sum}");
        }
    }

    [Fact]
    public void SingleTryteTable_ContainsExpectedAsciiChars()
    {
        var unicodeCodePoints = CharTCu8StandardTable.SingleTryteTable
            .Select(e => e.UnicodeCodePoint)
            .ToHashSet();

        // Must include the 4 controls, space, a-z, 0-9, '.'
        unicodeCodePoints.Should().Contain(0,  "NUL");
        unicodeCodePoints.Should().Contain(9,  "TAB");
        unicodeCodePoints.Should().Contain(10, "LF");
        unicodeCodePoints.Should().Contain(13, "CR");
        unicodeCodePoints.Should().Contain(32, "SPC");
        foreach (char c in "abcdefghijklmnopqrstuvwxyz")
            unicodeCodePoints.Should().Contain(c, $"'{c}'");
        foreach (char c in "0123456789")
            unicodeCodePoints.Should().Contain(c, $"'{c}'");
        unicodeCodePoints.Should().Contain('.', "'.'");
    }

    [Fact]
    public void ComputeCrcBalanced_ProducesZeroModSum()
    {
        // Test all partial sums in a realistic range
        for (int s = -20; s <= 20; s++)
        {
            int crc = CharTCu8StandardTable.ComputeCrcBalanced(s);
            crc.Should().BeInRange(-1, 1, $"CRC must be a valid balanced trit for partialSum={s}");
            int total = s + crc;
            (((total % 3) + 3) % 3).Should().Be(0,
                because: $"partialSum={s} + crc={crc} = {total} must be ≡ 0 (mod 3)");
        }
    }

}
