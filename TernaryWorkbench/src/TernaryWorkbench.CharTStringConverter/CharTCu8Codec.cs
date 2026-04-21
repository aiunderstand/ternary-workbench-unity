using System.Text;

namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Encodes and decodes text using the charTC_u8 variable-length balanced ternary
/// character encoding with CRC (ternary-native 42-symbol single-tryte table, plus
/// direct Unicode code-point mapping for multi-tryte forms).
///
/// CRC:  The last trit (index 5) of every LEAD tryte is set so that the balanced
/// sum of ALL trits in the complete sequence ≡ 0 (mod 3).  This enables single-trit
/// error detection.  Continuation trytes carry no CRC trit.
///
/// Lead-tryte payload trits (after markers, before CRC):
///   1-tryte:  t1..t4  (4 payload, t5=CRC)
///   2-tryte lead: t2..t4 (3 payload, t5=CRC)
///   3-tryte lead: t3..t4 (2 payload, t5=CRC)
///   4-tryte lead: t4     (1 payload, t5=CRC)
///
/// Continuation trytes are identical to charT_u8 (no CRC involvement).
///
/// Multi-tryte Unicode mapping: each form directly encodes the Unicode code point
/// value as an offset; the form is chosen by the minimal-encoding rule:
///   2-tryte: unicode CP 0..6560 (not in single-tryte table)
///   3-tryte: unicode CP 6561..538001
///   4-tryte: unicode CP 538002..14886908 (covers all remaining Unicode up to U+10FFFF)
///
/// Thread safety: all methods are stateless and thread-safe.
/// </summary>
public static class CharTCu8Codec
{
    // -------------------------------------------------------------------------
    // Payload capacities per form (lead payload trits only; continuations same as charT_u8)
    // -------------------------------------------------------------------------
    private const int PayloadTritsLead2 = 3;
    private const int PayloadTritsLead3 = 2;
    private const int PayloadTritsLead4 = 1;
    private const int PayloadTritsCont5 = 5;  // final and middle-1 continuations
    private const int PayloadTritsCont4 = 4;  // middle-2 continuation (4-tryte only)

    // -------------------------------------------------------------------------
    // Lookup tables
    // -------------------------------------------------------------------------

    /// <summary>Unicode code point → canonical 1-tryte string for the 42 special chars.</summary>
    private static readonly Dictionary<int, string> SingleTryteByUcp;

    /// <summary>Canonical 1-tryte string → unicode code point.</summary>
    private static readonly Dictionary<string, int> UcpBySingleTryte;

    static CharTCu8Codec()
    {
        var table = CharTCu8StandardTable.SingleTryteTable;
        SingleTryteByUcp  = new Dictionary<int, string>(42);
        UcpBySingleTryte  = new Dictionary<string, int>(42);
        foreach (var entry in table)
        {
            int ucp = entry.UnicodeCodePoint!.Value;
            SingleTryteByUcp[ucp]                  = entry.TrytePattern;
            UcpBySingleTryte[entry.TrytePattern]   = ucp;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Encodes a UTF-8 string to charTC_u8 ternary text.
    /// Each symbol occupies one line; trytes within a symbol are space-separated.
    /// </summary>
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
    /// Decodes a charTC_u8 ternary string back to UTF-8 text.
    /// CRC violations are reported as warnings but decoding continues.
    /// The decoder self-synchronizes after structural errors.
    /// </summary>
    public static DecodeResult Decode(string? ternaryInput)
    {
        if (string.IsNullOrWhiteSpace(ternaryInput))
            return new DecodeResult(string.Empty, Array.Empty<ConversionError>());

        // Re-use the tokenizer from CharTu8Codec (same format)
        IList<Tryte> trytes;
        try { trytes = CharTu8Codec.TokenizeTrytes(ternaryInput); }
        catch (FormatException ex)
        {
            return new DecodeResult(string.Empty,
                new[] { new ConversionError(0, 0, ternaryInput, ex.Message) });
        }

        var output = new StringBuilder();
        var errors = new List<ConversionError>();
        int i = 0;
        int symbolIndex = 0;

        while (i < trytes.Count)
        {
            int tryteIndex = i;
            Tryte lead = trytes[i];
            TryteRole role = lead.GetRole();

            int sequenceLength = role switch
            {
                TryteRole.SingleTryte => 1,
                TryteRole.Lead2       => 2,
                TryteRole.Lead3       => 3,
                TryteRole.Lead4       => 4,
                _                    => 0
            };

            if (sequenceLength == 0)
            {
                errors.Add(new ConversionError(symbolIndex, tryteIndex,
                    lead.ToBalancedString(),
                    $"Unexpected continuation tryte '{lead}' at position {i}; skipping."));
                i++;
                continue;
            }

            if (i + sequenceLength > trytes.Count)
            {
                string raw = string.Join(" ", trytes.Skip(i).Select(t => t.ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Incomplete {sequenceLength}-tryte sequence at position {i}."));
                i = trytes.Count;
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

            if (!continuationValid) { i++; continue; }

            // CRC check: balanced sum of all trits ≡ 0 (mod 3)
            int totalSum = 0;
            for (int k = 0; k < sequenceLength; k++)
                totalSum += trytes[i + k].BalancedSum;

            if (((totalSum % 3) + 3) % 3 != 0)
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"CRC error: balanced sum of sequence is {totalSum} (≡ {((totalSum % 3) + 3) % 3} mod 3, expected 0)."));
                // Report but continue decoding
            }

            // Decode unicode code point
            int ucp;
            try
            {
                ucp = DecodeUnicodeCodePoint(trytes, i, sequenceLength);
            }
            catch (Exception ex)
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw, ex.Message));
                i += sequenceLength; symbolIndex++; continue;
            }

            // Minimal encoding check
            if (sequenceLength == 1)
            {
                // Already validated via lookup table in DecodeUnicodeCodePoint
            }
            else
            {
                // Over-long: if this unicode CP is in the single-tryte table, reject
                if (SingleTryteByUcp.ContainsKey(ucp))
                {
                    string raw = string.Join(" ",
                        Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                    errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                        $"Over-long encoding: U+{ucp:X4} is a single-tryte character in charTC_u8."));
                    i += sequenceLength; symbolIndex++; continue;
                }
                // Range check
                bool inRange = sequenceLength switch
                {
                    2 => ucp <= CharTCu8StandardTable.MaxCp2Tryte,
                    3 => ucp >= CharTCu8StandardTable.MinCp3Tryte && ucp <= CharTCu8StandardTable.MaxCp3Tryte,
                    4 => ucp >= CharTCu8StandardTable.MinCp4Tryte && ucp <= CharTCu8StandardTable.MaxCp4Tryte,
                    _ => false
                };
                if (!inRange)
                {
                    string raw = string.Join(" ",
                        Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                    errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                        $"U+{ucp:X4} is out of range for a {sequenceLength}-tryte sequence."));
                    i += sequenceLength; symbolIndex++; continue;
                }
            }

            if (!Rune.IsValid(ucp))
            {
                string raw = string.Join(" ",
                    Enumerable.Range(i, sequenceLength).Select(x => trytes[x].ToBalancedString()));
                errors.Add(new ConversionError(symbolIndex, tryteIndex, raw,
                    $"Decoded value {ucp} is not a valid Unicode scalar value."));
                i += sequenceLength; symbolIndex++; continue;
            }

            output.Append(new Rune(ucp).ToString());
            i += sequenceLength;
            symbolIndex++;
        }

        return new DecodeResult(output.ToString(), errors);
    }

    // =========================================================================
    // Encoding helpers
    // =========================================================================

    /// <summary>
    /// Encodes a single Unicode code point to one or more space-separated charTC_u8 tryte strings.
    /// </summary>
    internal static string EncodeCodePoint(int cp)
    {
        if (cp < 0 || cp > CharTCu8StandardTable.MaxCp4Tryte)
            throw new ArgumentOutOfRangeException(nameof(cp), cp, "Code point out of charTC_u8 range.");

        // 1-tryte: special 42-char table
        if (SingleTryteByUcp.TryGetValue(cp, out string? singleTryte))
            return singleTryte;

        // Multi-tryte: select minimal form based on unicode CP value
        if (cp <= CharTCu8StandardTable.MaxCp2Tryte)
            return Encode2Tryte(cp);

        if (cp <= CharTCu8StandardTable.MaxCp3Tryte)
            return Encode3Tryte(cp - CharTCu8StandardTable.MinCp3Tryte);

        return Encode4Tryte(cp - CharTCu8StandardTable.MinCp4Tryte);
    }

    /// <summary>
    /// Encodes a 2-tryte symbol.
    /// Lead: "+ -" marker + 3 payload trits at [2..4] + CRC at [5].
    /// Final: "-" marker + 5 payload trits at [1..5].
    /// </summary>
    private static string Encode2Tryte(int unicodeCp)
    {
        // 8 total payload trits: 3 in lead (before CRC), 5 in final
        int[] payload = ExtractTrits(unicodeCp, PayloadTritsLead2 + PayloadTritsCont5);

        // Build final continuation first (no CRC involvement)
        var final = new Trit[6];
        final[0] = Trit.Minus;
        for (int i = 0; i < 5; i++) final[1 + i] = TritHelper.FromPositionalDigit(payload[3 + i]);

        // Build lead with placeholder CRC=Zero, then compute real CRC
        var leadTrits = new Trit[6];
        leadTrits[0] = Trit.Plus; leadTrits[1] = Trit.Minus; // "+- " marker
        for (int i = 0; i < 3; i++) leadTrits[2 + i] = TritHelper.FromPositionalDigit(payload[i]);
        leadTrits[5] = Trit.Zero; // placeholder

        var leadTryte = new Tryte(leadTrits);
        int totalSumWithoutCrc = leadTryte.BalancedSum + new Tryte(final).BalancedSum;
        // CRC trit was Zero (0) in the placeholder, remove its contribution
        int partialSum = totalSumWithoutCrc;

        Trit crc = TritHelper.FromPositionalDigit(
            CharTCu8StandardTable.BalancedToPositional(
                CharTCu8StandardTable.ComputeCrcBalanced(partialSum)));

        leadTrits[5] = crc;
        return new Tryte(leadTrits).ToBalancedString() + " " + new Tryte(final).ToBalancedString();
    }

    /// <summary>
    /// Encodes a 3-tryte symbol.
    /// Lead: "++-" marker + 2 payload trits at [3..4] + CRC at [5].
    /// Middle: Zero marker + 5 payload trits at [1..5].
    /// Final: "-" marker + 5 payload trits at [1..5].
    /// </summary>
    private static string Encode3Tryte(int offset)
    {
        int[] payload = ExtractTrits(offset, PayloadTritsLead3 + PayloadTritsCont5 + PayloadTritsCont5);

        var final = new Trit[6];
        final[0] = Trit.Minus;
        for (int i = 0; i < 5; i++) final[1 + i] = TritHelper.FromPositionalDigit(payload[7 + i]);

        var mid = new Trit[6];
        mid[0] = Trit.Zero;
        for (int i = 0; i < 5; i++) mid[1 + i] = TritHelper.FromPositionalDigit(payload[2 + i]);

        var leadTrits = new Trit[6];
        leadTrits[0] = Trit.Plus; leadTrits[1] = Trit.Plus; leadTrits[2] = Trit.Minus;
        for (int i = 0; i < 2; i++) leadTrits[3 + i] = TritHelper.FromPositionalDigit(payload[i]);
        leadTrits[5] = Trit.Zero; // placeholder

        var leadTryte = new Tryte(leadTrits);
        int partialSum = leadTryte.BalancedSum + new Tryte(mid).BalancedSum + new Tryte(final).BalancedSum;

        Trit crc = TritHelper.FromPositionalDigit(
            CharTCu8StandardTable.BalancedToPositional(
                CharTCu8StandardTable.ComputeCrcBalanced(partialSum)));

        leadTrits[5] = crc;
        return new Tryte(leadTrits).ToBalancedString() + " "
             + new Tryte(mid).ToBalancedString() + " "
             + new Tryte(final).ToBalancedString();
    }

    /// <summary>
    /// Encodes a 4-tryte symbol.
    /// Lead: "+++-" marker + 1 payload trit at [4] + CRC at [5].
    /// Middle2: "00" marker + 4 payload trits at [2..5].
    /// Middle1: Zero marker + 5 payload trits at [1..5].
    /// Final: "-" marker + 5 payload trits at [1..5].
    /// </summary>
    private static string Encode4Tryte(int offset)
    {
        int totalPayload = PayloadTritsLead4 + PayloadTritsCont4 + PayloadTritsCont5 + PayloadTritsCont5;
        int[] payload = ExtractTrits(offset, totalPayload);

        var final = new Trit[6];
        final[0] = Trit.Minus;
        for (int i = 0; i < 5; i++) final[1 + i] = TritHelper.FromPositionalDigit(payload[10 + i]);

        var mid1 = new Trit[6];
        mid1[0] = Trit.Zero;
        for (int i = 0; i < 5; i++) mid1[1 + i] = TritHelper.FromPositionalDigit(payload[5 + i]);

        var mid2 = new Trit[6];
        mid2[0] = Trit.Zero; mid2[1] = Trit.Zero;
        for (int i = 0; i < 4; i++) mid2[2 + i] = TritHelper.FromPositionalDigit(payload[1 + i]);

        var leadTrits = new Trit[6];
        leadTrits[0] = Trit.Plus; leadTrits[1] = Trit.Plus; leadTrits[2] = Trit.Plus; leadTrits[3] = Trit.Minus;
        leadTrits[4] = TritHelper.FromPositionalDigit(payload[0]);
        leadTrits[5] = Trit.Zero; // placeholder

        var leadTryte = new Tryte(leadTrits);
        int partialSum = leadTryte.BalancedSum
                       + new Tryte(mid2).BalancedSum
                       + new Tryte(mid1).BalancedSum
                       + new Tryte(final).BalancedSum;

        Trit crc = TritHelper.FromPositionalDigit(
            CharTCu8StandardTable.BalancedToPositional(
                CharTCu8StandardTable.ComputeCrcBalanced(partialSum)));

        leadTrits[5] = crc;
        return new Tryte(leadTrits).ToBalancedString() + " "
             + new Tryte(mid2).ToBalancedString() + " "
             + new Tryte(mid1).ToBalancedString() + " "
             + new Tryte(final).ToBalancedString();
    }

    // =========================================================================
    // Decoding helpers
    // =========================================================================

    private static int DecodeUnicodeCodePoint(IList<Tryte> trytes, int startIndex, int length)
    {
        switch (length)
        {
            case 1:
                string pattern = trytes[startIndex].ToBalancedString();
                if (!UcpBySingleTryte.TryGetValue(pattern, out int ucp1))
                    throw new FormatException($"Unknown single-tryte pattern '{pattern}'.");
                return ucp1;

            case 2:
                // Lead payload: [2..4] (3 trits), Final: [1..5] (5 trits) = 8 trits → offset = unicode CP
            {
                var p = new List<int>();
                for (int j = 2; j <= 4; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex][j]));
                for (int j = 1; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                return PayloadToOffset(p); // = unicode CP directly
            }

            case 3:
                // Lead payload: [3..4] (2 trits), Middle: [1..5] (5), Final: [1..5] (5) = 12 trits
            {
                var p = new List<int>();
                for (int j = 3; j <= 4; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex][j]));
                for (int j = 1; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                for (int j = 1; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 2][j]));
                return CharTCu8StandardTable.MinCp3Tryte + PayloadToOffset(p);
            }

            case 4:
                // Lead payload: [4] (1 trit), Middle2: [2..5] (4), Middle1: [1..5] (5), Final: [1..5] (5) = 15
            {
                var p = new List<int>();
                p.Add(TritHelper.ToPositionalDigit(trytes[startIndex][4]));
                for (int j = 2; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 1][j]));
                for (int j = 1; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 2][j]));
                for (int j = 1; j <= 5; j++) p.Add(TritHelper.ToPositionalDigit(trytes[startIndex + 3][j]));
                return CharTCu8StandardTable.MinCp4Tryte + PayloadToOffset(p);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(length));
        }
    }

    // =========================================================================
    // Utility
    // =========================================================================

    private static int[] ExtractTrits(int value, int count)
    {
        var result = new int[count];
        for (int i = count - 1; i >= 0; i--) { result[i] = value % 3; value /= 3; }
        return result;
    }

    private static int PayloadToOffset(List<int> digits)
    {
        int offset = 0;
        foreach (int d in digits) { offset *= 3; offset += d; }
        return offset;
    }
}
