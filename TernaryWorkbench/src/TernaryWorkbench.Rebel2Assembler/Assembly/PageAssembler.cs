using TernaryWorkbench.Rebel2Assembler.Assembly.Models;
using static TernaryWorkbench.Rebel2Assembler.Assembly.InstructionSet;

namespace TernaryWorkbench.Rebel2Assembler.Assembly;

internal static class PageAssembler
{
    public static IReadOnlyList<AssembledInstruction> AssemblePage(string assembly, bool padPage, string padInstruction)
    {
        var instructions = new List<AssembledInstruction>(PageInstructionCount);
        var parsed = InstructionParser.ParsePage(assembly);

        foreach (var parsedInstruction in parsed.Instructions)
        {
            var machineCode = InstructionEncoder.Translate(parsedInstruction, parsed.Labels);
            instructions.Add(new AssembledInstruction(
                instructions.Count,
                AddressSpace[instructions.Count],
                parsedInstruction.Text,
                machineCode));
        }

        if (!padPage || instructions.Count >= PageInstructionCount)
            return instructions;

        var padMachineCode = InstructionEncoder.Translate(padInstruction);
        while (instructions.Count < PageInstructionCount)
        {
            var address = AddressSpace[instructions.Count];
            instructions.Add(new AssembledInstruction(
                instructions.Count,
                address,
                padInstruction,
                padMachineCode));
        }

        return instructions;
    }
}
