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
///   <item>Fields containing <c>;</c>, <c>"</c>, or embedded newlines are wrapped in
///         double-quotes with internal double-quotes escaped as <c>""</c>.</item>
///   <item>Quoted fields may span multiple physical lines; a record ends only when
///         the closing <c>"</c> is followed by a <c>;</c> or end-of-record
///         (<c>\n</c> outside all quoted fields).</item>
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
    public static CharTStringCsvParseResult Deserialize(string? csv)
    {
        var validRows = new List<CharTStringCsvImportRow>();
        var errors    = new List<CharTStringCsvParseError>();

        if (string.IsNullOrWhiteSpace(csv))
            return new CharTStringCsvParseResult(validRows, errors);

        // Normalise line endings before record splitting.
        string text = csv.Replace("\r\n", "\n").Replace('\r', '\n');

        bool headerSeen = false;

        foreach (var (rawRecord, startLine) in ReadRecords(text))
        {
            if (string.IsNullOrWhiteSpace(rawRecord))
                continue;

            if (!headerSeen)
            {
                // The header must be a plain single-line row with no quoted fields.
                string firstLine = rawRecord.Split('\n')[0].Trim();
                if (!string.Equals(firstLine, Header, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new CharTStringCsvParseError(
                        startLine,
                        rawRecord,
                        $"Expected header \"{Header}\" but got \"{rawRecord.Trim()}\"."));
                    return new CharTStringCsvParseResult(validRows, errors);
                }
                headerSeen = true;
                continue;
            }

            // Parse the (possibly multi-line) record into fields.
            var fields = SplitRecordFields(rawRecord);
            if (fields.Length != 6)
            {
                errors.Add(new CharTStringCsvParseError(
                    startLine,
                    rawRecord,
                    $"Expected 6 semicolon-separated fields, found {fields.Length}."));
                continue;
            }

            string inputField         = fields[0];
            // fields[1] and fields[2] are output columns — ignored on import (re-run semantics)
            string inputEncodingField = fields[3].Trim();
            // fields[4] and fields[5] are output encoding columns — ignored on import

            if (!ValidInputEncodings.Contains(inputEncodingField))
            {
                errors.Add(new CharTStringCsvParseError(
                    startLine,
                    rawRecord,
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
    /// Reads logical CSV records from normalised (LF-only) text, honouring
    /// double-quoted fields that may contain embedded newlines.  A <c>\n</c>
    /// that appears inside a quoted field does NOT terminate the record.
    /// </summary>
    private static IEnumerable<(string Record, int StartLine)> ReadRecords(string text)
    {
        var sb = new StringBuilder();
        bool inQuotes = false;
        int startLine = 1;
        int lineCount = 1;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    // Escaped quote ("") — preserve both chars for SplitRecordFields to decode.
                    sb.Append('"');
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
            }
            else if (c == '\n' && !inQuotes)
            {
                yield return (sb.ToString(), startLine);
                sb.Clear();
                lineCount++;
                startLine = lineCount;
            }
            else
            {
                if (c == '\n') lineCount++; // newline inside a quoted field
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
            yield return (sb.ToString(), startLine);
    }

    /// <summary>
    /// Splits a (possibly multi-line) CSV record on <c>;</c> delimiters,
    /// respecting double-quoted fields (including fields with embedded newlines).
    /// <c>""</c> sequences inside quoted fields are decoded to a single <c>"</c>.
    /// </summary>
    private static string[] SplitRecordFields(string record)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < record.Length; i++)
        {
            char c = record[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < record.Length && record[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip second quote
                }
                else
                {
                    inQuotes = !inQuotes;
                    // Structural quote — not appended.
                }
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString()); // last field (may be empty)
        return fields.ToArray();
    }
}
