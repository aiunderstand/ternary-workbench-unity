# charTC_u8 — Ternary UTF-8 Encoding with CRC

## Overview

charTC_u8 is a variable-length balanced ternary character encoding with a CRC trit
embedded in every multi-symbol sequence, enabling single-trit error detection.
It uses a ternary-native code space (not 1:1 Unicode) optimised so that the 42
most-used plain-text characters encode in a single tryte.

## CRC Mechanism

The last trit (position 5) of the lead tryte in every sequence is a CRC trit.
It is set so that the balanced sum of ALL trits in the complete sequence equals zero:

```
CRC trit = (−(sum of all other trits)) mod₃, mapped to {−1, 0, +1}
```

For 1-tryte symbols the sum of all 6 trits including the CRC must equal zero.
Any single-trit error in any tryte of the sequence changes the sum, allowing
immediate detection. The CRC trit never occupies a structural marker position
and therefore does not disturb self-synchronization.

## Tryte Roles

| Leading trits | Role | Payload trits (excl. CRC) |
|---------------|------|--------------------------|
| + (single) | 1-tryte symbol | 4 (42 valid) |
| + - | Lead of 2-tryte seq | 3 |
| + + - | Lead of 3-tryte seq | 2 |
| + + + - | Lead of 4-tryte seq | 1 |
| 0 0 x x x x | Middle continuation | 4 |
| 0 x x x x x | Middle continuation | 5 |
| - x x x x x | Final continuation | 5 |

## Code-Point Ranges

Multi-tryte forms address Unicode code points directly.
The 2-tryte form carries one fewer payload trit in the lead (stolen by CRC) than charT_u8,
so full Unicode coverage requires the 4-tryte form for high code points.

| Form | Unicode CP range | Notable coverage |
|------|-----------------|-----------------|
| 1-tryte | custom 42-char table | NUL, TAB, LF, CR, SPC, a–z, 0–9, '.' |
| 2-tryte | 0 – 6 560 | Full ASCII + Latin Extended A/B |
| 3-tryte | 6 561 – 538 001 | Greek, Cyrillic, CJK Unified Ideographs (partial) |
| 4-tryte | 538 002 – 14 886 908 | Remaining Unicode + ternary extension space |

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

### Single-Tryte Encoding Rules

Valid 1-tryte patterns: enumerate all 3^4 = 81 payload combinations (trits 1..4), skipping reserved leads.
81 − 27 (Lead2: t1=Minus) − 9 (Lead3: t1=Plus, t2=Minus) − 3 (Lead4: t1=Plus, t2=Plus, t3=Minus) = **42** valid slots.

Trit 0 is always `+` (sequence-start marker).
Payload trits 1–4 encode the code-point offset within the valid 81 slots.
Trit 5 is the CRC (sum of all 6 trits of the single tryte = 0).

### 42-Character Mapping (ordered by charTC_u8 code point)

| CP | Description | Unicode |
|----|-------------|---------|
| 0 | NUL (null) | U+0000 |
| 1 | HT (horizontal tab) | U+0009 |
| 2 | LF (line feed) | U+000A |
| 3 | CR (carriage return) | U+000D |
| 4 | SP (space) | U+0020 |
| 5 | 'a' | U+0061 |
| 6 | 'b' | U+0062 |
| 7 | 'c' | U+0063 |
| 8 | 'd' | U+0064 |
| 9 | 'e' | U+0065 |
| 10 | 'f' | U+0066 |
| 11 | 'g' | U+0067 |
| 12 | 'h' | U+0068 |
| 13 | 'i' | U+0069 |
| 14 | 'j' | U+006A |
| 15 | 'k' | U+006B |
| 16 | 'l' | U+006C |
| 17 | 'm' | U+006D |
| 18 | 'n' | U+006E |
| 19 | 'o' | U+006F |
| 20 | 'p' | U+0070 |
| 21 | 'q' | U+0071 |
| 22 | 'r' | U+0072 |
| 23 | 's' | U+0073 |
| 24 | 't' | U+0074 |
| 25 | 'u' | U+0075 |
| 26 | 'v' | U+0076 |
| 27 | 'w' | U+0077 |
| 28 | 'x' | U+0078 |
| 29 | 'y' | U+0079 |
| 30 | 'z' | U+007A |
| 31 | '0' | U+0030 |
| 32 | '1' | U+0031 |
| 33 | '2' | U+0032 |
| 34 | '3' | U+0033 |
| 35 | '4' | U+0034 |
| 36 | '5' | U+0035 |
| 37 | '6' | U+0036 |
| 38 | '7' | U+0037 |
| 39 | '8' | U+0038 |
| 40 | '9' | U+0039 |
| 41 | '.' | U+002E |
