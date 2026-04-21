namespace TernaryWorkbench.Rebel2Assembler.Assembly.Models;

internal sealed record ParsedPage(
    IReadOnlyList<ParsedInstruction> Instructions,
    IReadOnlyDictionary<string, LabelDefinition> Labels);
