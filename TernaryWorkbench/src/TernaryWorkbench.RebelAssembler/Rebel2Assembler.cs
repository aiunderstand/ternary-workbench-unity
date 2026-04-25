using TernaryWorkbench.RebelAssembler.Assembly;
using TernaryWorkbench.RebelAssembler.Assembly.Models;
using static TernaryWorkbench.RebelAssembler.Assembly.InstructionSet;

namespace TernaryWorkbench.RebelAssembler;

/// <summary>
/// Public facade for assembling and disassembling REBEL-2 programs.
/// </summary>
/// <remarks>
/// Ported from <c>MRCSStudio.REBEL2.Assembler</c>.
/// Deviations from the original:
/// <list type="bullet">
///   <item>No <c>VectorEmitter</c> — TernaryWorkbench has no circuit simulator.</item>
///   <item>No <c>MRCSStudio.Domain</c> dependency — comment stripping is inlined.</item>
/// </list>
/// </remarks>
public static class Rebel2Assembler
{
    // -------------------------------------------------------------------------
    // Assembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assemble a block of REBEL-2 assembly (1–9 instructions) into a list of
    /// <see cref="AssembledInstruction"/> records that include address, source text,
    /// and 10-trit machine code.  Labels are resolved within the block.
    /// </summary>
    public static IReadOnlyList<AssembledInstruction> AssembleInstructions(string assembly) =>
        PageAssembler.AssemblePage(assembly, padPage: false, DefaultPaddingInstruction);

    /// <summary>
    /// Translate a single assembly instruction string into a 10-trit machine code string.
    /// Labels are resolved within the single-instruction snippet.
    /// </summary>
    public static string Translate(string instruction) =>
        InstructionEncoder.Translate(instruction);

    // -------------------------------------------------------------------------
    // Disassembly
    // -------------------------------------------------------------------------

    /// <summary>
    /// Disassemble a single 10-trit machine code string into a mnemonic+operands string.
    /// </summary>
    public static string Disassemble(string machineCode) =>
        InstructionDisassembler.Disassemble(machineCode);

    /// <summary>
    /// Disassemble a block of 1–9 10-trit machine code strings (one per element)
    /// into a list of mnemonic+operands strings.
    /// </summary>
    public static IReadOnlyList<string> DisassemblePage(IEnumerable<string> machineCodes) =>
        [.. machineCodes.Select(mc => InstructionDisassembler.Disassemble(mc))];
}
