# TernaryWorkbench.Cli

Command-line interface for all Ternary Workbench tools.

## Usage

```bash
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- <command> [options]
```

Or after publishing, use the `twb` binary directly.

## Radix Conversion

```bash
# Convert decimal 42 to balanced ternary
twb --from dec --to balanced 42

# Convert hexadecimal to binary
twb --from hex --to bin FF

# Convert balanced ternary to decimal
twb --from balanced --to dec "+---"

# LSD-first output
twb --from dec --to balanced 42 --lsd-first

# Binary-coded ternary output
twb --from dec --to balanced 42 --bct
```

Supported radix aliases: `bin`, `bin1c`, `bin2c`, `oct`, `hex`, `base64`, `ter`, `ter2c`, `ter3c`, `balanced`, `bsdpnx`, `base9`, `base27`, `dec`.

## REBEL Assembler / Disassembler

```bash
# Assemble REBEL-2 V2.0
twb rebel2 asm "ADD.T X1, X2, X3"
twb rebel2 asm "NOP.T"

# Disassemble REBEL-2 V2.0
twb rebel2 dis "--+-+00+00"

# REBEL-2 V2.2
twb rebel2v2 asm "MUL.T X1, X2, X3"
twb rebel2v2 dis "--0000--00"

# REBEL-6
twb rebel6 asm "ADD.T X1, X2, X3"
twb rebel6 dis "0000+-0000+000000+000000000----++--"
```

## charT String Converter

```bash
# Encode text to charT (auto-detects best standard)
twb chart encode "hello world"

# Decode charT_u8
twb chart decode-u8 "+0000++000-+0000++000-"

# Decode charTC_u8
twb chart decode-tc "+0000++000-+0000++000-"

# Auto-detect encoding and decode
twb chart decode "+0000++000-+0000++000-"

# Detect encoding of a ternary string
twb chart detect "+0000++000-+0000++000-"
```

## Help

```bash
twb --help
twb -h
```
