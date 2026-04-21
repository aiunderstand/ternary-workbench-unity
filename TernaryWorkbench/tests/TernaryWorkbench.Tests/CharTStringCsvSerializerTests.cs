using System.Diagnostics;
using FluentAssertions;
using TernaryWorkbench.CharTStringConverter;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for <see cref="CharTStringCsvSerializer.Serialize"/> and
/// <see cref="CharTStringCsvSerializer.Deserialize"/>.
///
/// Coverage map
/// ============
/// Serialize:
///   • Empty list          → header-only output
///   • UTF-8→Ternary row   → both output columns + both output encoding columns populated
///   • Ternary→UTF-8 row   → both outputs UTF-8, second output enc = "UTF-8"
///   • Single-codec rows   → second output + encoding empty
///   • Field quoting       → fields containing ';' or '"' are double-quoted
///   • Multi-row ordering  → rows appear in the same order they were added
///   • Header constant     → <see cref="CharTStringCsvSerializer.Header"/> literal is correct
///
/// Deserialize:
///   • Valid rows (all four input encodings)
///   • Output columns are ignored on import (re-run semantics)
///   • Windows CRLF line endings normalised
///   • Blank lines silently skipped
///   • Wrong header → single file-level error, no rows parsed
///   • Wrong field count → per-row error, subsequent rows still parsed
///   • Unknown input encoding value → per-row error
///   • Empty input string → no rows, no errors
///   • Quoted fields round-trip through serialize→deserialize
///
/// Regression:
///   • CharTu8Codec.Decode() with non-ternary input (the original crash from the bug report)
///     must return a DecodeResult with a non-empty error list, NOT throw a FormatException.
///   • CharTCu8Codec.Decode() exhibits the same safe behaviour (baseline unchanged).
///
/// Performance / benchmarks (inline, no external framework required):
///   • Serializing 10 000 records completes within 2 s
///   • Deserializing a 10 000-row CSV completes within 2 s
///   • Encode + decode round-trip of a 1 000-char ASCII string completes within 500 ms
/// </summary>
public class CharTStringCsvSerializerTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static List<string> DataLines(string csv)
        => csv.Replace("\r\n", "\n").Split('\n')
              .Where(l => !string.IsNullOrWhiteSpace(l))
              .Skip(1)   // skip header
              .ToList();

    private static CharTStringConversionRecord Utf8Row(
        string input = "hello",
        string u8    = "+0000+ -00+00",
        string uc    = "+0000- -00+0+")
        => new(input, u8, uc, "UTF-8", "charT_u8", "charTC_u8");

    private static CharTStringConversionRecord TernaryRow(
        string input = "+0000+ -00+00",
        string out1  = "hello",
        string out2  = "hello")
        => new(input, out1, out2, "ternary", "UTF-8", "UTF-8");

    private static CharTStringConversionRecord SingleCodecRow(
        string inputEncoding, string input = "+0000+ -00+00", string out1 = "hello")
        => new(input, out1, string.Empty, inputEncoding, "UTF-8", string.Empty);

    // =========================================================================
    // Header constant
    // =========================================================================

    [Fact]
    public void Header_HasExactExpectedValue()
    {
        // Regression guard: if the header literal ever drifts, every existing
        // exported file would fail to import.
        CharTStringCsvSerializer.Header
            .Should().Be("input;output;output;input encoding;output encoding;output encoding");
    }

    // =========================================================================
    // Serialize — structural
    // =========================================================================

    [Fact]
    public void Serialize_EmptyList_ReturnsOnlyHeaderLine()
    {
        // An empty history export must still produce a valid importable file.
        string csv = CharTStringCsvSerializer.Serialize([]);

        var nonEmpty = csv.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        nonEmpty.Should().HaveCount(1);
        nonEmpty[0].Should().Be(CharTStringCsvSerializer.Header);
    }

    [Fact]
    public void Serialize_SingleUtf8Row_ProducesCorrectColumns()
    {
        // Columns: input ; output1 ; output2 ; inputEnc ; out1Enc ; out2Enc
        var record = Utf8Row("hi", "u8encoded", "ucencoded");
        string csv = CharTStringCsvSerializer.Serialize([record]);

        DataLines(csv).Should().HaveCount(1);
        var fields = DataLines(csv)[0].Split(';');
        fields.Should().HaveCount(6);
        fields[0].Should().Be("hi");
        fields[1].Should().Be("u8encoded");
        fields[2].Should().Be("ucencoded");
        fields[3].Should().Be("UTF-8");
        fields[4].Should().Be("charT_u8");
        fields[5].Should().Be("charTC_u8");
    }

    [Fact]
    public void Serialize_TernaryRow_BothOutputEncodingsAreUtf8()
    {
        var record = TernaryRow();
        string csv = CharTStringCsvSerializer.Serialize([record]);

        var fields = DataLines(csv)[0].Split(';');
        fields[3].Should().Be("ternary");
        fields[4].Should().Be("UTF-8");
        fields[5].Should().Be("UTF-8");
    }

    [Theory]
    [InlineData("charT_u8")]   // single-codec charT_u8 input
    [InlineData("charTC_u8")]  // single-codec charTC_u8 input
    public void Serialize_SingleCodecRow_SecondOutputColumnsAreEmpty(string enc)
    {
        // When only one codec ran, output2 and output2Encoding must be empty so
        // re-importing the file does not invent a second decode that never happened.
        var record = SingleCodecRow(enc);
        string csv = CharTStringCsvSerializer.Serialize([record]);

        var fields = DataLines(csv)[0].Split(';');
        fields[2].Should().BeEmpty("output2 must be empty for single-codec rows");
        fields[5].Should().BeEmpty("output2 encoding must be empty for single-codec rows");
    }

    [Fact]
    public void Serialize_MultipleRecords_PreservesInsertionOrder()
    {
        // History rows must appear in chronological order in the export.
        var records = new[]
        {
            Utf8Row("first"),
            TernaryRow("second"),
            Utf8Row("third"),
        };
        string csv = CharTStringCsvSerializer.Serialize(records);
        var lines = DataLines(csv);

        lines.Should().HaveCount(3);
        lines[0].Split(';')[0].Should().Be("first");
        lines[1].Split(';')[0].Should().Be("second");
        lines[2].Split(';')[0].Should().Be("third");
    }

    // =========================================================================
    // Serialize — field quoting
    // =========================================================================

    [Fact]
    public void Serialize_InputContainsSemicolon_FieldIsQuoted()
    {
        // Input "a;b" must be quoted so the CSV parser doesn't split on that semicolon.
        var record = Utf8Row("a;b");
        string csv = CharTStringCsvSerializer.Serialize([record]);

        DataLines(csv)[0].Should().StartWith("\"a;b\"");
    }

    [Fact]
    public void Serialize_InputContainsDoubleQuote_InternalQuoteEscaped()
    {
        var record = Utf8Row("say \"hi\"");
        string csv = CharTStringCsvSerializer.Serialize([record]);

        DataLines(csv)[0].Should().StartWith("\"say \"\"hi\"\"\"");
    }

    [Fact]
    public void Serialize_NormalInput_IsNotQuoted()
    {
        var record = Utf8Row("hello world");
        string csv = CharTStringCsvSerializer.Serialize([record]);

        DataLines(csv)[0].Should().StartWith("hello world;");
    }

    // =========================================================================
    // Deserialize — happy path
    // =========================================================================

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("ternary")]
    [InlineData("charT_u8")]
    [InlineData("charTC_u8")]
    public void Deserialize_AllValidInputEncodings_ParseWithoutError(string encoding)
    {
        // Every legal input encoding value must be accepted by the deserializer.
        string csv = CharTStringCsvSerializer.Header + "\n"
                   + $"some input;;; {encoding}; UTF-8;\n";

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty(because: $"'{encoding}' is a valid input encoding");
        result.ValidRows.Should().HaveCount(1);
        result.ValidRows[0].InputEncoding.Should().Be(encoding);
        result.ValidRows[0].Input.Should().Be("some input");
    }

    [Fact]
    public void Deserialize_OutputColumnsIgnoredOnImport()
    {
        // The importer must ignore existing output columns (re-run semantics).
        // We verify that whatever was in columns 1, 2, 4, 5 has no effect on
        // the parsed row (only Input and InputEncoding are stored).
        string csv = CharTStringCsvSerializer.Header + "\n"
                   + "test data; old output 1; old output 2; ternary; old enc 1; old enc 2\n";

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        var row = result.ValidRows[0];
        row.Input.Should().Be("test data");
        row.InputEncoding.Should().Be("ternary");
        // Verify the record type only exposes Input and InputEncoding (compile-time check)
        _ = (row is CharTStringCsvImportRow(_, _));
    }

    [Fact]
    public void Deserialize_WindowsLineEndings_Handled()
    {
        string csv = CharTStringCsvSerializer.Header + "\r\n"
                   + "hello;;; UTF-8;;\r\n";

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_BlankLines_AreSkipped()
    {
        string csv = "\n" + CharTStringCsvSerializer.Header + "\n"
                   + "\n"
                   + "foo;;; UTF-8;;\n"
                   + "\n";

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_EmptyInput_ReturnsNoRowsAndNoErrors()
    {
        // An empty file (or all-whitespace) should produce zero rows and zero errors,
        // not throw an exception.
        var result = CharTStringCsvSerializer.Deserialize("");

        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    // =========================================================================
    // Deserialize — error handling
    // =========================================================================

    [Fact]
    public void Deserialize_WrongHeader_ReturnsSingleFileErrorAndNoRows()
    {
        // If the header is wrong the column layout is unknown; we must not parse
        // any data rows and must surface exactly one file-level error.
        string csv = "wrong;header;line\nfoo;;; UTF-8;;\n";

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.ValidRows.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Contain("Expected header");
    }

    [Fact]
    public void Deserialize_WrongFieldCount_SkipsRowAndAddsError()
    {
        // A row with the wrong number of fields must be reported as an error
        // WITHOUT stopping the processing of subsequent rows.
        string csv = CharTStringCsvSerializer.Header + "\n"
                   + "too;few\n"                           // bad row
                   + "good;;; UTF-8;;\n";                  // valid row

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Contain("6 semicolon-separated fields");
        result.ValidRows.Should().HaveCount(1, because: "the good row must still be parsed");
    }

    [Fact]
    public void Deserialize_UnknownInputEncoding_SkipsRowAndAddsError()
    {
        // An unrecognised input encoding must be reported without stopping subsequent rows.
        string csv = CharTStringCsvSerializer.Header + "\n"
                   + "foo;;;base16;;\n"                    // unknown encoding
                   + "bar;;; UTF-8;;\n";                   // valid row

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Contain("input encoding");
        result.ValidRows.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_MultipleErrorRows_AllReported()
    {
        string csv = CharTStringCsvSerializer.Header + "\n"
                   + "a;b\n"                               // wrong field count
                   + "c;;;bad_enc;;\n"                     // bad encoding
                   + "ok;;; ternary;;\n";                  // valid

        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().HaveCount(2);
        result.ValidRows.Should().HaveCount(1);
    }

    // =========================================================================
    // Serialize → Deserialize round-trips
    // =========================================================================

    [Fact]
    public void SerializeDeserialize_Utf8Row_InputAndEncodingPreserved()
    {
        // After serialize → deserialize the importer must recover the original
        // input string and its encoding.  Outputs are ignored on import.
        var original = new CharTStringConversionRecord(
            Input:           "hello",
            Output1:         "+0000+",
            Output2:         "+0000-",
            InputEncoding:   "UTF-8",
            Output1Encoding: "charT_u8",
            Output2Encoding: "charTC_u8");

        string csv = CharTStringCsvSerializer.Serialize([original]);
        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(1);
        result.ValidRows[0].Input.Should().Be("hello");
        result.ValidRows[0].InputEncoding.Should().Be("UTF-8");
    }

    [Fact]
    public void SerializeDeserialize_QuotedInput_RoundTrips()
    {
        // A field containing both semicolons and quotes must survive a full cycle.
        var original = new CharTStringConversionRecord(
            Input:           "a;b\"c",
            Output1:         "some output",
            Output2:         string.Empty,
            InputEncoding:   "charT_u8",
            Output1Encoding: "UTF-8",
            Output2Encoding: string.Empty);

        string csv = CharTStringCsvSerializer.Serialize([original]);
        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows[0].Input.Should().Be("a;b\"c");
    }

    [Fact]
    public void SerializeDeserialize_MultiRowHistory_AllRowsPreserved()
    {
        // A realistic four-row export containing all four encoding types.
        var records = new List<CharTStringConversionRecord>
        {
            Utf8Row("hello"),
            TernaryRow("+0000+ -00+00"),
            SingleCodecRow("charT_u8"),
            SingleCodecRow("charTC_u8"),
        };

        string csv = CharTStringCsvSerializer.Serialize(records);
        var result = CharTStringCsvSerializer.Deserialize(csv);

        result.Errors.Should().BeEmpty();
        result.ValidRows.Should().HaveCount(4);
        result.ValidRows.Select(r => r.InputEncoding)
              .Should().BeEquivalentTo(["UTF-8", "ternary", "charT_u8", "charTC_u8"],
                  options => options.WithStrictOrdering());
    }

    // =========================================================================
    // Regression: CharTu8Codec.Decode() must not throw FormatException
    // =========================================================================

    /// <summary>
    /// Regression test for the runtime crash reported when switching direction
    /// from UTF-8 input (e.g. "test") to Ternary→UTF-8 without clearing the input.
    ///
    /// Before the fix, <see cref="CharTu8Codec.Decode"/> let
    /// <c>TokenizeTrytes</c> throw a <see cref="FormatException"/> (because "test"
    /// contains 4 trit characters, not a multiple of 6), which propagated all the
    /// way to the Blazor renderer and caused an unhandled exception.
    ///
    /// After the fix the method must return a <see cref="DecodeResult"/> whose
    /// <c>Errors</c> collection is non-empty instead of throwing.
    /// </summary>
    [Theory]
    [InlineData("test")]          // original crash input from the bug report
    [InlineData("hello world")]   // plain ASCII — also invalid ternary
    [InlineData("abc")]           // 3 chars, not a multiple of 6
    [InlineData("abcde")]         // 5 chars — not valid ternary
    [InlineData("++--0")]         // 5 valid trit chars but not multiple-of-6 tryte boundary
    public void CharTu8Codec_Decode_WithNonTernaryInput_ReturnsErrorResult_NotException(string input)
    {
        // The decoder must NEVER throw; it must absorb format errors into the result.
        Action act = () => CharTu8Codec.Decode(input);
        act.Should().NotThrow<FormatException>(
            because: $"CharTu8Codec.Decode(\"{input}\") must not propagate FormatException to callers");

        var result = CharTu8Codec.Decode(input);
        result.Errors.Should().NotBeEmpty(
            because: $"\"{input}\" is not valid charT_u8 ternary; an error must be reported");
    }

    [Theory]
    [InlineData("test")]
    [InlineData("hello world")]
    [InlineData("++--0")]
    public void CharTCu8Codec_Decode_WithNonTernaryInput_ReturnsErrorResult_NotException(string input)
    {
        // Baseline: CharTCu8Codec already had the fix; this test guards against regression.
        Action act = () => CharTCu8Codec.Decode(input);
        act.Should().NotThrow<FormatException>(
            because: $"CharTCu8Codec.Decode(\"{input}\") must not propagate FormatException");

        var result = CharTCu8Codec.Decode(input);
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void CharTu8Codec_Decode_EmptyInput_ReturnsEmptyWithNoErrors()
    {
        // Whitespace / empty must be treated as "no input", not a format error.
        CharTu8Codec.Decode("").Errors.Should().BeEmpty();
        CharTu8Codec.Decode("   ").Errors.Should().BeEmpty();
        CharTu8Codec.Decode(null).Errors.Should().BeEmpty();
    }

    [Fact]
    public void CharTu8Codec_Decode_ValidTernaryInput_ReturnsNoErrors()
    {
        // Verify that the try-catch does not swallow genuine successes.
        string encoded = CharTu8Codec.Encode("hello").EncodedText;
        var result = CharTu8Codec.Decode(encoded);

        result.Errors.Should().BeEmpty();
        result.DecodedText.Should().Be("hello");
    }

    // =========================================================================
    // Performance benchmarks
    //
    // These tests assert that key operations complete within generous but
    // observable time bounds.  They are intentionally coarse (multi-second
    // budgets) so they only catch catastrophic regressions (e.g. accidentally
    // O(n²) serialization).  For micro-benchmarking use BenchmarkDotNet separately.
    // =========================================================================

    [Fact]
    public void Performance_Serialize10kRecords_CompletesWithin2Seconds()
    {
        // Build a realistic 10 000-row history covering all encoding types.
        var records = Enumerable.Range(0, 10_000)
            .Select(i => (i % 4) switch
            {
                0 => Utf8Row($"input_{i}"),
                1 => TernaryRow($"+0000+ iter_{i}"),
                2 => SingleCodecRow("charT_u8", $"+0000+ iter_{i}"),
                _ => SingleCodecRow("charTC_u8", $"+0000+ iter_{i}"),
            })
            .ToList();

        var sw = Stopwatch.StartNew();
        string csv = CharTStringCsvSerializer.Serialize(records);
        sw.Stop();

        csv.Should().NotBeNullOrEmpty();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            because: "serializing 10 000 records must not be pathologically slow");
    }

    [Fact]
    public void Performance_Deserialize10kRows_CompletesWithin2Seconds()
    {
        // Generate the CSV in advance (not counted in the benchmark).
        var records = Enumerable.Range(0, 10_000)
            .Select(i => Utf8Row($"input_{i}"))
            .ToList();
        string csv = CharTStringCsvSerializer.Serialize(records);

        var sw = Stopwatch.StartNew();
        var result = CharTStringCsvSerializer.Deserialize(csv);
        sw.Stop();

        result.ValidRows.Should().HaveCount(10_000);
        result.Errors.Should().BeEmpty();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            because: "deserializing 10 000 rows must not be pathologically slow");
    }

    [Fact]
    public void Performance_EncodeDecodeRoundTrip_1kCharAsciiString_CompletesWithin500ms()
    {
        // Build a 1 000-character ASCII payload representative of typical text.
        string input = string.Concat(Enumerable.Repeat("Hello, world! ", 72)).Substring(0, 1_000);

        var sw = Stopwatch.StartNew();
        // Run 100 iterations so short strings don't escape timing resolution.
        for (int i = 0; i < 100; i++)
        {
            var encoded = CharTu8Codec.Encode(input);
            var decoded = CharTu8Codec.Decode(encoded.EncodedText);
            _ = decoded.DecodedText; // ensure result is not optimised away
        }
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            because: "100 × encode+decode of a 1 000-char string must complete quickly");
    }

    [Fact]
    public void Performance_CharTCu8EncodeDecodeRoundTrip_1kCharAsciiString_CompletesWithin500ms()
    {
        string input = string.Concat(Enumerable.Repeat("hello world 123. ", 59)).Substring(0, 1_000);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var encoded = CharTCu8Codec.Encode(input);
            var decoded = CharTCu8Codec.Decode(encoded.EncodedText);
            _ = decoded.DecodedText;
        }
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }
}
