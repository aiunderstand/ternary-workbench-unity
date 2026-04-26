# charT_u8 — Ternary UTF-8 Encoding (no CRC)

## Overview

charT_u8 is a variable-length balanced ternary character encoding with a 1:1 mapping
to Unicode code points. Each symbol is encoded as 1 to 4 trytes (6 trits each),
using leading-trit patterns to identify tryte roles. The encoding is self-synchronizing:
any '+'-leading tryte is always the start of a new symbol; any '-' or '0'-leading tryte
is always a continuation. No backward scan is needed to re-synchronize after an error.

## Tryte Roles

The structural role of each tryte is determined solely by its leading trits:

| Leading trits | Role | Payload trits |
|---------------|------|---------------|
| + (single) | 1-tryte symbol | 5 (126 valid) |
| + - | Lead of 2-tryte seq | 4 |
| + + - | Lead of 3-tryte seq | 3 |
| + + + - | Lead of 4-tryte seq | 2 |
| 0 0 x x x x | Middle continuation | 4 |
| 0 x x x x x | Middle continuation | 5 |
| - x x x x x | Final continuation | 5 |

## Code-Point Ranges

| Form | CP range | Unicode coverage |
|------|----------|-----------------|
| 1-tryte | 0 – 125 | ASCII 0–125 (NUL–'}'). '~'(126) and DEL(127) use 2-tryte. |
| 2-tryte | 126 – 19 808 | Full Basic Latin + Latin-1 Supplement and beyond |
| 3-tryte | 19 809 – 1 614 131 | Covers all Unicode (max U+10FFFF = 1 114 111) |
| 4-tryte | 1 614 132 – 44 660 852 | Ternary extension space, beyond Unicode |

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

### Single-Tryte Encoding Rules

The lead trit is always `+`. The 5 remaining trits encode the code point using restricted groups:

- **Group 1** (CP 0–80): t1=0, t2..t5 free → 81 patterns
- **Group 2** (CP 81–107): t1=+, t2=0, t3..t5 free → 27 patterns
- **Group 3** (CP 108–116): t1=+, t2=+, t3=0, t4..t5 free → 9 patterns
- **Group 4** (CP 117–125): remaining valid patterns → 9 patterns

Reserved prefixes excluded (would be misread as multi-tryte leads):
- Lead2: t1=Minus → 81 patterns reserved
- Lead3: t1=Plus, t2=Minus → 27 patterns reserved
- Lead4: t1=Plus, t2=Plus, t3=Minus → 9 patterns reserved
- Total available: 243 − 81 − 27 − 9 = **126** valid single-tryte patterns
