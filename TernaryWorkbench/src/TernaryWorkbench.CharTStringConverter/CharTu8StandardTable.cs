using System.Text;

namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Specification tables and metadata for the charT_u8 encoding standard
/// (no CRC, 1:1 Unicode mapping).
/// </summary>
public static class CharTu8StandardTable
{
    // -------------------------------------------------------------------------
    // Code-point range boundaries (inclusive minimum CP for each form)
    // -------------------------------------------------------------------------

    /// <summary>Minimum code point handled by a 1-tryte sequence (always 0).</summary>
    public const int MinCp1Tryte = 0;

    /// <summary>Maximum code point handled by a 1-tryte sequence.</summary>
    public const int MaxCp1Tryte = 125;

    /// <summary>Minimum code point for a 2-tryte sequence.</summary>
    public const int MinCp2Tryte = 126;

    /// <summary>Maximum code point for a 2-tryte sequence. (126 + 3^9 − 1)</summary>
    public const int MaxCp2Tryte = 126 + 19_683 - 1; // 19808

    /// <summary>Minimum code point for a 3-tryte sequence.</summary>
    public const int MinCp3Tryte = 19_809;

    /// <summary>Maximum code point for a 3-tryte sequence. (19809 + 3^13 − 1)</summary>
    public const int MaxCp3Tryte = 19_809 + 1_594_323 - 1; // 1614131

    /// <summary>Minimum code point for a 4-tryte sequence.</summary>
    public const int MinCp4Tryte = 1_614_132;

    /// <summary>Maximum code point for a 4-tryte sequence. (1614132 + 3^16 − 1)</summary>
    public const int MaxCp4Tryte = 1_614_132 + 43_046_721 - 1; // 44660852

    // -------------------------------------------------------------------------
    // 1-tryte table
    // -------------------------------------------------------------------------

    /// <summary>
    /// All 126 valid single-tryte code-point entries, ordered by code point (0–125).
    ///
    /// Encoding rule: the lead trit is always '+'. The 5 remaining trits encode
    /// the code point using the following restricted groups, ordered ascending:
    ///
    ///   Group 1 (CP 0–80):   t1=0, t2..t5 free → 81 patterns
    ///   Group 2 (CP 81–107): t1=+, t2=0, t3..t5 free → 27 patterns
    ///   Group 3 (CP 108–116):t1=+, t2=+, t3=0, t4..t5 free → 9 patterns
    ///   Group 4 (CP 117–125):t1=+, t2=+, t3=+, t4..t5 free (≠ - in t4 avoided by
    ///                         prior lead rules) → 9 patterns, but Lead4 steals
    ///                         t4=- leaving 9 - 3 = ... wait, Lead4 needs t1..t3=+, t4=-.
    ///                         So t4 ∈ {0,+} → 2×3 = 6, but we need 9. Recalculate:
    ///
    /// Actual count derivation:
    ///   Total '+'-leading trytes = 3^5 = 243
    ///   Lead2 reserved (t1=-)             = 3^4 = 81
    ///   Lead3 reserved (t1=+, t2=-)       = 3^3 = 27
    ///   Lead4 reserved (t1=+,t2=+,t3=-)   = 3^2 = 9
    ///   Available for SingleTryte = 243 − 81 − 27 − 9 = 126
    ///
    /// Within the 126, ascending order over the raw 5-trit payload value
    /// (treating '-'=0,'0'=1,'+'=2 positionally, skipping reserved patterns).
    /// </summary>
    public static IReadOnlyList<CodePointEntry> SingleTryteTable { get; } = BuildSingleTryteTable();

    // -------------------------------------------------------------------------
    // Spec text
    // -------------------------------------------------------------------------

    /// <summary>
    /// Multi-paragraph specification text for the charT_u8 standard,
    /// suitable for display in a collapsible panel.
    /// </summary>
    public static string SpecificationText { get; } = """
        # charT_u8 — Ternary UTF-8 Encoding (no CRC)

        ## Overview
        charT_u8 is a variable-length balanced ternary character encoding with a 1:1 mapping
        to Unicode code points. Each symbol is encoded as 1 to 4 trytes (6 trits each),
        using leading-trit patterns to identify tryte roles. The encoding is self-synchronizing:
        any '+'-leading tryte is always the start of a new symbol; any '-' or '0'-leading tryte
        is always a continuation. No backward scan is needed to re-synchronize after an error.

        ## Tryte Roles
        The structural role of each tryte is determined solely by its leading trits:

        | Leading trits  | Role                  | Payload trits |
        |----------------|-----------------------|---------------|
        | + (single)     | 1-tryte symbol        | 5 (126 valid) |
        | + -            | Lead of 2-tryte seq   | 4             |
        | + + -          | Lead of 3-tryte seq   | 3             |
        | + + + -        | Lead of 4-tryte seq   | 2             |
        | 0 0 x x x x   | Middle continuation   | 4             |
        | 0 x x x x x   | Middle continuation   | 5             |
        | - x x x x x   | Final continuation    | 5             |

        ## Code-Point Ranges
        | Form    | CP range              | Unicode coverage        |
        |---------|-----------------------|-------------------------|
        | 1-tryte | 0 – 125               | ASCII 0–125 (NUL–'}'). '~'(126) and DEL(127) use 2-tryte. |
        | 2-tryte | 126 – 19 808          | Full Basic Latin + Latin-1 Supplement and beyond |
        | 3-tryte | 19 809 – 1 614 131    | Covers all Unicode (max U+10FFFF = 1 114 111) |
        | 4-tryte | 1 614 132 – 44 660 852| Ternary extension space, beyond Unicode |

        ## Self-Synchronization
        To re-sync after a stream error, advance forward until a '+'-leading tryte is found.
        That tryte is the start of the next symbol. No backward scan is required—a key advantage
        over binary UTF-8.

        ## Minimal Encoding Rule
        A code point MUST be encoded using the shortest valid form. Over-long encodings
        (e.g. encoding CP 65 'A' as a 2-tryte sequence) are invalid and MUST be rejected
        by decoders. This rule prevents security bypass attacks analogous to the UTF-8
        over-long encoding vulnerability.

        ## Payload Encoding
        Payload trits use the convention: '-' = 0, '0' = 1, '+' = 2 (positional digit values).
        The offset from the form's minimum code point is encoded MST-first.

        ## 1-Tryte ASCII Mapping
        Code points 0–125 map to 1-tryte symbols in ascending order.
        ASCII characters '~' (CP 126) and DEL (CP 127) require 2-tryte encoding.
        All other printable ASCII fits in a single tryte.
        """;

    // -------------------------------------------------------------------------
    // Internal builder
    // -------------------------------------------------------------------------

    private static IReadOnlyList<CodePointEntry> BuildSingleTryteTable()
    {
        var entries = new List<CodePointEntry>(126);
        int cp = 0;

        // Enumerate all 243 5-trit payloads in ascending positional order
        // (each trit index 0..4, value 0=Minus,1=Zero,2=Plus → digit value)
        for (int raw = 0; raw < 243; raw++) // 3^5
        {
            // Decode 5 positional digits from raw index
            int r = raw;
            var payload = new int[5];
            for (int i = 4; i >= 0; i--)
            {
                payload[i] = r % 3;
                r /= 3;
            }

            // Determine role by inspecting what the full 6-trit tryte would look like
            // trit0=Plus always; payload[0..4] = trits 1..5
            // Check reserved prefixes:
            //   Lead2:  payload[0] == 0 (Minus) → skip
            //   Lead3:  payload[0] == 2(Plus), payload[1] == 0(Minus) → skip
            //   Lead4:  payload[0] == 2(Plus), payload[1] == 2(Plus), payload[2] == 0(Minus) → skip
            bool isLead2 = payload[0] == 0; // t1=Minus
            bool isLead3 = payload[0] == 2 && payload[1] == 0; // t1=Plus, t2=Minus
            bool isLead4 = payload[0] == 2 && payload[1] == 2 && payload[2] == 0; // t1=Plus, t2=Plus, t3=Minus

            if (isLead2 || isLead3 || isLead4) continue;

            // Build tryte string: lead '+', then 5 payload trits
            var sb = new StringBuilder(6);
            sb.Append('+');
            foreach (int d in payload) sb.Append(d == 0 ? '-' : (d == 1 ? '0' : '+'));
            string pattern = sb.ToString();

            // Determine Unicode code point (1:1 mapping, cp 0..125)
            int? ucp = cp <= 0x10FFFF ? (int?)cp : null;
            string desc = cp < 128 ? DescribeAscii(cp) : $"Code point {cp}";

            entries.Add(new CodePointEntry(cp, pattern, ucp, desc));
            cp++;

            if (cp > 125) break; // we only need 126 entries
        }

        return entries.AsReadOnly();
    }

    /// <summary>Returns a short description for an ASCII code point.</summary>
    internal static string DescribeAscii(int cp) => cp switch
    {
        0  => "NUL (null)",
        1  => "SOH (start of heading)",
        2  => "STX (start of text)",
        3  => "ETX (end of text)",
        4  => "EOT (end of transmission)",
        5  => "ENQ (enquiry)",
        6  => "ACK (acknowledge)",
        7  => "BEL (bell)",
        8  => "BS (backspace)",
        9  => "HT (horizontal tab)",
        10 => "LF (line feed)",
        11 => "VT (vertical tab)",
        12 => "FF (form feed)",
        13 => "CR (carriage return)",
        14 => "SO (shift out)",
        15 => "SI (shift in)",
        16 => "DLE (data link escape)",
        17 => "DC1 (device control 1)",
        18 => "DC2 (device control 2)",
        19 => "DC3 (device control 3)",
        20 => "DC4 (device control 4)",
        21 => "NAK (negative acknowledge)",
        22 => "SYN (synchronous idle)",
        23 => "ETB (end of transmission block)",
        24 => "CAN (cancel)",
        25 => "EM (end of medium)",
        26 => "SUB (substitute)",
        27 => "ESC (escape)",
        28 => "FS (file separator)",
        29 => "GS (group separator)",
        30 => "RS (record separator)",
        31 => "US (unit separator)",
        32 => "SP (space)",
        127 => "DEL (delete)",
        _ when cp >= 33 && cp <= 126 => $"'{(char)cp}'",
        _ => $"Code point {cp}"
    };
}
