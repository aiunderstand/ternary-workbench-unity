using System.Text;

namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Encodes and decodes text using the charT_u8 variable-length balanced ternary
/// character encoding (no CRC, 1:1 Unicode code-point mapping).
///
/// Encoding format:
///   • Each symbol is 1–4 trytes.
///   • Within a symbol, trytes are separated by a single space.
///   • Symbols are separated by newlines.
///   • Each tryte is 6 characters from {'-','0','+'}, MST first.
///
/// Thread safety: all methods are stateless and thread-safe.
/// </summary>
public static class CharTu8Codec
{
    // -------------------------------------------------------------------------
    // Payload capacity per form  (number of payload trits)
    // -------------------------------------------------------------------------
    // 1-tryte:  5 trits, but 126 valid patterns (not 3^5=243) due to reserved prefixes.
    //           Rather than a formula, the encoder uses a lookup table built from
    //           CharTu8StandardTable.SingleTryteTable.
    // 2-tryte:  lead has 4 payload trits, final continuation has 5 → 9 total → 3^9 = 19683
    // 3-tryte:  lead 3, middle 5, final 5 → 13 total → 3^13 = 1594323
    // 4-tryte:  lead 2, middle2 4, middle1 5, final 5 → 16 total → 3^16 = 43046721
    private const int PayloadTrits2 = 9;
    private const int PayloadTrits3 = 13;
    private const int PayloadTrits4 = 16;

    // -------------------------------------------------------------------------
    // Lookup tables (built once at startup)
    // -------------------------------------------------------------------------

    /// <summary>Maps Unicode code point (0–125) → canonical 1-tryte string.</summary>
    private static readonly string[] SingleTryteByCP;

    /// <summary>Maps canonical 1-tryte string → code point.</summary>
    private static readonly Dictionary<string, int> CPBySingleTryte;

    static CharTu8Codec()
    {
        var table = CharTu8StandardTable.SingleTryteTable;
        SingleTryteByCP = new string[126];
        CPBySingleTryte = new Dictionary<string, int>(126);
        foreach (var entry in table)
        {
            SingleTryteByCP[entry.CodePoint] = entry.TrytePattern;
            CPBySingleTryte[entry.TrytePattern] = entry.CodePoint;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Encodes a UTF-8 string to charT_u8 ternary text.
    /// Each symbol occupies one line; trytes within a symbol are space-separated.
    /// </summary>
    /// <param name="utf8Text">The input string. Null is treated as empty.</param>
    /// <returns>An <see cref="EncodeResult"/> containing the encoded text and any errors.</returns>
    public static EncodeResult Encode(string? utf8Text)
    {
        if (string.IsNullOrEmpty(utf8Text))
            return new EncodeResult(string.Empty, Array.Empty<ConversionError>());

        var output = new StringBuilder();
        var errors = new List<ConversionError>();
        int symbolIndex = 0;

        foreach (var rune in utf8Text.EnumerateRunes())
        {
            int cp = rune.Value;
            try
            {
                string encoded = EncodeCodePoint(cp);
                if (output.Length > 0) output.Append('\n');
                output.Append(encoded);
            }
            catch (Exception ex)
            {
                errors.Add(new ConversionError(symbolIndex, symbolIndex, rune.ToString(),
                    $"Cannot encode U+{cp:X4}: {ex.Message}"));
            }
            symbolIndex++;
        }

        return new EncodeResult(output.ToString(), errors);
    }

    /// <summary>
    /// Decodes a charT_u8 ternary string back to UTF-8 text.
    /// The decoder self-synchronizes after errors: it skips to the next
    /// '+'-leading tryte and continues.
    /// </summary>
    /// <param name="ternaryInput">
    /// The ternary input. Symbols may be separated by newlines or '|'.
    /// Trytes within a symbol may be space-separated or concatenated.
    /// </param>
    /// <returns>A <see cref="DecodeResult"/> containing the decoded text and any errors.</returns>
    public static DecodeResult Decode(string? ternaryInput)
    {
        if (string.IsNullOrWhiteSpace(ternaryInput))
            return new DecodeResult(string.Empty, Array.Empty<ConversionError>());

        var trytes = TokenizeTrytes(ternaryInput);
        var output = new StringBuilder();
        var errors = new List<ConversionError>();
        int i = 0;
        int symbolIndex = 0;
        int tryteIndex = 0;

        while (i < trytes.Count)
        {
            tryteIndex = i;
            Tryte lead = trytes[i];
            TryteRole role = lead.GetRole();

            int sequenceLength = role switch
            {
                TryteRole.SingleTryte => 1,
                TryteRole.Lead2       => 2,
                TryteRole.Lead3       => 3,
                TryteRole.Lead4       => 4,
                _ => 0 // continuation without lead — error
            };

            if (sequenceLength == 0)
            {
                // Unexpected continuation tryte — skip and re-sync
                errors.Add(new ConversionError(symbolIndex, tryteIndex,
                    lead.ToBalancedString(),
                    $"Unexpected continuation tryte '{lead}' at position {i}; skipping."));
                i++;
                continue;
            }

            // Check we have enough trytes for the full sequence
            if (i + sequenceLength > trytes.Count)
            {
                string raw = string.Join(" ", trytes.Skip(i).Select(t => t.ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Incomplete sequence starting at tryte {i}: expected {sequenceLength} trytes, got {trytes.Count - i}."));
                i = trytes.Count; // consume remaining
                continue;
            }

            // Validate continuation roles
            bool continuationValid = true;
            for (int k = 1; k < sequenceLength; k++)
            {
                TryteRole contRole = trytes[i + k].GetRole();
                bool valid = k == sequenceLength - 1
                    ? contRole == TryteRole.ContinuationFinal
                    : (contRole == TryteRole.ContinuationMiddle1 || contRole == TryteRole.ContinuationMiddle2);
                if (!valid)
                {
                    string raw = string.Join(" ",
                        Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                    errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                        $"Tryte {i + k} has unexpected role {contRole} in a {sequenceLength}-tryte sequence."));
                    continuationValid = false;
                    break;
                }
            }

            if (!continuationValid)
            {
                i++; // skip lead and try re-sync
                continue;
            }

            // Extract payload and compute code point
            int cp;
            try
            {
                cp = DecodeCodePoint(trytes, i, sequenceLength);
            }
            catch (OverflowException ex)
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw, ex.Message));
                i += sequenceLength;
                symbolIndex++;
                continue;
            }

            // Minimal encoding check
            int minCp = MinCpForFormLength(sequenceLength);
            int maxCp = MaxCpForFormLength(sequenceLength);
            if (cp < minCp)
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Over-long encoding: CP {cp} encoded as {sequenceLength}-tryte sequence (minimum form requires {CpFormLength(cp)} tryte(s))."));
                i += sequenceLength;
                symbolIndex++;
                continue;
            }
            if (cp > maxCp)
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Code point {cp} is out of range for a {sequenceLength}-tryte sequence."));
                i += sequenceLength;
                symbolIndex++;
                continue;
            }

            if (!TryCodePointToRune(cp, out Rune rune))
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Code point {cp} (U+{cp:X4}) is not a valid Unicode scalar value."));
                i += sequenceLength;
                symbolIndex++;
                continue;
            }

            output.Append(rune.ToString());
            i += sequenceLength;
            symbolIndex++;
        }

        return new DecodeResult(output.ToString(), errors);
    }

    // =========================================================================
    // Internal encoding helpers
    // =========================================================================

    /// <summary>Encodes a single Unicode code point to one or more space-separated tryte strings.</summary>
    internal static string EncodeCodePoint(int cp)
    {
        if (cp < 0 || cp > CharTu8StandardTable.MaxCp4Tryte)
            throw new ArgumentOutOfRangeException(nameof(cp), cp, "Code point out of charT_u8 range.");

        if (cp <= CharTu8StandardTable.MaxCp1Tryte)
            return SingleTryteByCP[cp];

        if (cp <= CharTu8StandardTable.MaxCp2Tryte)
            return Encode2Tryte(cp - CharTu8StandardTable.MinCp2Tryte);

        if (cp <= CharTu8StandardTable.MaxCp3Tryte)
            return Encode3Tryte(cp - CharTu8StandardTable.MinCp3Tryte);

        return Encode4Tryte(cp - CharTu8StandardTable.MinCp4Tryte);
    }

    /// <summary>
    /// Encodes a 2-tryte symbol.
    /// Format: "+- p1 p2 p3 p4" (lead) + "- q1 q2 q3 q4 q5" (final continuation).
    /// Payload = 9 trits, MST first.
    /// </summary>
    private static string Encode2Tryte(int offset)
    {
        // Split 9 payload trits: first 4 into lead, last 5 into final continuation
        int[] payload = ExtractTrits(offset, PayloadTrits2);

        var lead = new Trit[6];
        lead[0] = Trit.Plus; lead[1] = Trit.Minus; // "+-" marker
        for (int i = 0; i < 4; i++) lead[2 + i] = TritHelper.FromPositionalDigit(payload[i]);

        var cont = new Trit[6];
        cont[0] = Trit.Minus; // "-" continuation marker
        for (int i = 0; i < 5; i++) cont[1 + i] = TritHelper.FromPositionalDigit(payload[4 + i]);

        return new Tryte(lead).ToBalancedString() + " " + new Tryte(cont).ToBalancedString();
    }

    /// <summary>
    /// Encodes a 3-tryte symbol.
    /// Format: "++- p1 p2 p3" (lead) + "0 q1..q5" (middle) + "- r1..r5" (final).
    /// Payload = 13 trits, MST first.
    /// </summary>
    private static string Encode3Tryte(int offset)
    {
        int[] payload = ExtractTrits(offset, PayloadTrits3);

        var lead = new Trit[6];
        lead[0] = Trit.Plus; lead[1] = Trit.Plus; lead[2] = Trit.Minus; // "++-"
        for (int i = 0; i < 3; i++) lead[3 + i] = TritHelper.FromPositionalDigit(payload[i]);

        // Middle continuation: t0=Zero (role marker), t1..t5 = payload trits.
        // If payload[3] happens to be Zero, the role appears as ContinuationMiddle2;
        // the decoder handles this correctly by reading t1..t5 based on sequence position.
        var mid = new Trit[6];
        mid[0] = Trit.Zero;
        for (int i = 0; i < 5; i++) mid[1 + i] = TritHelper.FromPositionalDigit(payload[3 + i]);

        var cont = new Trit[6];
        cont[0] = Trit.Minus;
        for (int i = 0; i < 5; i++) cont[1 + i] = TritHelper.FromPositionalDigit(payload[8 + i]);

        return new Tryte(lead).ToBalancedString() + " "
             + new Tryte(mid).ToBalancedString() + " "
             + new Tryte(cont).ToBalancedString();
    }

    /// <summary>
    /// Encodes a 4-tryte symbol.
    /// Format: "+++- p1 p2" (lead) + "00 q1..q4" (middle2) + "0 r1..r5" (middle1) + "- s1..s5" (final).
    /// Payload = 16 trits, MST first.
    /// </summary>
    private static string Encode4Tryte(int offset)
    {
        int[] payload = ExtractTrits(offset, PayloadTrits4);

        var lead = new Trit[6];
        lead[0] = Trit.Plus; lead[1] = Trit.Plus; lead[2] = Trit.Plus; lead[3] = Trit.Minus; // "+++-"
        for (int i = 0; i < 2; i++) lead[4 + i] = TritHelper.FromPositionalDigit(payload[i]);

        var mid2 = new Trit[6];
        mid2[0] = Trit.Zero; mid2[1] = Trit.Zero; // "00" = ContinuationMiddle2
        for (int i = 0; i < 4; i++) mid2[2 + i] = TritHelper.FromPositionalDigit(payload[2 + i]);

        // Middle1 continuation: t0=Zero (role marker), t1..t5 = payload trits.
        var mid1 = new Trit[6];
        mid1[0] = Trit.Zero;
        for (int i = 0; i < 5; i++) mid1[1 + i] = TritHelper.FromPositionalDigit(payload[6 + i]);

        var cont = new Trit[6];
        cont[0] = Trit.Minus;
        for (int i = 0; i < 5; i++) cont[1 + i] = TritHelper.FromPositionalDigit(payload[11 + i]);

        return new Tryte(lead).ToBalancedString() + " "
             + new Tryte(mid2).ToBalancedString() + " "
             + new Tryte(mid1).ToBalancedString() + " "
             + new Tryte(cont).ToBalancedString();
    }

    // =========================================================================
    // Internal decoding helpers
    // =========================================================================

    /// <summary>Decodes a code point from a slice of trytes starting at <paramref name="startIndex"/>.</summary>
    private static int DecodeCodePoint(IList<Tryte> trytes, int startIndex, int length)
    {
        // Collect payload trits in MST-first order
        var payload = new List<int>();

        switch (length)
        {
            case 1:
                // Single tryte: the 5 trits after the lead '+' are not entirely free;
                // the CP is decoded via the reverse lookup table.
                string pattern = trytes[startIndex].ToBalancedString();
                if (!CPBySingleTryte.TryGetValue(pattern, out int cp1))
                    throw new FormatException($"Unknown single-tryte pattern '{pattern}'.");
                return cp1;

            case 2:
                // Lead: trits [2..5], Final: trits [1..5]
                for (int j = 2; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex][j]));
                for (int j = 1; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                return CharTu8StandardTable.MinCp2Tryte + PayloadToOffset(payload);

            case 3:
                // Lead: [3..5], Middle: [1..5], Final: [1..5]
                for (int j = 3; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex][j]));
                for (int j = 1; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                for (int j = 1; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 2][j]));
                return CharTu8StandardTable.MinCp3Tryte + PayloadToOffset(payload);

            case 4:
                // Lead: [4..5], Middle2: [2..5], Middle1: [1..5], Final: [1..5]
                for (int j = 4; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex][j]));
                for (int j = 2; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                for (int j = 1; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 2][j]));
                for (int j = 1; j <= 5; j++) payload.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 3][j]));
                return CharTu8StandardTable.MinCp4Tryte + PayloadToOffset(payload);

            default:
                throw new ArgumentOutOfRangeException(nameof(length), length, "Sequence length must be 1–4.");
        }
    }

    // =========================================================================
    // Tokenizer
    // =========================================================================

    /// <summary>
    /// Splits the input string into individual <see cref="Tryte"/> values.
    /// Accepts symbols separated by newlines or '|', trytes optionally separated
    /// by spaces within symbols. Blank lines are silently ignored.
    /// Throws <see cref="FormatException"/> if any 6-char block is not a valid tryte.
    /// </summary>
    internal static IList<Tryte> TokenizeTrytes(string input)
    {
        var result = new List<Tryte>();

        // Normalize: replace '|' with newline, split on newlines
        var lines = input.Replace('|', '\n').Split('\n');
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            // Within a line, strip spaces and split into 6-char blocks
            string compact = trimmed.Replace(" ", "");
            if (compact.Length % Tryte.Length != 0)
                throw new FormatException(
                    $"Line '{trimmed}' does not contain a whole number of 6-trit trytes (got {compact.Length} trit characters).");

            for (int i = 0; i < compact.Length; i += Tryte.Length)
                result.Add(Tryte.Parse(compact.Substring(i, Tryte.Length)));
        }

        return result;
    }

    // =========================================================================
    // Utility
    // =========================================================================

    /// <summary>
    /// Extracts <paramref name="count"/> base-3 positional digits from
    /// <paramref name="value"/>, MST first.
    /// </summary>
    private static int[] ExtractTrits(int value, int count)
    {
        var result = new int[count];
        for (int i = count - 1; i >= 0; i--)
        {
            result[i] = value % 3;
            value /= 3;
        }
        return result;
    }

    /// <summary>Converts a list of positional digit values (0,1,2) to an integer offset, MST first.</summary>
    private static int PayloadToOffset(List<int> digits)
    {
        int offset = 0;
        foreach (int d in digits) { offset *= 3; offset += d; }
        return offset;
    }

    private static int MinCpForFormLength(int len) => len switch
    {
        1 => CharTu8StandardTable.MinCp1Tryte,
        2 => CharTu8StandardTable.MinCp2Tryte,
        3 => CharTu8StandardTable.MinCp3Tryte,
        4 => CharTu8StandardTable.MinCp4Tryte,
        _ => throw new ArgumentOutOfRangeException(nameof(len))
    };

    private static int MaxCpForFormLength(int len) => len switch
    {
        1 => CharTu8StandardTable.MaxCp1Tryte,
        2 => CharTu8StandardTable.MaxCp2Tryte,
        3 => CharTu8StandardTable.MaxCp3Tryte,
        4 => CharTu8StandardTable.MaxCp4Tryte,
        _ => throw new ArgumentOutOfRangeException(nameof(len))
    };

    /// <summary>Returns the minimum tryte-form length required to encode a code point.</summary>
    internal static int CpFormLength(int cp)
    {
        if (cp <= CharTu8StandardTable.MaxCp1Tryte) return 1;
        if (cp <= CharTu8StandardTable.MaxCp2Tryte) return 2;
        if (cp <= CharTu8StandardTable.MaxCp3Tryte) return 3;
        return 4;
    }

    private static bool TryCodePointToRune(int cp, out Rune rune)
    {
        if (Rune.IsValid(cp)) { rune = new Rune(cp); return true; }
        rune = default;
        return false;
    }
}
