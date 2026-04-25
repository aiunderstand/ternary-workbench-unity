namespace TernaryWorkbench.RebelAssembler;

/// <summary>
/// A single history entry: one assembled or disassembled instruction.
/// </summary>
/// <param name="Assembly">Assembly mnemonic text (e.g. <c>ADD.T X1, X2, X3</c>).</param>
/// <param name="MachineCode">10-trit machine code string (e.g. <c>--+-+00+00</c>).</param>
/// <param name="Isa">ISA identifier — see <see cref="TernaryWorkbench.RebelAssembler.Isa"/>.</param>
/// <param name="Direction">Whether this entry was produced by assembly or disassembly.</param>
public record AssemblyRecord(
    string Assembly,
    string MachineCode,
    string Isa,
    AssemblyDirection Direction);
