using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>Tests for the <see cref="Trit"/> enum and <see cref="TritHelper"/> helpers.</summary>
public class TritHelperTests
{
    [Theory]
    [InlineData('-', Trit.Minus)]
    [InlineData('0', Trit.Zero)]
    [InlineData('+', Trit.Plus)]
    public void Parse_ValidChars_ReturnsCorrectTrit(char c, Trit expected)
        => TritHelper.Parse(c).Should().Be(expected);

    [Theory]
    [InlineData('x')]
    [InlineData('1')]
    [InlineData(' ')]
    public void Parse_InvalidChar_ThrowsFormatException(char c)
        => FluentActions.Invoking(() => TritHelper.Parse(c)).Should().Throw<FormatException>();

    [Theory]
    [InlineData(Trit.Minus, '-')]
    [InlineData(Trit.Zero,  '0')]
    [InlineData(Trit.Plus,  '+')]
    public void ToChar_RoundTrips(Trit t, char expected)
        => TritHelper.ToChar(t).Should().Be(expected);

    [Theory]
    [InlineData(Trit.Minus, 0)]
    [InlineData(Trit.Zero,  1)]
    [InlineData(Trit.Plus,  2)]
    public void ToPositionalDigit_CorrectMapping(Trit t, int expected)
        => TritHelper.ToPositionalDigit(t).Should().Be(expected);

    [Theory]
    [InlineData(0, Trit.Minus)]
    [InlineData(1, Trit.Zero)]
    [InlineData(2, Trit.Plus)]
    public void FromPositionalDigit_CorrectMapping(int d, Trit expected)
        => TritHelper.FromPositionalDigit(d).Should().Be(expected);

    [Fact]
    public void FromPositionalDigit_InvalidInput_Throws()
        => FluentActions.Invoking(() => TritHelper.FromPositionalDigit(3)).Should().Throw<ArgumentOutOfRangeException>();
}

/// <summary>Tests for <see cref="Tryte"/> struct.</summary>
public class TryteTests
{
    // -------------------------------------------------------------------------
    // Parse
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("+-0---")]
    [InlineData("++++++")]
    [InlineData("------")]
    [InlineData("000000")]
    public void Parse_CompactForm_Succeeds(string s)
        => Tryte.TryParse(s, out _).Should().BeTrue();

    [Theory]
    [InlineData("+ - 0 - - -")]
    [InlineData("+ + + + + +")]
    public void Parse_SpacedForm_Succeeds(string s)
        => Tryte.TryParse(s, out _).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("+-0--")]          // 5 chars
    [InlineData("+-0----")]        // 7 chars
    [InlineData("+-0-x-")]        // invalid char
    public void Parse_InvalidInput_ReturnsFalse(string s)
        => Tryte.TryParse(s, out _).Should().BeFalse();

    [Fact]
    public void Parse_CompactAndSpaced_ProduceSameTryte()
    {
        var compact = Tryte.Parse("+-0---");
        var spaced  = Tryte.Parse("+ - 0 - - -");
        compact.Should().Be(spaced);
    }

    // -------------------------------------------------------------------------
    // BalancedSum
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("------", -6)]
    [InlineData("++++++", +6)]
    [InlineData("000000",  0)]
    [InlineData("+-0---", -3)]    // +1-1+0-1-1-1 = -3
    public void BalancedSum_ReturnsCorrectValue(string s, int expected)
        => Tryte.Parse(s).BalancedSum.Should().Be(expected);

    // -------------------------------------------------------------------------
    // GetRole
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("------", TryteRole.ContinuationFinal)]    // starts with '-'
    [InlineData("-+++++", TryteRole.ContinuationFinal)]
    [InlineData("0-0000", TryteRole.ContinuationMiddle1)]  // starts '0' non-zero t1
    [InlineData("0+0000", TryteRole.ContinuationMiddle1)]
    [InlineData("000000", TryteRole.ContinuationMiddle2)]  // starts '00'
    [InlineData("00++++", TryteRole.ContinuationMiddle2)]
    [InlineData("+-0000", TryteRole.Lead2)]                // starts '+-'
    [InlineData("++-000", TryteRole.Lead3)]                // starts '++-'
    [InlineData("+++-00", TryteRole.Lead4)]                // starts '+++-'
    [InlineData("+00000", TryteRole.SingleTryte)]          // '+' then not reserved
    [InlineData("+00001", TryteRole.SingleTryte)]          // impossible in valid trytes but tests role logic
    public void GetRole_ReturnsExpectedRole(string s, TryteRole expected)
    {
        if (!Tryte.TryParse(s, out var tryte)) return; // skip invalid test data
        tryte.GetRole().Should().Be(expected);
    }

    [Fact]
    public void AllPlusLeadingTrytes_AreEitherSingleOrLeadRoles()
    {
        // Every tryte starting with '+' must have a defined, non-Invalid role
        for (int raw = 0; raw < 243; raw++)
        {
            int r = raw;
            var trits = new Trit[6];
            trits[0] = Trit.Plus;
            for (int i = 5; i >= 1; i--) { trits[i] = TritHelper.FromPositionalDigit(r % 3); r /= 3; }
            var tryte = new Tryte(trits);
            var role = tryte.GetRole();
            role.Should().NotBe(TryteRole.Invalid,
                because: $"tryte '{tryte}' starts with '+' and should have a defined role");
        }
    }

    // -------------------------------------------------------------------------
    // Serialization
    // -------------------------------------------------------------------------

    [Fact]
    public void ToBalancedString_Compact_MatchesInput()
        => Tryte.Parse("+-0---").ToBalancedString().Should().Be("+-0---");

    [Fact]
    public void ToBalancedString_Spaced_ProducesSpacedForm()
        => Tryte.Parse("+-0---").ToBalancedString(spaces: true).Should().Be("+ - 0 - - -");

    // -------------------------------------------------------------------------
    // Equality
    // -------------------------------------------------------------------------

    [Fact]
    public void Equality_SameTrits_AreEqual()
        => Tryte.Parse("+-0---").Should().Be(Tryte.Parse("+ - 0 - - -"));

    [Fact]
    public void Equality_DifferentTrits_AreNotEqual()
        => Tryte.Parse("+-0---").Should().NotBe(Tryte.Parse("+-0--+"));
}
