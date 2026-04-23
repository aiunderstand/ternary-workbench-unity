using TernaryWorkbench.Rebel2Assembler.Assembly;
using TernaryWorkbench.Rebel2Assembler.Assembly.Models;

namespace TernaryWorkbench.Rebel2Assembler;

/// <summary>
/// Public facade for assembling and disassembling REBEL-2v2 (ISA v0.5) programs.
/// </summary>
/// <remarks>
/// Uses the same encoding infrastructure as <see cref="Rebel2Assembler"/> but with
/// the updated ISA v0.5 instruction table.  Key differences from v1:
/// <list type="bullet">
///   <item>NOP.T is now <c>0000000000</c> (opcode "00") instead of <c>-000000000</c>.</item>
///   <item>MUL.T has moved to opcode "--" (func "-0"); opcode "0-" now hosts MAJV.T.</item>
///   <item>ADDI.T, LI.T, MV.T, STI.T and MinMax family have new opcodes.</item>
///   <item>New instructions: MULH, DIV, REM, MOD, LD2, ST2, NTI, PTI, MTI, CycleUp, SWAP,
///         MAJV, MINI, MAXI, CMPWI, CMPTI, BNE, KIMP, FENCE, WFI, IRET, EBREAK, ECALL.</item>
/// </list>
/// </remarks>
public static class Rebel2v2Assembler
{
    // -------------------------------------------------------------------------
    // Assembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assemble a block of REBEL-2v2 assembly (1–9 instructions) into a list of
    /// <see cref="AssembledInstruction"/> records.  Labels are resolved within the block.
    /// </summary>
    public static IReadOnlyList<AssembledInstruction> AssembleInstructions(string assembly) =>
        PageAssembler.AssemblePage(assembly, padPage: false, InstructionSet2v2.DefaultPaddingInstruction, InstructionSet2v2.Patterns);

    /// <summary>
    /// Translate a single assembly instruction string into a 10-trit machine code string.
    /// </summary>
    public static string Translate(string instruction) =>
        InstructionEncoder.Translate(instruction, InstructionSet2v2.Patterns);

    // -------------------------------------------------------------------------
    // Disassembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Disassemble a single 10-trit machine code string into a mnemonic+operands string.
    /// </summary>
    public static string Disassemble(string machineCode) =>
        InstructionDisassembler.Disassemble(machineCode, InstructionSet2v2.Patterns);

    /// <summary>
    /// Disassemble a block of 1–9 10-trit machine code strings (one per element)
    /// into a list of mnemonic+operands strings.
    /// </summary>
    public static IReadOnlyList<string> DisassemblePage(IEnumerable<string> machineCodes) =>
        [.. machineCodes.Select(mc => InstructionDisassembler.Disassemble(mc, InstructionSet2v2.Patterns))];
}
