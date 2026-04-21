using System.Text;

namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Serialises <see cref="CharTStringConversionRecord"/> collections to the
/// TernaryWorkbench charT-string CSV exchange format and deserialises them back
/// into <see cref="CharTStringCsvImportRow"/> collections.
/// </summary>
/// <remarks>
/// <para><b>Format specification</b></para>
/// <list type="bullet">
///   <item>Field delimiter: <c>;</c></item>
///   <item>Line ending: <c>\n</c> (CRLF accepted on input)</item>
///   <item>Header row (always present):
///         <c>input;output;output;input encoding;output encoding;output encoding</c></item>
///   <item>Fields containing <c>;</c> or <c>"</c> are wrapped in double-quotes with
///         internal double-quotes escaped as <c>""</c>.</item>
///   <item>The two <c>output</c> columns and the two <c>output encoding</c> columns
///         may be empty when only one codec produced output.</item>
/// </list>
/// </remarks>
public static class CharTStringCsvSerializer
{
    /// <summary>
    /// The exact header line that must appear as the first non-blank line in any
    /// CSV file produced or accepted by this serialiser.
    /// </summary>
    public const string Header = "input;output;output;input encoding;output encoding;output encoding";

    /// <summary>Valid input encoding values accepted on import.</summary>
    private static readonly IReadOnlySet<string> ValidInputEncodings =
        new HashSet<string>(StringComparer.Ordinal) { "UTF-8", "ternary", "charT_u8", "charTC_u8" };

    // -------------------------------------------------------------------------
    // Serialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serialises a sequence of <see cref="CharTStringConversionRecord"/>s to a
    /// CSV string in chronological order.
    /// </summary>
    public static string Serialize(IEnumerable<CharTStringConversionRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var r in records)
        {
            sb.Append(EscapeField(r.Input));             sb.Append(';');
            sb.Append(EscapeField(r.Output1));           sb.Append(';');
            sb.Append(EscapeField(r.Output2));           sb.Append(';');
            sb.Append(EscapeField(r.InputEncoding));     sb.Append(';');
            sb.Append(EscapeField(r.Output1Encoding));   sb.Append(';');
            sb.AppendLine(EscapeField(r.Output2Encoding));
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Deserialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a CSV string produced by <see cref="Serialize"/> (or compatible files)
    /// into a <see cref="CharTStringCsvParseResult"/>.
    /// </summary>
    /// <remarks>
    /// The first non-blank line must be the expected header.  Blank lines are
    /// silently skipped.  Each unparseable row contributes an error without
    /// stopping subsequent rows.
    /// </remarks>
    public static CharTStringCsvParseResult Deserialize(string csv)
    {
        var validRows = new List<CharTStringCsvImportRow>();
        var errors    = new List<CharTStringCsvParseError>();

        var lines = csv.Replace("\r\n", "\n").Split('\n');

        int fileLineNumber = 0;
        bool headerSeen = false;

        foreach (var rawLine in lines)
        {
            fileLineNumber++;

            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            if (!headerSeen)
            {
                if (!string.Equals(rawLine.Trim(), Header, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new CharTStringCsvParseError(
                        fileLineNumber,
                        rawLine,
                        $"Expected header \"{Header}\" but got \"{rawLine.Trim()}\"."));
                    return new CharTStringCsvParseResult(validRows, errors);
                }
                headerSeen = true;
                continue;
            }

            // Data row: expect exactly 6 semicolon-separated fields (honouring quoting).
            var fields = SplitFields(rawLine);
            if (fields.Length != 6)
            {
                errors.Add(new CharTStringCsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Expected 6 semicolon-separated fields, found {fields.Length}."));
                continue;
            }

            string inputField         = UnescapeField(fields[0]);
            // fields[1] and fields[2] are output columns — ignored on import
            string inputEncodingField = UnescapeField(fields[3]);
            // fields[4] and fields[5] are output encoding columns — ignored on import

            if (!ValidInputEncodings.Contains(inputEncodingField))
            {
                errors.Add(new CharTStringCsvParseError(
                    fileLineNumber,
                    rawLine,
                    $"Column 4 (input encoding) must be one of UTF-8, ternary, charT_u8, charTC_u8; got \"{inputEncodingField}\"."));
                continue;
            }

            validRows.Add(new CharTStringCsvImportRow(inputField, inputEncodingField));
        }

        return new CharTStringCsvParseResult(validRows, errors);
    }

    // -------------------------------------------------------------------------
    // Field escaping helpers
    // -------------------------------------------------------------------------

    private static string EscapeField(string field)
    {
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
        {
            return '"' + field.Replace("\"", "\"\"") + '"';
        }
        return field;
    }

    private static string UnescapeField(string field)
    {
        field = field.Trim();
        if (field.Length >= 2 && field[0] == '"' && field[^1] == '"')
            field = field[1..^1].Replace("\"\"", "\"");
        return field;
    }

    /// <summary>
    /// Splits a single CSV line on <c>;</c> while respecting double-quoted fields.
    /// A field that begins with <c>"</c> is read until the closing <c>"</c>,
    /// treating <c>""</c> as an escaped quote, so embedded semicolons are preserved.
    /// </summary>
    private static string[] SplitFields(string line)
    {
        var fields = new List<string>();
        int pos = 0;
        while (pos <= line.Length)
        {
            if (pos < line.Length && line[pos] == '"')
            {
                // Quoted field: read until the matching closing quote.
                int start = pos + 1;
                var sb = new System.Text.StringBuilder();
                pos = start;
                while (pos < line.Length)
                {
                    if (line[pos] == '"')
                    {
                        if (pos + 1 < line.Length && line[pos + 1] == '"')
                        {
                            sb.Append('"');
                            pos += 2;
                        }
                        else
                        {
                            pos++; // skip closing quote
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(line[pos++]);
                    }
                }
                fields.Add(sb.ToString());
                // Skip the delimiter (or end-of-string)
                if (pos < line.Length && line[pos] == ';') pos++;
            }
            else
            {
                // Unquoted field: everything up to the next ';'.
                int semi = line.IndexOf(';', pos);
                if (semi == -1)
                {
                    fields.Add(line[pos..]);
                    break;
                }
                fields.Add(line[pos..semi]);
                pos = semi + 1;
            }
        }
        return fields.ToArray();
    }
}
