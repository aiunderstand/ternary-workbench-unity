namespace TernaryWorkbench.Rebel2Assembler.Assembly.Models;

internal sealed record ParsedInstruction(int LineNumber, string Text, IReadOnlyList<string> Parts);
