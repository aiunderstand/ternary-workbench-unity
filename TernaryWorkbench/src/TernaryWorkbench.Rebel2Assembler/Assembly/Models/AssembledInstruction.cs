namespace TernaryWorkbench.Rebel2Assembler.Assembly.Models;

/// <summary>
/// A single assembled instruction with its address and source text.
/// </summary>
public sealed record AssembledInstruction(int Index, string Address, string Assembly, string MachineCode);
