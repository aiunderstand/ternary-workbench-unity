namespace TernaryWorkbench.Core;

/// <summary>
/// A single data row parsed from an imported CSV file, representing the
/// parameters needed to re-run a conversion.  The <c>output</c> column of
/// the CSV is intentionally ignored; the conversion is always re-executed.
/// </summary>
/// <param name="Input">The input value string.</param>
/// <param name="FromRadix">The source radix.</param>
/// <param name="ToRadix">The target radix.</param>
/// <param name="LsdFirst">
/// <see langword="true"/> for least-significant digit first output;
/// <see langword="false"/> for most-significant digit first (default).
/// </param>
/// <param name="FixedOutputLength">
/// The fixed output word length, or <see langword="null"/> for variable length.
/// </param>
public record CsvImportRow(
    string Input,
    Radix FromRadix,
    Radix ToRadix,
    bool LsdFirst,
    int? FixedOutputLength);

/// <summary>Describes a single parse failure for one CSV data row.</summary>
/// <param name="RowNumber">1-based row number in the CSV file (header = row 1).</param>
/// <param name="RawLine">The raw text of the offending line.</param>
/// <param name="Message">Human-readable description of the parse failure.</param>
public record CsvParseError(int RowNumber, string RawLine, string Message);

/// <summary>The result of parsing an imported CSV file.</summary>
/// <param name="ValidRows">Rows that were parsed successfully and are ready to convert.</param>
/// <param name="Errors">Rows that could not be parsed, with the reason for each failure.</param>
public record CsvParseResult(List<CsvImportRow> ValidRows, List<CsvParseError> Errors);
