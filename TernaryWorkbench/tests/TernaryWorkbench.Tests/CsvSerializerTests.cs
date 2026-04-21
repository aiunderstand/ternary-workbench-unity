using FluentAssertions;
using TernaryWorkbench.Core;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="ConversionCsvSerializer.Serialize"/> and
/// <see cref="ConversionCsvSerializer.Deserialize"/>.
/// </summary>
public class CsvSerializerTests
{
    // -------------------------------------------------------------------------
    // Serialize
    // -------------------------------------------------------------------------

    [Fact]
    public void Serialize_EmptyList_ReturnsOnlyHeaderLine()
    {
        string csv = ConversionCsvSerializer.Serialize([]);

        // Should have exactly one non-empty line: the header.
        var nonEmpty = csv.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        nonEmpty.Should().HaveCount(1);
        nonEmpty[0].Should().Be(ConversionCsvSerializer.Header);
    }

    [Fact]
    public void Serialize_SingleRecord_MSD_NoWordLength_ProducesCorrectRow()
    {
        var record = new ConversionRecord("42", "1120", Radix.Base10, Radix.Base3Unbalanced,
            LsdFirst: false, FixedOutputLength: null);

        string csv = ConversionCsvSerializer.Serialize([record]);
        var lines = csv.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        lines.Should().HaveCount(2); // header + 1 data row
        lines[1].Should().Be("42;1120;Base10;Base3Unbalanced;MSD;");
    }

    [Fact]
    public void Serialize_SingleRecord_LSD_WithWordLength_ProducesCorrectRow()
    {
        var record = new ConversionRecord("5", "10100", Radix.Base10, Radix.Base2Unsigned,
            LsdFirst: true, FixedOutputLength: 8);

        string csv = ConversionCsvSerializer.Serialize([record]);
        var lines = csv.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        lines[1].Should().Be("5;10100;Base10;Base2Unsigned;LSD;8");
    }

    [Fact]
    public void Serialize_MultipleRecords_CorrectDataRowCount()
    {
        var records = new[]
        {
            new ConversionRecord("1", "+", Radix.Base10, Radix.Base3Balanced, false, null),
            new ConversionRecord("2", "+-", Radix.Base10, Radix.Base3Balanced, false, null),
            new ConversionRecord("3", "+0", Radix.Base10, Radix.Base3Balanced, false, null),
        };

        string csv = ConversionCsvSerializer.Serialize(records);
        var lines = csv.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        lines.Should().HaveCount(4); // header + 3 rows
    }

    [Fact]
    public void Serialize_OutputColumn_UsesRecordOutputValue()
    {
        var record = new ConversionRecord("FF", "255", Radix.Base16Unsigned, Radix.Base10,
            false, null);

        string csv = ConversionCsvSerializer.Serialize([record]);
        var dataRow = csv.Replace("\r\n", "\n").Split('\n')
            .First(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("input"));

        dataRow.Split(';')[1].Should().Be("255");
    }

    [Fact]
    public void Serialize_BsdPnxRadix_SerializesAsEnumName()
    {
        var record = new ConversionRecord("10", "1011", Radix.Base10, Radix.Base3BsdPnx,
            false, null);

        string csv = ConversionCsvSerializer.Serialize([record]);
        csv.Should().Contain("Base3BsdPnx");
    }

    // -------------------------------------------------------------------------
    // Deserialize — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public void Deserialize_ValidSingleRow_ParsesCorrectly()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "42;1120;Base10;Base3Unbalanced;MSD;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);

        CsvImportRow row = result.ValidRows[0];
        row.Input.Should().Be("42");
        row.FromRadix.Should().Be(Radix.Base10);
        row.ToRadix.Should().Be(Radix.Base3Unbalanced);
        row.LsdFirst.Should().BeFalse();
        row.FixedOutputLength.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ValidSingleRow_LSD_WithWordLength()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "5;00000101;Base10;Base2Unsigned;LSD;8\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        CsvImportRow row = result.ValidRows[0];
        row.LsdFirst.Should().BeTrue();
        row.FixedOutputLength.Should().Be(8);
    }

    [Fact]
    public void Deserialize_ValidMultipleRows_AllParsed()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;Base3Balanced;MSD;\n"
                   + "2;+-;Base10;Base3Balanced;MSD;\n"
                   + "3;+0;Base10;Base3Balanced;MSD;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(3);
    }

    [Fact]
    public void Deserialize_WindowsLineEndings_Handled()
    {
        // CRLF line endings must be treated the same as LF.
        string csv = ConversionCsvSerializer.Header + "\r\n"
                   + "10;101;Base10;Base2Unsigned;MSD;\r\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_BlankLines_AreSkipped()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "\n"
                   + "7;21;Base10;Base3Unbalanced;MSD;\n"
                   + "\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_EmptyOutputWordLength_ParsesAsNull()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "0;0;Base10;Base10;MSD;\n";   // trailing empty field

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows[0].FixedOutputLength.Should().BeNull();
    }

    [Fact]
    public void Deserialize_BsdPnxRadix_Roundtrip()
    {
        var original = new ConversionRecord("3", "1011", Radix.Base10, Radix.Base3BsdPnx,
            false, null);

        string csv = ConversionCsvSerializer.Serialize([original]);
        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        CsvImportRow row = result.ValidRows[0];
        row.FromRadix.Should().Be(Radix.Base10);
        row.ToRadix.Should().Be(Radix.Base3BsdPnx);
    }

    // -------------------------------------------------------------------------
    // Deserialize — error cases
    // -------------------------------------------------------------------------

    [Fact]
    public void Deserialize_InvalidHeader_ReturnsFileError_NoRows()
    {
        string csv = "wrong header line\n"
                   + "1;+;Base10;Base3Balanced;MSD;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().HaveCount(1);
        result.ValidRows.Should().BeEmpty();
        result.Errors[0].Message.Should().Contain("Expected header");
    }

    [Fact]
    public void Deserialize_MissingFields_ReturnsRowError()
    {
        // Only 3 fields instead of 6.
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Contain("6");
    }

    [Fact]
    public void Deserialize_InvalidFromRadixName_ReturnsRowError()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;UnknownRadix;Base3Balanced;MSD;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors[0].Message.Should().Contain("UnknownRadix");
    }

    [Fact]
    public void Deserialize_InvalidToRadixName_ReturnsRowError()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;NotARadix;MSD;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors[0].Message.Should().Contain("NotARadix");
    }

    [Fact]
    public void Deserialize_InvalidSignificantDigitField_ReturnsRowError()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;Base3Balanced;BOTH;\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors[0].Message.Should().Contain("BOTH");
    }

    [Fact]
    public void Deserialize_NonIntegerOutputWordLength_ReturnsRowError()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;Base3Balanced;MSD;eight\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors[0].Message.Should().Contain("eight");
    }

    [Fact]
    public void Deserialize_ZeroOutputWordLength_ReturnsRowError()
    {
        // Zero is not a valid positive word length.
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;Base3Balanced;MSD;0\n";

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_OneValidOneInvalidRow_OnlyValidRowReturned()
    {
        string csv = ConversionCsvSerializer.Header + "\n"
                   + "1;+;Base10;Base3Balanced;MSD;\n"    // valid
                   + "bad;row;with;wrong;column;count;extra\n"; // 7 fields

        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().HaveCount(1);
        result.Errors.Should().HaveCount(1);
    }

    // -------------------------------------------------------------------------
    // Round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public void Deserialize_RoundTrip_PreservesAllFields()
    {
        var original = new ConversionRecord(
            Input:             "+0-",
            Output:            "8",
            FromRadix:         Radix.Base3Balanced,
            ToRadix:           Radix.Base10,
            LsdFirst:          true,
            FixedOutputLength: 9);

        string csv = ConversionCsvSerializer.Serialize([original]);
        CsvParseResult result = ConversionCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        CsvImportRow row = result.ValidRows[0];
        row.Input.Should().Be(original.Input);
        row.FromRadix.Should().Be(original.FromRadix);
        row.ToRadix.Should().Be(original.ToRadix);
        row.LsdFirst.Should().Be(original.LsdFirst);
        row.FixedOutputLength.Should().Be(original.FixedOutputLength);
    }
}
