namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Specification tables and metadata for the charTC_u8 encoding standard
/// (with CRC trit, ternary-native 42-symbol 1-tryte table).
/// </summary>
public static class CharTCu8StandardTable
{
    // -------------------------------------------------------------------------
    // Multi-tryte Unicode code-point ranges
    // -------------------------------------------------------------------------
    // charTC_u8 multi-tryte forms encode Unicode code points directly.
    // The minimal-encoding rule forbids encoding a CP that is in the
    // single-tryte table using a multi-tryte form.
    //
    // Payload capacities (CRC steals 1 trit from every lead tryte):
    //   2-tryte: lead[2..4]=3 + final[1..5]=5 → 8 trits → 3^8  =     6 561 CPs
    //   3-tryte: lead[3..4]=2 + mid[1..5]=5  + final[1..5]=5 → 12 → 3^12 = 531 441 CPs
    //   4-tryte: lead[4]=1 + mid2[2..5]=4 + mid1[1..5]=5 + final[1..5]=5 → 15 → 3^15 = 14 348 907 CPs

    /// <summary>Number of single-tryte code points in charTC_u8.</summary>
    public const int SingleTryteCount = 42;

    // Multi-tryte forms address Unicode code points directly (offset = unicode_cp).
    // 2-tryte range: unicode CPs 0 – 6 560  (any not also in single-tryte table)
    // 3-tryte range: unicode CPs 6 561 – 538 001
    // 4-tryte range: unicode CPs 538 002 – 14 886 908

    /// <summary>Maximum Unicode CP encodable in a 2-tryte sequence (3^8 − 1).</summary>
    public const int MaxCp2Tryte = 6_560;

    /// <summary>Minimum Unicode CP for a 3-tryte sequence.</summary>
    public const int MinCp3Tryte = 6_561;

    /// <summary>Maximum Unicode CP for a 3-tryte sequence. (6561 + 3^12 − 1)</summary>
    public const int MaxCp3Tryte = 6_560 + 531_441; // 538 001

    /// <summary>Minimum Unicode CP for a 4-tryte sequence.</summary>
    public const int MinCp4Tryte = 538_002;

    /// <summary>Maximum Unicode CP for a 4-tryte sequence. (538002 + 3^15 − 1)</summary>
    public const int MaxCp4Tryte = 538_001 + 14_348_907; // 14 886 908

    // -------------------------------------------------------------------------
    // CRC details
    // -------------------------------------------------------------------------

    /// <summary>
    /// The CRC trit occupies the last (index 5) trit of the lead tryte only.
    /// Its value is chosen so that the balanced sum of ALL trits in the full
    /// encoded sequence equals zero.
    /// </summary>
    public const int CrcTritIndex = 5;

    // -------------------------------------------------------------------------
    // 1-tryte table  (42 entries, ternary-native code space)
    // -------------------------------------------------------------------------

    /// <summary>
    /// The 42-entry single-tryte code-point table for charTC_u8.
    ///
    /// The 42 slots are curated to hold the most useful ASCII characters
    /// for plain-text use: 4 control codes, space, a–z (26), 0–9 (10), '.'.
    ///
    /// CRC: each tryte has a CRC trit in position 5 so that the balanced sum
    /// of all 6 trits equals zero (single-tryte sequences have no continuation
    /// trits, so sum of the 6 trits of the single tryte itself = 0).
    ///
    /// Trit 0 is always '+' (sequence-start marker).
    /// Payload trits 1–4 encode the code-point offset within the valid 81 slots.
    /// Trit 5 is the CRC.
    /// Reserved prefixes excluded:
    ///   Lead2: t1=Minus  (27 patterns)
    ///   Lead3: t1=Plus, t2=Minus  (9 patterns)
    ///   Lead4: t1=Plus, t2=Plus, t3=Minus  (3 patterns)
    ///   → 81 − 39 = 42 valid 1-tryte slots.
    /// </summary>
    public static IReadOnlyList<CodePointEntry> SingleTryteTable { get; } = BuildTable();

    // -------------------------------------------------------------------------
    // Spec text
    // -------------------------------------------------------------------------

    /// <summary>
    /// Multi-paragraph specification text for the charTC_u8 standard,
    /// suitable for display in a collapsible panel.
    /// </summary>
    public static string SpecificationText { get; } = """
        # charTC_u8 — Ternary UTF-8 Encoding with CRC

        ## Overview
        charTC_u8 is a variable-length balanced ternary character encoding with a CRC trit
        embedded in every multi-symbol sequence, enabling single-trit error detection.
        It uses a ternary-native code space (not 1:1 Unicode) optimised so that the 42
        most-used plain-text characters encode in a single tryte.

        ## CRC Mechanism
        The last trit (position 5) of the lead tryte in every sequence is a CRC trit.
        It is set so that the balanced sum of ALL trits in the complete sequence equals zero:

            CRC trit = (−(sum of all other trits)) mod₃, mapped to {−1, 0, +1}

        For 1-tryte symbols the sum of all 6 trits including the CRC must equal zero.
        Any single-trit error in any tryte of the sequence changes the sum, allowing
        immediate detection. The CRC trit never occupies a structural marker position
        and therefore does not disturb self-synchronization.

        ## Tryte Roles (same as charT_u8)
        | Leading trits  | Role                  | Payload trits (excl. CRC) |
        |----------------|-----------------------|---------------------------|
        | + (single)     | 1-tryte symbol        | 4 (42 valid)              |
        | + -            | Lead of 2-tryte seq   | 3                         |
        | + + -          | Lead of 3-tryte seq   | 2                         |
        | + + + -        | Lead of 4-tryte seq   | 1                         |
        | 0 0 x x x x   | Middle continuation   | 4                         |
        | 0 x x x x x   | Middle continuation   | 5                         |
        | - x x x x x   | Final continuation    | 5                         |

        ## Code-Point Ranges
        Multi-tryte forms address Unicode code points directly.
        The 2-tryte form carries one fewer payload trit in the lead (stolen by CRC) than charT_u8,
        so full Unicode coverage requires the 4-tryte form for high code points.

        | Form    | Unicode CP range      | Notable coverage                                    |
        |---------|-----------------------|-----------------------------------------------------|
        | 1-tryte | custom 42-char table  | NUL, TAB, LF, CR, SPC, a–z, 0–9, '.'              |
        | 2-tryte | 0 – 6 560             | Full ASCII + Latin Extended A/B                     |
        | 3-tryte | 6 561 – 538 001       | Greek, Cyrillic, CJK Unified Ideographs (partial)  |
        | 4-tryte | 538 002 – 14 886 908  | Remaining Unicode + ternary extension space         |

        Unicode maximum U+10FFFF = 1 114 111, which falls in the 4-tryte range.
        Contrast with charT_u8 where all Unicode fits in 3-tryte; the CRC trit
        costs one payload trit per lead tryte.

        ## Self-Synchronization
        Identical to charT_u8: scan forward for any '+'-leading tryte to re-synchronize.

        ## Minimal Encoding Rule
        Same as charT_u8: a code point MUST use its shortest valid form.

        ## 1-Tryte Symbol Set (42 characters)
        The 42 single-tryte code points cover the most frequently needed plain-text characters:
        4 control codes (NUL, TAB, LF, CR), space, lowercase a–z, digits 0–9, and period '.'.
        Upper-case letters, punctuation, and all Unicode characters beyond this set require
        2-tryte or higher encoding with CRC.
        """;

    // -------------------------------------------------------------------------
    // Internal builder
    // -------------------------------------------------------------------------

    private static IReadOnlyList<CodePointEntry> BuildTable()
    {
        // Curated 42-entry mapping: CP → (Unicode CP, description)
        // Order: NUL(0), TAB(9), LF(10), CR(13), SPC(32), a-z, 0-9, '.'
        var unicodeMappings = new (int UCP, string Desc)[]
        {
            (0,  "NUL (null)"),
            (9,  "HT (horizontal tab)"),
            (10, "LF (line feed)"),
            (13, "CR (carriage return)"),
            (32, "SP (space)"),
            // a–z
            (97,  "'a'"), (98,  "'b'"), (99,  "'c'"), (100, "'d'"), (101, "'e'"),
            (102, "'f'"), (103, "'g'"), (104, "'h'"), (105, "'i'"), (106, "'j'"),
            (107, "'k'"), (108, "'l'"), (109, "'m'"), (110, "'n'"), (111, "'o'"),
            (112, "'p'"), (113, "'q'"), (114, "'r'"), (115, "'s'"), (116, "'t'"),
            (117, "'u'"), (118, "'v'"), (119, "'w'"), (120, "'x'"), (121, "'y'"),
            (122, "'z'"),
            // 0–9
            (48, "'0'"), (49, "'1'"), (50, "'2'"), (51, "'3'"), (52, "'4'"),
            (53, "'5'"), (54, "'6'"), (55, "'7'"), (56, "'8'"), (57, "'9'"),
            // '.'
            (46, "'.'"),
        };

        // The 42 valid 1-tryte patterns for charTC_u8: enumerate all 3^4 = 81
        // 4-trit payload combinations (trits 1..4), skipping reserved leads,
        // then assign CRC trit at position 5.
        var validPatterns = new List<int[]>(42); // each is [t1,t2,t3,t4] as positional digits
        for (int raw = 0; raw < 81; raw++) // 3^4
        {
            int r = raw;
            var p = new int[4];
            for (int i = 3; i >= 0; i--) { p[i] = r % 3; r /= 3; }

            bool lead2 = p[0] == 0;                          // t1=Minus
            bool lead3 = p[0] == 2 && p[1] == 0;             // t1=Plus, t2=Minus
            bool lead4 = p[0] == 2 && p[1] == 2 && p[2] == 0; // t1=Plus, t2=Plus, t3=Minus

            if (!lead2 && !lead3 && !lead4)
                validPatterns.Add(p);
        }
        // validPatterns.Count should be exactly 42

        var entries = new List<CodePointEntry>(42);
        for (int i = 0; i < unicodeMappings.Length; i++)
        {
            var (ucp, desc) = unicodeMappings[i];
            var p = validPatterns[i]; // p = [t1,t2,t3,t4] positional

            // Compute CRC: balanced sum of t0..t4 then CRC so total = 0
            // t0 = Plus = +1
            int partialSum = 1; // t0 = Plus
            for (int j = 0; j < 4; j++)
                partialSum += PositionalToBalanced(p[j]);

            // crcBalanced such that partialSum + crcBalanced ≡ 0 (mod 3), value in {-1,0,+1}
            int crcBalanced = ComputeCrcBalanced(partialSum);
            int crcDigit = BalancedToPositional(crcBalanced); // 0,1,2

            char d(int v) => v == 0 ? '-' : (v == 1 ? '0' : '+');
            string pattern = $"+{d(p[0])}{d(p[1])}{d(p[2])}{d(p[3])}{d(crcDigit)}";

            entries.Add(new CodePointEntry(i, pattern, ucp, desc));
        }

        return entries.AsReadOnly();
    }

    private static int PositionalToBalanced(int d) => d - 1; // 0→-1, 1→0, 2→+1

    /// <summary>Converts a balanced trit value {−1, 0, +1} to a positional digit {0, 1, 2}.</summary>
    internal static int BalancedToPositional(int b) => b + 1; // -1→0, 0→1, +1→2

    /// <summary>
    /// Returns the balanced trit value c ∈ {−1, 0, +1} such that
    /// (sum + c) ≡ 0 (mod 3), where sum is the balanced sum of the other trits.
    ///
    /// Derivation:
    ///   r = (−sum) mod 3  (result in {0,1,2})
    ///   r=0 → c=0  (Zero):  sum≡0, c=0,  total≡0 ✓
    ///   r=1 → c=+1 (Plus):  sum≡−1≡2, c=+1, total≡3≡0 ✓
    ///   r=2 → c=−1 (Minus): sum≡1, c=−1, total≡0 ✓
    /// </summary>
    public static int ComputeCrcBalanced(int partialSum)
    {
        int r = ((-partialSum) % 3 + 3) % 3; // guaranteed non-negative
        // r=0→0(Zero), r=1→+1(Plus), r=2→−1(Minus)
        return new[] { 0, 1, -1 }[r];
    }
}
