# Ternary Workbench — Claude Code Guide

## Project Purpose

Ternary Workbench is an open-source educational and practical toolkit for balanced ternary computing, developed by Dr. Steven Bos and the Ternary Research Group at the University of South-Eastern Norway (USN). It provides radix conversion, REBEL CPU assemblers/disassemblers, ternary string encoding, and links to MRCS Studio (an EDA tool for ternary circuit design).

## Repository Layout

```
ternary-workbench/
  docs/                          # Editable documentation (single source of truth)
    rebel2-isa.md                # REBEL-2 V2.0 instruction set reference
    rebel2v2-isa.md              # REBEL-2 V2.2 instruction set reference
    rebel6-isa.md                # REBEL-6 instruction set reference
    chart-u8-standard.md         # charT_u8 encoding standard specification
    chartc-u8-standard.md        # charTC_u8 encoding standard specification
    mrcs-studio.md               # MRCS Studio overview and changelog
  TernaryWorkbench/
    TernaryWorkbench.slnx        # .NET solution file
    src/
      TernaryWorkbench.Core/             # Radix conversion library
      TernaryWorkbench.RebelAssembler/   # REBEL-2/2v2/6 assembler/disassembler
      TernaryWorkbench.CharTStringConverter/ # charT_u8 / charTC_u8 codecs
      TernaryWorkbench.Cli/              # CLI tool (dotnet run or twb binary)
      TernaryWorkbench.Web/              # Blazor WebAssembly web app
    tests/
      TernaryWorkbench.Tests/            # Unit tests for core libraries
```

## Key Concepts

- **Balanced ternary**: Three-valued logic using trits (+, 0, −) instead of binary bits. Each trit has value +1, 0, or −1.
- **Tryte**: A group of 6 trits (analogous to a byte).
- **REBEL-2**: A minimal 10-trit, 9-register Harvard-architecture balanced ternary CPU for education (two ISA versions: V2.0 and V2.2).
- **REBEL-6**: REBEL-2's successor — 32-trit, 729-register, with full RV32I binary compatibility for real-world use.
- **charT_u8**: Variable-length balanced ternary character encoding, 1:1 Unicode mapping, no CRC.
- **charTC_u8**: Same structure but with a CRC trit for error detection, 42-character ternary-native code space.
- **MRCS Studio**: External browser-based EDA tool for mixed-radix and ternary circuit design (separate project, linked from the web app).

## Build & Run

All commands run from the repo root.

```bash
# Build the entire solution
dotnet build TernaryWorkbench/TernaryWorkbench.slnx

# Run all unit tests
dotnet test TernaryWorkbench/TernaryWorkbench.slnx

# Run the Blazor web app (dev server with hot reload)
dotnet watch --project TernaryWorkbench/src/TernaryWorkbench.Web

# Run the CLI
dotnet run --project TernaryWorkbench/src/TernaryWorkbench.Cli -- --help
```

## Project Responsibilities

| Project | Responsibility |
|---------|---------------|
| `TernaryWorkbench.Core` | Radix conversion between 15+ numeral systems (binary variants, octal, hex, base64, balanced ternary, base9, base27, decimal) |
| `TernaryWorkbench.RebelAssembler` | Assemble/disassemble REBEL-2 (V2.0), REBEL-2v2 (V2.2), and REBEL-6 assembly to/from ternary machine code |
| `TernaryWorkbench.CharTStringConverter` | Encode/decode UTF-8 text to/from charT_u8 and charTC_u8 ternary strings |
| `TernaryWorkbench.Cli` | Command-line interface exposing all three libraries |
| `TernaryWorkbench.Web` | Blazor WebAssembly SPA with interactive tool UIs; loads ISA reference and encoding specs from `docs/` at runtime via Markdig |
| `TernaryWorkbench.Tests` | Unit tests for Core, RebelAssembler, and CharTStringConverter |

## Tech Stack

- **.NET 10**, C# with nullable reference types and implicit usings
- **Blazor WebAssembly** for the web app (runs entirely in-browser)
- **MudBlazor 9** (Material Design UI components)
- **Markdig 0.40** (Markdown rendering in the web app)
- No database, no server-side state; all data lives in memory or browser storage

## Documentation

ISA references and encoding specifications live in `docs/` as Markdown files. These are the **single source of truth** — edit them directly. The web app fetches and renders them at runtime via the `MarkdownViewer` component ([TernaryWorkbench.Web/Components/MarkdownViewer.razor](TernaryWorkbench/src/TernaryWorkbench.Web/Components/MarkdownViewer.razor)). The MSBuild configuration in `TernaryWorkbench.Web.csproj` copies `docs/*.md` into `wwwroot/docs/` at build time.

## Conventions

- **Trit notation**: `+` = +1, `0` = 0, `-` = −1 (always lowercase minus, never en/em dash in code).
- **ISA mnemonics**: Ternary instructions have `.T` suffix (e.g. `ADD.T`); binary RV32I-compatible instructions in REBEL-6 have no suffix.
- **Opcode notation**: Balanced ternary digits, MST-first (most significant trit at left).
- **No comments in code** unless explaining a non-obvious invariant or workaround.
- **Tests**: Test project targets `TernaryWorkbench.Tests`; run with `dotnet test`.
