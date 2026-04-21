using System.Text;

namespace TernaryWorkbench.Core;

/// <summary>
/// Serialises <see cref="ConversionRecord"/> collections to the TernaryWorkbench CSV
/// exchange format and deserialises them back into <see cref="CsvImportRow"/> collections.
/// </summary>
/// <remarks>
/// <para><b>Format specification</b></para>
/// <list type="bullet">
///   <item>Field delimiter: <c>;</c></item>
///   <item>Line ending: <c>\n</c> (CRLF accepted on input)</item>
///   <item>Header row (always present): <c>input;output;from radix;to radix;significant digit;output word length</c></item>
///   <item><c>significant digit</c>: <c>MSD</c> (most-significant first) or <c>LSD</c> (least-significant first)</item>
///   <item><c>output word length</c>: integer, or empty when variable-length</item>
///   <item>Radix columns: enum member name, e.g. <c>Base3Balanced</c></item>
/// </list>
/// </remarks>
public static class ConversionCsvSerializer
{
    /// <summary>
    /// The exact header line that must appear as the first non-empty line in any
    /// CSV file produced or accepted by this serialiser.
    /// </summary>
    public const string Header = "input;output;from radix;to radix;significant digit;output word length";

    // -------------------------------------------------------------------------
    // Serialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serialises a sequence of <see cref="ConversionRecord"/>s to a CSV string
    /// in chronological order (first record in the sequence becomes the first data row).
    /// </summary>
    /// <param name="records">The records to serialise. May be empty.</param>
    /// <returns>
    /// A UTF-8 string beginning with the header row followed by one row per record,
    /// lines terminated with <c>\n</c>.
    /// </returns>
    public static string Serialize(IEnumerable<ConversionRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var r in records)
        {
            sb.Append(EscapeField(r.Input));
            sb.Append(';');
            sb.Append(EscapeField(r.Output));
            sb.Append(';');
            sb.Append(r.FromRadix.ToString());
            sb.Append(';');
            sb.Append(r.ToRadix.ToString());
            sb.Append(';');
            sb.Append(r.LsdFirst ? "LSD" : "MSD");
            sb.Append(';');
            if (r.FixedOutputLength.HasValue)
                sb.Append(r.FixedOutputLength.Value);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Deserialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a CSV string produced by <see cref="Serialize"/> (or compatible files)
    /// into a <see cref="CsvParseResult"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Blank lines are silently skipped.</item>
    ///   <item>Windows-style CRLF line endings are normalised to LF before parsing.</item>
    ///   <item>The first non-blank line must be the expected header; if not, a single
    ///         file-level error is returned and no rows are parsed.</item>
    ///   <item>Each data row that cannot be parsed contributes a <see cref="CsvParseError"/>
    ///         but does not stop processing of subsequent rows.</item>
    /// </list>
    /// </remarks>
    /// <param name="csv">The CSV text to parse.</param>
    /// <returns>A <see cref="CsvParseResult"/> with valid rows and any per-row errors.</returns>
    public static CsvParseResult Deserialize(string csv)
    {
        var validRows = new List<CsvImportRow>();
        var errors    = new List<CsvParseError>();

        // Normalise line endings and split
        var lines = csv.Replace("\r\n", "\n").Split('\n');

        int fileLineNumber = 0;  // tracks the 1-based position in the raw file
        bool headerSeen = false;

        foreach (var rawLine in lines)
        {
            fileLineNumber++;

            // Skip blank lines
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            // The first non-blank line must be the header
            if (!headerSeen)
            {
                if (!string.Equals(rawLine.Trim(), Header, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new CsvParseError(
                        fileLineNumber,
                        rawLine,
                        $"Expected header \"{Header}\" but got \"{rawLine.Trim()}\"."));
                    // Without a valid header we cannot trust the column layout.
                    return new CsvParseResult(validRows, errors);
                }
                headerSeen = true;
                continue;
            }

            // Parse data row
            var fields = rawLine.Split(';');
            if (fields.Length != 6)
            {
                errors.Add(new CsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Expected 6 semicolon-separated fields, found {fields.Length}."));
                continue;
            }

            string inputField          = fields[0].Trim();
            // fields[1] is the output column — intentionally ignored on import
            string fromRadixField      = fields[2].Trim();
            string toRadixField        = fields[3].Trim();
            string sigDigitField       = fields[4].Trim();
            string wordLengthField     = fields[5].Trim();

            // from radix
            if (!Enum.TryParse<Radix>(fromRadixField, ignoreCase: false, out Radix fromRadix))
            {
                errors.Add(new CsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Unknown radix \"{fromRadixField}\" in column 3 (from radix)."));
                continue;
            }

            // to radix
            if (!Enum.TryParse<Radix>(toRadixField, ignoreCase: false, out Radix toRadix))
            {
                errors.Add(new CsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Unknown radix \"{toRadixField}\" in column 4 (to radix)."));
                continue;
            }

            // significant digit order
            bool lsdFirst;
            if (sigDigitField.Equals("MSD", StringComparison.OrdinalIgnoreCase))
                lsdFirst = false;
            else if (sigDigitField.Equals("LSD", StringComparison.OrdinalIgnoreCase))
                lsdFirst = true;
            else
            {
                errors.Add(new CsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Column 5 (significant digit) must be \"MSD\" or \"LSD\", got \"{sigDigitField}\"."));
                continue;
            }

            // output word length (optional)
            int? fixedOutputLength = null;
            if (!string.IsNullOrEmpty(wordLengthField))
            {
                if (!int.TryParse(wordLengthField, out int wl) || wl <= 0)
                {
                    errors.Add(new CsvParseError(
                        fileLineNumber,
                        rawLine,
                        $"Column 6 (output word length) must be a positive integer or empty, got \"{wordLengthField}\"."));
                    continue;
                }
                fixedOutputLength = wl;
            }

            validRows.Add(new CsvImportRow(inputField, fromRadix, toRadix, lsdFirst, fixedOutputLength));
        }

        return new CsvParseResult(validRows, errors);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Quotes a field if it contains the delimiter (<c>;</c>), a double-quote,
    /// or a newline character, following RFC 4180 quoting rules.
    /// </summary>
    private static string EscapeField(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
