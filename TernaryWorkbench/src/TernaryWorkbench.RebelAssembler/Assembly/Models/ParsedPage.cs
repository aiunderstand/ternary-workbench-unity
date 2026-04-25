namespace TernaryWorkbench.RebelAssembler.Assembly.Models;

internal sealed record ParsedPage(
    IReadOnlyList<ParsedInstruction> Instructions,
    IReadOnlyDictionary<string, LabelDefinition> Labels);
