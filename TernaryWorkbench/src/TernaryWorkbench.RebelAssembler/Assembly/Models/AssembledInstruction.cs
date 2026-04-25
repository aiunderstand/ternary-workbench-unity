namespace TernaryWorkbench.RebelAssembler.Assembly.Models;

/// <summary>
/// A single assembled instruction with its address and source text.
/// </summary>
public sealed record AssembledInstruction(int Index, string Address, string Assembly, string MachineCode);
