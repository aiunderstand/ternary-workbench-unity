using TernaryWorkbench.Rebel2Assembler.Assembly.Models;
using static TernaryWorkbench.Rebel2Assembler.Assembly.InstructionSet;

namespace TernaryWorkbench.Rebel2Assembler.Assembly;

internal static class InstructionDisassembler
{
    /// <summary>
    /// Disassemble a 10-trit machine code string into a mnemonic+operands string.
    /// </summary>
    public static string Disassemble(string instruction)
    {
        if (instruction.Length != 10)
            throw new InvalidOperationException($"Instruction must be 10 trits long, got {instruction.Length}.");

        if (!instruction.All(ch => ch is '+' or '-' or '0'))
            throw new InvalidOperationException($"Instruction contains invalid characters: '{instruction}'. Only '+', '-', '0' are allowed.");

        var opcode = instruction[..2];
        var rs1    = instruction[2..4];
        var rs2    = instruction[4..6];
        var rd1    = instruction[6..8];
        var rd2    = instruction[8..10];

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { Rs1, rs1 },
            { Rs2, rs2 },
            { Rd1, rd1 },
            { Rd2, rd2 }
        };

        var pattern = ResolvePattern(opcode, fields)
            ?? throw new InvalidOperationException($"Unknown opcode '{opcode}'.");

        var operands = pattern.AssemblyOperands
            .Select(fieldName => string.Equals(fieldName, Imm, StringComparison.OrdinalIgnoreCase) ? Rs2 : fieldName)
            .Select(sourceField => FormatOperand(fields[sourceField]))
            .ToList();

        return operands.Count == 0
            ? pattern.Mnemonic
            : $"{pattern.Mnemonic} {string.Join(", ", operands)}";
    }

    // -------------------------------------------------------------------------
    // Pattern matching
    // -------------------------------------------------------------------------

    private static InstructionPattern? ResolvePattern(string opcode, IReadOnlyDictionary<string, string> fields)
    {
        var best = Patterns.Values
            .Select((pattern, index) => (pattern, index, score: ScorePattern(pattern, opcode, fields)))
            .Where(x => x.score >= 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.index)
            .Select(x => x.pattern)
            .FirstOrDefault();
        return best;
    }

    /// <summary>
    /// Score how well a pattern matches the given opcode and field values.
    /// Returns -1 if the pattern does not match (wrong opcode or a fixed field differs).
    /// Returns ≥0 counting the number of non-assembly fields that matched their expected value.
    /// </summary>
    private static int ScorePattern(InstructionPattern pattern, string opcode, IReadOnlyDictionary<string, string> fields)
    {
        if (!string.Equals(pattern.Opcode, opcode, StringComparison.Ordinal))
            return -1;

        // Fields that are supplied by the assembly operands (variable)
        var assemblyFields = pattern.AssemblyOperands
            .Select(op => string.Equals(op, Imm, StringComparison.OrdinalIgnoreCase) ? Rs2 : op)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var score = 0;
        foreach (var fieldName in FieldOffsets.Keys.Where(f => !string.Equals(f, "opcode", StringComparison.OrdinalIgnoreCase)))
        {
            if (assemblyFields.Contains(fieldName))
                continue; // variable field — don't check against default

            var expected = DefaultField;
            if (pattern.Defaults != null && pattern.Defaults.TryGetValue(fieldName, out var defaultValue))
                expected = defaultValue;

            if (!string.Equals(fields[fieldName], expected, StringComparison.Ordinal))
                return -1;

            score++;
        }

        return score;
    }

    // -------------------------------------------------------------------------
    // Operand formatting
    // -------------------------------------------------------------------------

    private static string FormatOperand(string fieldValue)
    {
        // Prefer canonical register name (first match wins)
        foreach (var kvp in RegisterDictionary)
        {
            if (kvp.Value == fieldValue)
                return kvp.Key;
        }

        // Fall back to numeric index in AddressSpace (used for immediates and labels)
        var numericIndex = Array.IndexOf(AddressSpace, fieldValue);
        if (numericIndex >= 0)
            return (numericIndex - 4).ToString();

        return fieldValue;
    }
}
