using TernaryWorkbench.RebelAssembler.Assembly.Models;

namespace TernaryWorkbench.RebelAssembler.Assembly;

/// <summary>
/// REBEL-6 ISA constants and instruction patterns.
/// <para>
/// Standard encoding (4-trit opcode), 32 trits total, left to right:
/// <c>rs1[6] | rs2[6] | rd1[6] | rd2[6] | func[4] | opcode[4]</c>
/// </para>
/// <para>
/// G-type (2-trit opcode, long immediate + destination):
/// <c>imm[24] | rd1[6] | opcode[2]</c>
/// </para>
/// <para>
/// Y-type (2-trit opcode, long immediate + source):
/// <c>imm[18] | rs1[6] | func[6] | opcode[2]</c>
/// </para>
/// </summary>
internal static class InstructionSet6
{
    // -------------------------------------------------------------------------
    // Field name constants (shared with existing encoder conventions where possible)
    // -------------------------------------------------------------------------

    public const string Rs1    = "rs1";
    public const string Rs2    = "rs2";
    public const string Rd1    = "rd1";
    public const string Rd2    = "rd2";
    public const string Func   = "func";
    public const string Imm    = "imm";     // maps to rs2 slot in I-type
    public const string Offset = "offset";  // maps to rd2 slot in B-type

    public const string DefaultField = "000000"; // 6 trits, zero register / unused field
    public const string DefaultFunc  = "0000";   // 4-trit func field, all zero
    public const string DefaultPaddingInstruction = "NOP.T";

    // REBEL-6 page: 3^6 = 729 instruction slots
    public const int PageInstructionCount = 729;
    public const int InstructionWidth     = 32;

    // -------------------------------------------------------------------------
    // Address space: 729 6-trit balanced-ternary strings, -364 … +364
    // -------------------------------------------------------------------------

    public static readonly string[] AddressSpace = BuildAddressSpace();

    private static string[] BuildAddressSpace()
    {
        var space = new string[729];
        for (int i = 0; i < 729; i++)
            space[i] = ToBalancedTernaryN(i - 364, 6);
        return space;
    }

    // -------------------------------------------------------------------------
    // Register dictionary: X-364 … X364 (X-0 omitted, only X0)
    // -------------------------------------------------------------------------

    public static readonly Dictionary<string, string> RegisterDictionary = BuildRegisterDictionary();

    private static Dictionary<string, string> BuildRegisterDictionary()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int n = -364; n <= 364; n++)
        {
            string trits = ToBalancedTernaryN(n, 6);
            if (n == 0)
                dict["X0"] = trits;
            else if (n > 0)
                dict[$"X{n}"] = trits;
            else
                dict[$"X{n}"] = trits; // e.g. "X-1", "X-364"
        }
        return dict;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts an integer to a balanced-ternary string of exactly <paramref name="width"/> trits
    /// (most-significant trit first).
    /// </summary>
    public static string ToBalancedTernaryN(int value, int width)
    {
        var digits = new char[width];
        for (int i = width - 1; i >= 0; i--)
        {
            int rem = ((value % 3) + 3) % 3; // ensure non-negative: 0, 1, or 2
            if (rem == 2)      { digits[i] = '-'; value = (value + 1) / 3; }
            else if (rem == 1) { digits[i] = '+'; value = (value - 1) / 3; }
            else               { digits[i] = '0'; value /= 3; }
        }
        if (value != 0)
            throw new OverflowException($"Value is out of the {width}-trit balanced-ternary range.");
        return new string(digits);
    }

    // -------------------------------------------------------------------------
    // Instruction patterns
    // -------------------------------------------------------------------------

    public static readonly IReadOnlyDictionary<string, InstructionPattern> Patterns =
        new Dictionary<string, InstructionPattern>(StringComparer.OrdinalIgnoreCase)
        {
            // =================================================================
            // TERNARY BASE (opcode prefix ++)
            // =================================================================

            // ----------------------------------------------------------------
            // R-type ALU  opcode=++--  func discriminator (stored as 4 trits)
            // ----------------------------------------------------------------
            { "ADD.T",  new InstructionPattern("ADD.T",  "++--", [Rd1, Rs1, Rs2], Func4("00--")) },
            { "SUB.T",  new InstructionPattern("SUB.T",  "++--", [Rd1, Rs1, Rs2], Func4("00-0")) },
            { "SL.T",   new InstructionPattern("SL.T",   "++--", [Rd1, Rs1, Rs2], Func4("00-+")) },
            { "SR.T",   new InstructionPattern("SR.T",   "++--", [Rd1, Rs1, Rs2], Func4("000-")) },
            { "SLT.T",  new InstructionPattern("SLT.T",  "++--", [Rd1, Rs1, Rs2], Func4("0000")) },
            { "OR.T",   new InstructionPattern("OR.T",   "++--", [Rd1, Rs1, Rs2], Func4("000+")) },
            { "XOR.T",  new InstructionPattern("XOR.T",  "++--", [Rd1, Rs1, Rs2], Func4("00+-")) },
            { "AND.T",  new InstructionPattern("AND.T",  "++--", [Rd1, Rs1, Rs2], Func4("00+0")) },

            // ----------------------------------------------------------------
            // R-type misc  opcode=++-0
            // ----------------------------------------------------------------
            { "CMP.T",  new InstructionPattern("CMP.T",  "++-0", [Rd1, Rs1, Rs2], Func4("00--")) },
            { "STI.T",  new InstructionPattern("STI.T",  "++-0", [Rd1, Rs1],
                Merge(Func4("00-0"), Fixed(Rs2, DefaultField))) },

            // ----------------------------------------------------------------
            // I-type ALU  opcode=++-+   (Imm → rs2 slot)
            // ----------------------------------------------------------------
            { "ADDI.T",  new InstructionPattern("ADDI.T",  "++-+", [Rd1, Rs1, Imm], Func4("00--")) },
            { "SLI.T",   new InstructionPattern("SLI.T",   "++-+", [Rd1, Rs1, Imm], Func4("00-0")) },
            { "SRI.T",   new InstructionPattern("SRI.T",   "++-+", [Rd1, Rs1, Imm], Func4("00-+")) },
            { "SLTI.T",  new InstructionPattern("SLTI.T",  "++-+", [Rd1, Rs1, Imm], Func4("000-")) },
            { "ORI.T",   new InstructionPattern("ORI.T",   "++-+", [Rd1, Rs1, Imm], Func4("0000")) },
            { "XORI.T",  new InstructionPattern("XORI.T",  "++-+", [Rd1, Rs1, Imm], Func4("000+")) },
            { "ANDI.T",  new InstructionPattern("ANDI.T",  "++-+", [Rd1, Rs1, Imm], Func4("00+-")) },

            // Pseudo-instructions (reuse ADDI.T encoding)
            { "NOP.T",   new InstructionPattern("NOP.T",   "++-+", [],
                Merge(Func4("00--"), Fixed(Rs1, DefaultField), Fixed(Rs2, DefaultField), Fixed(Rd1, DefaultField), Fixed(Rd2, DefaultField))) },
            { "MV.T",    new InstructionPattern("MV.T",    "++-+", [Rd1, Rs1],
                Merge(Func4("00--"), Fixed(Rs2, DefaultField), Fixed(Rd2, DefaultField))) },

            // ----------------------------------------------------------------
            // I-type load  opcode=++0-   (Imm → rs2 slot)
            // ----------------------------------------------------------------
            { "LW.T",    new InstructionPattern("LW.T",    "++0-", [Rd1, Rs1, Imm], Func4("00--")) },
            { "LH.T",    new InstructionPattern("LH.T",    "++0-", [Rd1, Rs1, Imm], Func4("00-0")) },
            { "LT.T",    new InstructionPattern("LT.T",    "++0-", [Rd1, Rs1, Imm], Func4("00-+")) },
            { "JALR.T",  new InstructionPattern("JALR.T",  "++0-", [Rd1, Rs1, Imm], Func4("000-")) },

            // ----------------------------------------------------------------
            // B-type branch  opcode=++00   (Offset → rd2 slot, Rd1 fixed)
            // ----------------------------------------------------------------
            { "BEQ.T",   new InstructionPattern("BEQ.T",   "++00", [Rs1, Rs2, Offset],
                Merge(Func4("00--"), Fixed(Rd1, DefaultField))) },
            { "BNE.T",   new InstructionPattern("BNE.T",   "++00", [Rs1, Rs2, Offset],
                Merge(Func4("00-0"), Fixed(Rd1, DefaultField))) },
            { "BLT.T",   new InstructionPattern("BLT.T",   "++00", [Rs1, Rs2, Offset],
                Merge(Func4("00-+"), Fixed(Rd1, DefaultField))) },
            { "BGE.T",   new InstructionPattern("BGE.T",   "++00", [Rs1, Rs2, Offset],
                Merge(Func4("000-"), Fixed(Rd1, DefaultField))) },

            // ----------------------------------------------------------------
            // B-type store  opcode=++0+   (Offset/Imm → rd2 slot, Rd1 fixed)
            // ----------------------------------------------------------------
            { "SW.T",    new InstructionPattern("SW.T",    "++0+", [Rs1, Rs2, Offset],
                Merge(Func4("00--"), Fixed(Rd1, DefaultField))) },
            { "SH.T",    new InstructionPattern("SH.T",    "++0+", [Rs1, Rs2, Offset],
                Merge(Func4("00-0"), Fixed(Rd1, DefaultField))) },
            { "ST.T",    new InstructionPattern("ST.T",    "++0+", [Rs1, Rs2, Offset],
                Merge(Func4("00-+"), Fixed(Rd1, DefaultField))) },

            // ----------------------------------------------------------------
            // D-type (3 sources)  opcode=+++-   (Rd2 slot encodes Rs3)
            // ----------------------------------------------------------------
            { "MAJV.T",  new InstructionPattern("MAJV.T",  "+++-", [Rd1, Rs1, Rs2, Rd2], Func4("00--")) },
            { "MINV.T",  new InstructionPattern("MINV.T",  "+++-", [Rd1, Rs1, Rs2, Rd2], Func4("00-0")) },

            // ----------------------------------------------------------------
            // X-type (dual immediate)  opcode=+++0
            // [Rd1, Rd2, Rs1, Rs2] — Rs1/Rs2 slots carry the two immediates
            // ----------------------------------------------------------------
            { "LI2.T",   new InstructionPattern("LI2.T",   "+++0", [Rd1, Rd2, Rs1, Rs2], Func4("00--")) },

            // =================================================================
            // LONG-IMMEDIATE FORMS (2-trit opcode)
            // G-type: imm24(24) | rd1_as_func(6) | opcode(2)
            // Y-type: imm18(18) | rs1(6)          | func(6=000000) | opcode(2)
            // =================================================================
            { "LWA.T",   new InstructionPattern("LWA.T",   "0+",  [Rd1, Imm]) },
            { "LI.T",    new InstructionPattern("LI.T",    "00",  [Rd1, Imm]) },
            { "SWA.T",   new InstructionPattern("SWA.T",   "0-",  [Rs1, Imm]) },
            { "JAL.T",   new InstructionPattern("JAL.T",   "-+",  [Rd1, Imm]) },
            { "AIPC.T",  new InstructionPattern("AIPC.T",  "-0",  [Rd1, Imm]) },

            // =================================================================
            // BINARY BASE (opcode prefix +0)
            // =================================================================

            // ----------------------------------------------------------------
            // R-type binary ALU  opcode=+0--
            // ----------------------------------------------------------------
            { "ADD",     new InstructionPattern("ADD",  "+0--", [Rd1, Rs1, Rs2], Func4("00--")) },
            { "SUB",     new InstructionPattern("SUB",  "+0--", [Rd1, Rs1, Rs2], Func4("00-0")) },
            { "SLL",     new InstructionPattern("SLL",  "+0--", [Rd1, Rs1, Rs2], Func4("00-+")) },
            { "SRL",     new InstructionPattern("SRL",  "+0--", [Rd1, Rs1, Rs2], Func4("000-")) },
            { "SRA",     new InstructionPattern("SRA",  "+0--", [Rd1, Rs1, Rs2], Func4("0000")) },
            { "SLTU",    new InstructionPattern("SLTU", "+0--", [Rd1, Rs1, Rs2], Func4("000+")) },
            { "OR",      new InstructionPattern("OR",   "+0--", [Rd1, Rs1, Rs2], Func4("00+-")) },
            { "XOR",     new InstructionPattern("XOR",  "+0--", [Rd1, Rs1, Rs2], Func4("00+0")) },
            { "AND",     new InstructionPattern("AND",  "+0--", [Rd1, Rs1, Rs2], Func4("00++")) },

            // ----------------------------------------------------------------
            // I-type binary ALU  opcode=+0-0  (Imm → rs2 slot)
            // ----------------------------------------------------------------
            { "ADDI",    new InstructionPattern("ADDI",  "+0-0", [Rd1, Rs1, Imm], Func4("00--")) },
            { "SLLI",    new InstructionPattern("SLLI",  "+0-0", [Rd1, Rs1, Imm], Func4("00-0")) },
            { "SRLI",    new InstructionPattern("SRLI",  "+0-0", [Rd1, Rs1, Imm], Func4("00-+")) },
            { "SRAI",    new InstructionPattern("SRAI",  "+0-0", [Rd1, Rs1, Imm], Func4("000-")) },
            { "SLTIU",   new InstructionPattern("SLTIU", "+0-0", [Rd1, Rs1, Imm], Func4("0000")) },
            { "ORI",     new InstructionPattern("ORI",   "+0-0", [Rd1, Rs1, Imm], Func4("000+")) },
            { "XORI",    new InstructionPattern("XORI",  "+0-0", [Rd1, Rs1, Imm], Func4("00+-")) },
            { "ANDI",    new InstructionPattern("ANDI",  "+0-0", [Rd1, Rs1, Imm], Func4("00+0")) },

            // ----------------------------------------------------------------
            // I-type binary load  opcode=+0-+  (Imm → rs2 slot)
            // ----------------------------------------------------------------
            { "LW",      new InstructionPattern("LW",  "+0-+", [Rd1, Rs1, Imm], Func4("00--")) },
            { "LH",      new InstructionPattern("LH",  "+0-+", [Rd1, Rs1, Imm], Func4("00-0")) },
            { "LB",      new InstructionPattern("LB",  "+0-+", [Rd1, Rs1, Imm], Func4("00-+")) },
            { "LHU",     new InstructionPattern("LHU", "+0-+", [Rd1, Rs1, Imm], Func4("000-")) },
            { "LBU",     new InstructionPattern("LBU", "+0-+", [Rd1, Rs1, Imm], Func4("0000")) },

            // ----------------------------------------------------------------
            // B-type binary unsigned branch  opcode=+00-  (Offset → rd2 slot)
            // ----------------------------------------------------------------
            { "BLTU",    new InstructionPattern("BLTU", "+00-", [Rs1, Rs2, Offset],
                Merge(Func4("00--"), Fixed(Rd1, DefaultField))) },
            { "BGEU",    new InstructionPattern("BGEU", "+00-", [Rs1, Rs2, Offset],
                Merge(Func4("00-0"), Fixed(Rd1, DefaultField))) },

            // ----------------------------------------------------------------
            // B-type binary signed branch  opcode=+00+  (Offset → rd2 slot)
            // ----------------------------------------------------------------
            { "BEQ",     new InstructionPattern("BEQ",  "+00+", [Rs1, Rs2, Offset],
                Merge(Func4("00--"), Fixed(Rd1, DefaultField))) },
            { "BNE",     new InstructionPattern("BNE",  "+00+", [Rs1, Rs2, Offset],
                Merge(Func4("00-0"), Fixed(Rd1, DefaultField))) },
            { "BLT",     new InstructionPattern("BLT",  "+00+", [Rs1, Rs2, Offset],
                Merge(Func4("00-+"), Fixed(Rd1, DefaultField))) },
            { "BGE",     new InstructionPattern("BGE",  "+00+", [Rs1, Rs2, Offset],
                Merge(Func4("000-"), Fixed(Rd1, DefaultField))) },

            // ----------------------------------------------------------------
            // B-type binary store  opcode=+000  (Offset → rd2 slot)
            // ----------------------------------------------------------------
            { "SW",      new InstructionPattern("SW",  "+000", [Rs1, Rs2, Offset],
                Merge(Func4("00--"), Fixed(Rd1, DefaultField))) },
            { "SH",      new InstructionPattern("SH",  "+000", [Rs1, Rs2, Offset],
                Merge(Func4("00-0"), Fixed(Rd1, DefaultField))) },
            { "SB",      new InstructionPattern("SB",  "+000", [Rs1, Rs2, Offset],
                Merge(Func4("00-+"), Fixed(Rd1, DefaultField))) },

            // ----------------------------------------------------------------
            // Binary control flow  opcode=+0+-
            // ----------------------------------------------------------------
            { "JAL",     new InstructionPattern("JAL",  "+0+-", [Rd1, Imm],
                Merge(Func4("00--"), Fixed(Rs1, DefaultField))) },
            { "JALR",    new InstructionPattern("JALR", "+0+-", [Rd1, Rs1, Imm], Func4("00-0")) },

            // ----------------------------------------------------------------
            // Binary upper immediate  opcode=+0+0
            // ----------------------------------------------------------------
            { "LUI",     new InstructionPattern("LUI",   "+0+0", [Rd1, Imm],
                Merge(Func4("00--"), Fixed(Rs1, DefaultField))) },
            { "AUIPC",   new InstructionPattern("AUIPC", "+0+0", [Rd1, Imm],
                Merge(Func4("00-0"), Fixed(Rs1, DefaultField))) },

            // ----------------------------------------------------------------
            // Binary system  opcode=+0++
            // ----------------------------------------------------------------
            { "FENCE",   new InstructionPattern("FENCE",  "+0++", [],
                Merge(Func4("00--"), Fixed(Rs1, DefaultField), Fixed(Rs2, DefaultField), Fixed(Rd1, DefaultField), Fixed(Rd2, DefaultField))) },
            { "ECALL",   new InstructionPattern("ECALL",  "+0++", [],
                Merge(Func4("00-0"), Fixed(Rs1, DefaultField), Fixed(Rs2, DefaultField), Fixed(Rd1, DefaultField), Fixed(Rd2, DefaultField))) },
            { "EBREAK",  new InstructionPattern("EBREAK", "+0++", [],
                Merge(Func4("00-+"), Fixed(Rs1, DefaultField), Fixed(Rs2, DefaultField), Fixed(Rd1, DefaultField), Fixed(Rd2, DefaultField))) },
        };

    // -------------------------------------------------------------------------
    // Pattern-building helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns a Defaults dict with a single Func entry.</summary>
    private static Dictionary<string, string> Func4(string func4) =>
        new(StringComparer.OrdinalIgnoreCase) { { Func, func4 } };

    /// <summary>Returns a single-entry fixed-field dict.</summary>
    private static Dictionary<string, string> Fixed(string field, string value) =>
        new(StringComparer.OrdinalIgnoreCase) { { field, value } };

    /// <summary>Merges multiple single-entry dicts into one Defaults dict.</summary>
    private static Dictionary<string, string> Merge(params Dictionary<string, string>[] parts)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
            foreach (var kv in part)
                result[kv.Key] = kv.Value;
        return result;
    }
}
