# TernaryWorkbench.Core

Radix conversion library supporting 15+ numeral systems including balanced ternary.

## Supported Radix Types

| Alias | Radix | Description |
|-------|-------|-------------|
| `bin` / `base2` | Base2Unsigned | Unsigned binary |
| `bin1c` / `base2c1` | Base2Signed1C | Binary 1's complement |
| `bin2c` / `base2c` | Base2Signed2C | Binary 2's complement |
| `oct` / `base8` | Base8Unsigned | Unsigned octal |
| `hex` / `base16` | Base16Unsigned | Unsigned hexadecimal |
| `base64` | Base64Rfc4648 | RFC 4648 Base-64 |
| `ter` / `base3` | Base3Unbalanced | Unbalanced ternary |
| `ter2c` / `base3c2` | Base3Signed2C | Ternary 2's complement |
| `ter3c` / `base3c3` | Base3Signed3C | Ternary 3's complement |
| `balanced` / `base3b` | Base3Balanced | Balanced ternary (+, 0, −) |
| `bsdpnx` / `base3pnx` | Base3BsdPnx | BSD PNX balanced ternary |
| `base9` | Base9Unbalanced | Unsigned base-9 |
| `base27` | Base27Unbalanced | Unsigned base-27 |
| `dec` / `base10` | Base10 | Decimal |

## Key API

```csharp
using TernaryWorkbench.Core;

// Convert decimal 42 to balanced ternary
string result = RadixConverter.Convert("42", Radix.Base10, Radix.Base3Balanced);
// result → "+---" (= +27 + 0 - 9 + 0 - 1 - 1 - ... no, verify with the tool)

// Convert with options (LSD-first, binary-coded ternary)
var options = new OutputOptions { LsdFirst = false, BinaryCoded = false };
string result2 = RadixConverter.Convert("42", Radix.Base10, Radix.Base3Balanced, options);
```

## CSV Serialization

`ConversionCsvSerializer` serializes and deserializes conversion history (`ConversionRecord[]`) to and from CSV for import/export in the web UI and CLI.
