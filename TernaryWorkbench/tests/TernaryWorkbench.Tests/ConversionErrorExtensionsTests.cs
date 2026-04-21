using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="ConversionErrorExtensions.CollapseRepeated"/>.
/// </summary>
public class ConversionErrorExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ConversionError Err(string message, int symbolIndex = 0)
        => new(symbolIndex, symbolIndex, "raw", message);

    // -------------------------------------------------------------------------
    // Empty input
    // -------------------------------------------------------------------------

    [Fact]
    public void CollapseRepeated_EmptyList_ReturnsEmpty()
    {
        var result = Array.Empty<ConversionError>().CollapseRepeated();
        result.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // All-unique messages
    // -------------------------------------------------------------------------

    [Fact]
    public void CollapseRepeated_AllUnique_CountIsOneForEach()
    {
        var errors = new[]
        {
            Err("Error A"),
            Err("Error B"),
            Err("Error C"),
        };

        var result = errors.CollapseRepeated();

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(item => item.Count.Should().Be(1));
        result.Select(r => r.Message).Should().Equal("Error A", "Error B", "Error C");
    }

    // -------------------------------------------------------------------------
    // Duplicate messages — grouped by message, count accumulated
    // -------------------------------------------------------------------------

    [Fact]
    public void CollapseRepeated_AllSameMessage_CollapsesToSingleEntry()
    {
        var errors = Enumerable.Range(0, 5)
            .Select(i => Err("CRC error: bad sum", i))
            .ToList();

        var result = errors.CollapseRepeated();

        result.Should().HaveCount(1);
        result[0].Message.Should().Be("CRC error: bad sum");
        result[0].Count.Should().Be(5);
    }

    [Fact]
    public void CollapseRepeated_TwoDistinctMessages_EachCollapsed()
    {
        var errors = new[]
        {
            Err("Unknown single-tryte pattern '+++0++'", 0),
            Err("CRC error: balanced sum is 5", 0),
            Err("Unknown single-tryte pattern '+++0++'", 1),
            Err("CRC error: balanced sum is 5", 1),
        };

        var result = errors.CollapseRepeated();

        // Two distinct messages
        result.Should().HaveCount(2);

        // First message (first seen) should be the unknown-pattern one
        result[0].Message.Should().Contain("Unknown single-tryte pattern");
        result[0].Count.Should().Be(2);

        result[1].Message.Should().Contain("CRC error");
        result[1].Count.Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // Ordering: first-seen order is preserved
    // -------------------------------------------------------------------------

    [Fact]
    public void CollapseRepeated_PreservesFirstSeenOrder()
    {
        var errors = new[]
        {
            Err("B"),
            Err("A"),
            Err("B"),
            Err("C"),
            Err("A"),
        };

        var result = errors.CollapseRepeated();

        // B was first seen, then A, then C
        result.Select(r => r.Message).Should().Equal("B", "A", "C");
        result.Select(r => r.Count).Should().Equal(2, 2, 1);
    }

    // -------------------------------------------------------------------------
    // Real-world example: the +++0++ +++0++ case (two CRC + two unknown errors)
    // -------------------------------------------------------------------------

    [Fact]
    public void CollapseRepeated_RealWorldRepeatedErrors_CollapsesCorrectly()
    {
        // Simulates charTC_u8 decoding errors for "+++0++ +++0++"
        var errors = new[]
        {
            Err("CRC error: balanced sum of sequence is 5 (\u2261 2 mod 3, expected 0)."),
            Err("Unknown single-tryte pattern '+++0++'."),
            Err("CRC error: balanced sum of sequence is 5 (\u2261 2 mod 3, expected 0)."),
            Err("Unknown single-tryte pattern '+++0++'."),
        };

        var result = errors.CollapseRepeated();

        result.Should().HaveCount(2);
        result[0].Count.Should().Be(2, "both CRC errors should be collapsed");
        result[1].Count.Should().Be(2, "both unknown-pattern errors should be collapsed");
    }
}
