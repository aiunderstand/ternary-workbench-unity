namespace TernaryWorkbench.RebelAssembler.Assembly.Models;

internal sealed record ParsedInstruction(int LineNumber, string Text, IReadOnlyList<string> Parts);
