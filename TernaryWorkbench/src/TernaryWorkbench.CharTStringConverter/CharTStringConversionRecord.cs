namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Represents a completed charT-string conversion, capturing both outputs and
/// their encodings.  When the input is UTF-8, <see cref="Output1"/> holds the
/// charT_u8 result and <see cref="Output2"/> holds the charTC_u8 result.
/// When the input is ternary, both outputs hold the decoded UTF-8 text (one per codec).
/// When only one codec was used, <see cref="Output2"/> and
/// <see cref="Output2Encoding"/> are empty strings.
/// </summary>
/// <param name="Input">The original input as entered by the user.</param>
/// <param name="Output1">The primary output string.</param>
/// <param name="Output2">The secondary output string, or empty when not applicable.</param>
/// <param name="InputEncoding">
/// The encoding of the input: <c>"UTF-8"</c>, <c>"ternary"</c>,
/// <c>"charT_u8"</c>, or <c>"charTC_u8"</c>.
/// </param>
/// <param name="Output1Encoding">
/// The encoding of <see cref="Output1"/>: <c>"charT_u8"</c>, <c>"charTC_u8"</c>,
/// or <c>"UTF-8"</c>.
/// </param>
/// <param name="Output2Encoding">
/// The encoding of <see cref="Output2"/>, or an empty string when
/// <see cref="Output2"/> is empty.
/// </param>
public record CharTStringConversionRecord(
    string Input,
    string Output1,
    string Output2,
    string InputEncoding,
    string Output1Encoding,
    string Output2Encoding);

// -------------------------------------------------------------------------
// CSV import types
// -------------------------------------------------------------------------

/// <summary>
/// A single data row parsed from an imported CSV file.
/// The output columns are intentionally ignored; the conversion is always re-run.
/// </summary>
/// <param name="Input">The input value string.</param>
/// <param name="InputEncoding">
/// The encoding that specifies how to interpret the input:
/// <c>"UTF-8"</c>, <c>"ternary"</c>, <c>"charT_u8"</c>, or <c>"charTC_u8"</c>.
/// </param>
public record CharTStringCsvImportRow(string Input, string InputEncoding);

/// <summary>Describes a single parse failure for one CSV data row.</summary>
/// <param name="RowNumber">1-based row number in the CSV file (header = row 1).</param>
/// <param name="RawLine">The raw text of the offending line.</param>
/// <param name="Message">Human-readable description of the parse failure.</param>
public record CharTStringCsvParseError(int RowNumber, string RawLine, string Message);

/// <summary>The result of parsing an imported CSV file.</summary>
/// <param name="ValidRows">Rows that were parsed successfully and are ready to convert.</param>
/// <param name="Errors">Rows that could not be parsed, with the reason for each failure.</param>
public record CharTStringCsvParseResult(
    List<CharTStringCsvImportRow> ValidRows,
    List<CharTStringCsvParseError> Errors);
