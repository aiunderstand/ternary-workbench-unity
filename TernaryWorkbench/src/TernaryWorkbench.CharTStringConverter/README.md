# TernaryWorkbench.CharTStringConverter

Encode and decode UTF-8 text to/from balanced ternary using the charT string encoding standards.

## Standards

| Standard | CRC | Code space | 1-tryte symbols |
|----------|-----|-----------|----------------|
| charT_u8 | None | 1:1 Unicode | 126 (ASCII 0–125) |
| charTC_u8 | Yes (per sequence) | Ternary-native | 42 (curated plain-text set) |

See the encoding specifications:
- [charT_u8 Standard](../../../../docs/chart-u8-standard.md)
- [charTC_u8 Standard](../../../../docs/chartc-u8-standard.md)

## Key API

```csharp
using TernaryWorkbench.CharTStringConverter;

// Encode text to charT_u8
string ternary = CharTu8Codec.Encode("hello");

// Decode charT_u8
string text = CharTu8Codec.Decode(ternary);

// Encode to charTC_u8 (with CRC)
string ternaryWithCrc = CharTCu8Codec.Encode("hello");

// Detect encoding of an unknown ternary string
CharTStringEncoding detected = CharTStringEncodingDetector.Detect(ternary);

// Access the 1-tryte code-point tables
IReadOnlyList<CodePointEntry> table = CharTu8StandardTable.SingleTryteTable;  // 126 entries
IReadOnlyList<CodePointEntry> tableC = CharTCu8StandardTable.SingleTryteTable; // 42 entries
```

## CSV Serialization

`CharTStringCsvSerializer` serializes encode/decode history to/from CSV.
