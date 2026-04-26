# TernaryWorkbench.RebelAssembler

Assembler and disassembler for the REBEL balanced ternary CPU instruction set architectures.

## ISA Variants

| Variant | Word width | Registers | Architecture |
|---------|-----------|-----------|-------------|
| REBEL-2 V2.0 | 10 trits | 9 (X-4 … X4) | Harvard, register-oriented, educational |
| REBEL-2 V2.2 | 10 trits | 9 (X-4 … X4) | Same as V2.0, extended instruction set |
| REBEL-6 | 32 trits | 729 (X-364 … X364) | Harvard, RV32I binary compatible |

See the ISA reference documentation:
- [REBEL-2 V2.0 ISA Reference](../../../../docs/rebel2-isa.md)
- [REBEL-2 V2.2 ISA Reference](../../../../docs/rebel2v2-isa.md)
- [REBEL-6 ISA Reference](../../../../docs/rebel6-isa.md)

## Key API

```csharp
using TernaryWorkbench.RebelAssembler;

// REBEL-2 V2.0 assembly
var instructions = Rebel2Assembler.AssembleInstructions("ADD.T X1, X2, X3\nNOP.T");
// instructions[0].Address, .Assembly, .MachineCode

// REBEL-2 V2.0 disassembly
string mnemonic = Rebel2Assembler.Disassemble("--+-+00+00");

// REBEL-2 V2.2
var instructions2v2 = Rebel2v2Assembler.AssembleInstructions("MUL.T X1, X2, X3");
string mnemonic2v2 = Rebel2v2Assembler.Disassemble("--0000--00");

// REBEL-6
var instructions6 = Rebel6Assembler.AssembleInstructions("ADD.T X1, X2, X3\nADDI X4, X5, 10");
string mnemonic6 = Rebel6Assembler.Disassemble("0000+-0000+000000+000000000----++--");
```

## CSV Serialization

`AssemblyCsvSerializer` serializes `AssemblyRecord[]` to/from CSV for history import/export.
