# Ternary Workbench

An open-source toolkit for balanced ternary computing — radix conversion, REBEL CPU assembly, ternary string encoding, and circuit design tooling.

## Features

- **Radix Converter** — Convert numbers between 15+ numeral systems including balanced ternary, binary (unsigned, 1's complement, 2's complement), octal, hexadecimal, base-64, base-9, base-27, and decimal.
- **REBEL Assembler / Disassembler** — Assemble and disassemble programs for three REBEL CPU ISA variants:
  - REBEL-2 V2.0 — minimal 10-trit, 9-register Harvard-architecture CPU
  - REBEL-2 V2.2 — extended with multiply, divide, memory access, and more instructions
  - REBEL-6 — 32-trit, 729-register, RV32I-compatible production architecture
- **charT String Converter** — Encode/decode UTF-8 text to/from balanced ternary using the charT_u8 (no CRC) and charTC_u8 (CRC-protected) standards.
- **MRCS Studio** — Links to the browser-based EDA tool for mixed-radix and ternary VLSI circuit design.

## Quick Start

### Web App

```bash
dotnet watch --project TernaryWorkbench/src/TernaryWorkbench.Web
```

Open `https://localhost:5001` in your browser.

### CLI

```bash
# Radix conversion
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- --from dec --to balanced 42

# REBEL-6 assembly
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- rebel6 asm "ADD.T X1, X2, X3"

# charT encoding
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- chart encode "hello"

# Full help
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- --help
```

### Build & Test

```bash
dotnet build TernaryWorkbench/TernaryWorkbench.slnx
dotnet test TernaryWorkbench/TernaryWorkbench.slnx
```

## Documentation

- [REBEL-2 V2.0 ISA Reference](docs/rebel2-isa.md)
- [REBEL-2 V2.2 ISA Reference](docs/rebel2v2-isa.md)
- [REBEL-6 ISA Reference](docs/rebel6-isa.md)
- [charT_u8 Encoding Standard](docs/chart-u8-standard.md)
- [charTC_u8 Encoding Standard](docs/chartc-u8-standard.md)
- [MRCS Studio](docs/mrcs-studio.md)

## Project Structure

| Directory | Description |
|-----------|-------------|
| `TernaryWorkbench/src/TernaryWorkbench.Core` | Radix conversion library |
| `TernaryWorkbench/src/TernaryWorkbench.RebelAssembler` | REBEL assembler/disassembler |
| `TernaryWorkbench/src/TernaryWorkbench.CharTStringConverter` | charT codec library |
| `TernaryWorkbench/src/TernaryWorkbench.Cli` | Command-line tool |
| `TernaryWorkbench/src/TernaryWorkbench.Web` | Blazor WebAssembly web app |
| `docs/` | ISA references and encoding specifications (editable Markdown) |

## License

MIT — Copyright 2024 Steven Bos

## Contributors

Steven Bos, Sondre Bitubekk, Ole Christian Moholth, Halvor Nybø Risto, Henning Gundersen, Vetle Bodahl, Erika Fegri, Anders Minde
