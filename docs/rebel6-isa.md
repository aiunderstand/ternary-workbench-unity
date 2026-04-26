# REBEL-6 Instruction Set Reference

## Overview & Comparison with REBEL-2

REBEL-6 is the successor to REBEL-2, designed for real-world applications rather than education.
It extends REBEL-2 from a minimal 10-trit, 9-register ISA to a full 32-trit, 729-register architecture
with direct RV32I binary compatibility. The **.t** suffix marks ternary instructions;
binary (RV32I-compatible) instructions have no suffix. PC increments by 1 per instruction slot.

| Property | REBEL-2 | **REBEL-6** |
|----------|---------|------------|
| Radix | Balanced ternary | **Balanced ternary** |
| Instruction width | 10 trits | **32 trits** |
| Instruction count | 23 + 3 pseudo (9 opcode groups) | **36 ternary + 27 binary + 2 pseudo = 65** |
| Register count | 9 (X-4 … X4) | **729 (X-364 … X364; X0 = zero)** |
| Register width | 2 trits | **24 trits** |
| Operand count | 2–4 | **2–4** |
| PC jump/instr. | 1 | **1** |
| Binary compat. | None | **Full RV32I (L-type, hardware flag)** |
| Formats | R, I, D | **R, I, B, D, X, G, Y, L** |
| Primary use | Education | **Real-world applications** |

## RV32I Binary Compatibility (L-type)

A hardware flag enables direct execution of existing RV32I 32-bit binaries — no recompilation needed.
Binary is a strict subset of ternary: each 32-bit RV32I instruction fits exactly in 32 trits by
mapping binary 0 → trit **−** and binary 1 → trit **+**. A hardware binary-ternary ALU
and instruction translator handle the conversion transparently. The `xx-0` opcode group
provides a software-accessible RV32I instruction space for explicit binary-mode code.

## Instruction Formats

Fields shown MST-first. 4-trit opcodes: last trit = 0. 2-trit opcodes (G/Y): last trit ≠ 0.

| Format | Layout (MST → LST) | Examples |
|--------|--------------------|---------|
| **R** | rs1[6] \| rs2[6] \| rd1[6] \| rd2[6] \| func[4] \| opcode[4] | ADD.T, CMP.T, STI.T, ADD, SLL |
| **I** | rs1[6] \| imm[11:6][6] \| rd1[6] \| imm[5:0][6] \| func[4] \| opcode[4] | ADDI.T, LW.T, JALR.T, ADDI, LW |
| **B** | rs1[6] \| rs2[6] \| imm[11:0][12] \| func[4] \| opcode[4] | BEQ.T, SW.T, BLT, SW, SB |
| **G** | imm[23:12][12] \| rd1[6] \| imm[11:0][12] \| opc[2] | LWA.T, LI.T, JAL.T, AIPC.T |
| **X** | imm1[5:0][6] \| imm2[5:0][6] \| rd1[6] \| rd2[6] \| func[4] \| opcode[4] | LI2.T |
| **D** | rs1[6] \| rs2[6] \| rd1[6] \| rs3[6] \| func[4] \| opcode[4] | MAJV.T, MINV.T |
| **Y** | rs1[6] \| imm[23:0][24] \| opc[2] | SWA.T |
| **L** | RV32I 32-bit instruction format (binary compatibility, requires hardware flag) | native RV32I passthrough |

## Mnemonics

**Opcode groups (by last 2 trits):** `xx00` = Base Ternary (729 slots); `xx-0` = Base Binary / RV32I (729 slots); `xx+0` = Extensions (729 slots, reserved).
The upper 2 trits encode the instruction category — same in both ternary and binary:
`00`=I-type ALU, `0-`=Branch, `0+`=Store, `--`=R-type ALU, `-+`=I-type Load, `+-`=D/Control, `+0`=X/Imm, `++`=System.

**Func:** upper 2 trits always `00`; lower 2 trits (LST) shown in table discriminate the instruction.

**2-trit long-immediate (last trit ≠ 0):** `++` LWA.T, `0+` LI.T, `-+` SWA.T, `+-` JAL.T, `0-` AIPC.T; `--` *reserved*.

**Comments:** `#`, `;`, `$`, `//` strip to end-of-line.

| Mnemonic | Format | Opcode | Func | Operands | Category | Description |
|----------|--------|--------|------|----------|----------|-------------|
| ADD.T | R | --00 | -- | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 + rs2 |
| SUB.T | R | --00 | -0 | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 − rs2 |
| SL.T | R | --00 | -+ | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 << rs2 |
| SR.T | R | --00 | 0- | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 >> rs2 |
| SLT.T | R | --00 | 00 | rd1, rs1, rs2 | Ternary ALU | rd1 = (rs1 < rs2) ? +1 : 0 |
| OR.T | R | --00 | 0+ | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 OR rs2 |
| XOR.T | R | --00 | +- | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 XOR rs2 |
| AND.T | R | --00 | +0 | rd1, rs1, rs2 | Ternary ALU | rd1 = rs1 AND rs2 |
| CMP.T | R | -000 | -- | rd1, rs1, rs2 | Ternary ALU | rd1 = three-way compare: +1, 0, or -1 |
| STI.T | R | -000 | -0 | rd1, rs1 | Ternary ALU | rd1 = −rs1 (standard ternary inversion) |
| ADDI.T | I | 0000 | 00 | rd1, rs1, imm | Ternary ALU | rd1 = rs1 + imm |
| SLI.T | I | 0000 | -- | rd1, rs1, imm | Ternary ALU | rd1 = rs1 << shamt |
| SRI.T | I | 0000 | -0 | rd1, rs1, imm | Ternary ALU | rd1 = rs1 >> shamt |
| SLTI.T | I | 0000 | -+ | rd1, rs1, imm | Ternary ALU | rd1 = (rs1 < imm) ? +1 : 0 |
| ORI.T | I | 0000 | 0- | rd1, rs1, imm | Ternary ALU | rd1 = rs1 OR imm |
| XORI.T | I | 0000 | 0+ | rd1, rs1, imm | Ternary ALU | rd1 = rs1 XOR imm |
| ANDI.T | I | 0000 | +- | rd1, rs1, imm | Ternary ALU | rd1 = rs1 AND imm |
| LW.T | I | -+00 | -- | rd1, rs1, imm | Ternary Load | rd1 = mem[rs1 + imm] (word) |
| LH.T | I | -+00 | -0 | rd1, rs1, imm | Ternary Load | rd1 = mem[rs1 + imm] (halfword) |
| LT.T | I | -+00 | -+ | rd1, rs1, imm | Ternary Load | rd1 = mem[rs1 + imm] (trit-word) |
| JALR.T | I | -+00 | 0- | rd1, rs1, imm | Ternary Control | rd1 = PC+1; PC = rs1 + imm |
| BEQ.T | B | 0-00 | -- | rs1, rs2, offset | Ternary Branch | branch if rs1 == rs2 |
| BNE.T | B | 0-00 | -0 | rs1, rs2, offset | Ternary Branch | branch if rs1 ≠ rs2 |
| BLT.T | B | 0-00 | -+ | rs1, rs2, offset | Ternary Branch | branch if rs1 < rs2 |
| BGE.T | B | 0-00 | 0- | rs1, rs2, offset | Ternary Branch | branch if rs1 ≥ rs2 |
| SW.T | B | 0+00 | -- | rs1, rs2, offset | Ternary Store | mem[rs1 + offset] = rs2 (word) |
| SH.T | B | 0+00 | -0 | rs1, rs2, offset | Ternary Store | mem[rs1 + offset] = rs2 (halfword) |
| ST.T | B | 0+00 | -+ | rs1, rs2, offset | Ternary Store | mem[rs1 + offset] = rs2 (trit-word) |
| MAJV.T | D | +-00 | -- | rd1, rs1, rs2, rs3 | Ternary ALU | rd1 = majority(rs1, rs2, rs3) |
| MINV.T | D | +-00 | -0 | rd1, rs1, rs2, rs3 | Ternary ALU | rd1 = minority(rs1, rs2, rs3) |
| LI2.T | X | +000 | -- | rd1, rd2, imm1, imm2 | Ternary ALU | rd1 = imm1;  rd2 = imm2 |
| NOP.T | I | 0000 | 00 | | Pseudo | no-op (all-zero 32 trits = ADDI.T X0, X0, 0) |
| MV.T | I | 0000 | 00 | rd1, rs1 | Pseudo | rd1 = rs1 (ADDI.T rd1, rs1, 0) |
| LWA.T | G | ++ | — | rd1, imm24 | Ternary Load | rd1 = mem[imm24] (absolute word load) |
| LI.T | G | 0+ | — | rd1, imm24 | Ternary ALU | rd1 = imm24 (24-trit load immediate) |
| SWA.T | Y | -+ | — | rs1, imm24 | Ternary Store | mem[imm24] = rs1 (absolute word store) |
| JAL.T | G | +- | — | rd1, imm24 | Ternary Control | rd1 = PC+1; PC = PC + imm24 |
| AIPC.T | G | 0- | — | rd1, imm24 | Ternary Control | rd1 = PC + imm24 |
| ADD | R | ---0 | -- | rd1, rs1, rs2 | Binary ALU | rd1 = rs1 + rs2 |
| SUB | R | ---0 | -0 | rd1, rs1, rs2 | Binary ALU | rd1 = rs1 − rs2 |
| SLL | R | ---0 | -+ | rd1, rs1, rs2 | Binary ALU | rd1 = rs1 << rs2 |
| SRL | R | ---0 | 0- | rd1, rs1, rs2 | Binary ALU | logical shift right |
| SRA | R | ---0 | 00 | rd1, rs1, rs2 | Binary ALU | arithmetic shift right |
| SLTU | R | ---0 | 0+ | rd1, rs1, rs2 | Binary ALU | rd1 = (rs1 <u rs2) ? 1 : 0 |
| OR | R | ---0 | +- | rd1, rs1, rs2 | Binary ALU | bitwise OR |
| XOR | R | ---0 | +0 | rd1, rs1, rs2 | Binary ALU | bitwise XOR |
| AND | R | ---0 | ++ | rd1, rs1, rs2 | Binary ALU | bitwise AND |
| ADDI | I | 00-0 | -- | rd1, rs1, imm | Binary ALU | rd1 = rs1 + imm |
| SLLI | I | 00-0 | -0 | rd1, rs1, imm | Binary ALU | logical left shift immediate |
| SRLI | I | 00-0 | -+ | rd1, rs1, imm | Binary ALU | logical right shift immediate |
| SRAI | I | 00-0 | 0- | rd1, rs1, imm | Binary ALU | arithmetic right shift immediate |
| SLTIU | I | 00-0 | 00 | rd1, rs1, imm | Binary ALU | rd1 = (rs1 <u imm) ? 1 : 0 |
| ORI | I | 00-0 | 0+ | rd1, rs1, imm | Binary ALU | bitwise OR immediate |
| XORI | I | 00-0 | +- | rd1, rs1, imm | Binary ALU | bitwise XOR immediate |
| ANDI | I | 00-0 | +0 | rd1, rs1, imm | Binary ALU | bitwise AND immediate |
| LW | I | -+-0 | -- | rd1, rs1, imm | Binary Load | load word |
| LH | I | -+-0 | -0 | rd1, rs1, imm | Binary Load | load halfword signed |
| LB | I | -+-0 | -+ | rd1, rs1, imm | Binary Load | load byte signed |
| LHU | I | -+-0 | 0- | rd1, rs1, imm | Binary Load | load halfword unsigned |
| LBU | I | -+-0 | 00 | rd1, rs1, imm | Binary Load | load byte unsigned |
| BLTU | B | 0--0 | 0+ | rs1, rs2, offset | Binary Branch | branch if rs1 <u rs2 |
| BGEU | B | 0--0 | +- | rs1, rs2, offset | Binary Branch | branch if rs1 ≥u rs2 |
| SW | B | 0+-0 | -- | rs1, rs2, offset | Binary Store | store word |
| SH | B | 0+-0 | -0 | rs1, rs2, offset | Binary Store | store halfword |
| SB | B | 0+-0 | -+ | rs1, rs2, offset | Binary Store | store byte |
