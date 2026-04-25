using TernaryWorkbench.RebelAssembler.Assembly;
using TernaryWorkbench.RebelAssembler.Assembly.Models;

namespace TernaryWorkbench.RebelAssembler;

/// <summary>
/// Public facade for assembling and disassembling REBEL-6 programs.
/// </summary>
/// <remarks>
/// REBEL-6 differences from REBEL-2:
/// <list type="bullet">
///   <item>Instructions are 32 trits wide (not 10).</item>
///   <item>729 registers (6-trit address), PC increments by 1 per instruction slot.</item>
///   <item>7 instruction formats: R, I, B, D, X (4-trit opcode), G, Y (2-trit opcode).</item>
///   <item>Dedicated 4-trit func field (not encoded in Rd2 as in REBEL-2).</item>
///   <item>RV32I binary compatibility via binary instruction group (opcode prefix +0).</item>
/// </list>
/// </remarks>
public static class Rebel6Assembler
{
    // -------------------------------------------------------------------------
    // ISA metadata
    // -------------------------------------------------------------------------

    /// <summary>The 729 valid instruction addresses in 6-trit balanced ternary, ordered by integer value.</summary>
    public static string[] AddressSpace => InstructionSet6.AddressSpace;

    // -------------------------------------------------------------------------
    // Assembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assemble a block of REBEL-6 assembly into a list of <see cref="AssembledInstruction"/> records.
    /// Labels are resolved within the block.
    /// </summary>
    public static IReadOnlyList<AssembledInstruction> AssembleInstructions(string assembly) =>
        PageAssembler.AssemblePage(
            assembly,
            padPage: false,
            InstructionSet6.DefaultPaddingInstruction,
            InstructionSet6.Patterns,
            (inst, labels, pats, currentIdx) => InstructionEncoder6.Translate(inst, labels, pats, currentIdx),
            InstructionSet6.AddressSpace);

    /// <summary>
    /// Translate a single REBEL-6 assembly instruction string into a 32-trit machine code string.
    /// </summary>
    public static string Translate(string instruction) =>
        InstructionEncoder6.Translate(instruction, InstructionSet6.Patterns);

    // -------------------------------------------------------------------------
    // Disassembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Disassemble a single 32-trit machine code string into a mnemonic+operands string.
    /// </summary>
    public static string Disassemble(string machineCode) =>
        InstructionDisassembler6.Disassemble(machineCode, InstructionSet6.Patterns);

    /// <summary>
    /// Disassemble a sequence of 32-trit machine code strings into mnemonic+operands strings.
    /// </summary>
    public static IReadOnlyList<string> DisassemblePage(IEnumerable<string> machineCodes) =>
        [.. machineCodes.Select(mc => InstructionDisassembler6.Disassemble(mc, InstructionSet6.Patterns))];
}
