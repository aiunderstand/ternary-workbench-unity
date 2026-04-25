using FluentAssertions;
using TernaryWorkbench.RebelAssembler;
using Xunit;
using Asm = TernaryWorkbench.RebelAssembler.Rebel6Assembler;

namespace TernaryWorkbench.Tests;

/// <summary>
/// Tests for the REBEL-6 assembler and disassembler.
///
/// Machine-code layout (32 trits, left to right):
///   [31:26] rs1 (6T) | [25:20] rs2 (6T) | [19:14] rd1 (6T) |
///   [13:8]  rd2 (6T) | [7:4]   func (4T) | [3:0]   opcode (4T)
///
/// G-type (2-trit opcode, MST ≠ '+'):  imm(24) | rd1(6)          | opcode(2)
/// Y-type (2-trit opcode, MST ≠ '+'):  imm(18) | rs1(6) | 0*(6)  | opcode(2)
///
/// Register encoding (selected):
///   X0 = "000000"  X1 = "00000+"  X2 = "0000+-"
///   X3 = "0000+0"  X4 = "0000++"
/// </summary>
public class Rebel6AssemblerTests
{
    // =========================================================================
    // 1. Single-instruction assembly — explicit machine code verification
    //    Covers one representative per format type.
    // =========================================================================

    [Theory]
    // R-type  opcode=++--  func discriminates the operation
    [InlineData("ADD.T X1, X2, X3",        "0000+-0000+000000+00000000--++--")]
    [InlineData("SUB.T X1, X2, X3",        "0000+-0000+000000+00000000-0++--")]
    [InlineData("OR.T X1, X2, X3",         "0000+-0000+000000+000000000+++--")] // func=000+
    // I-type  opcode=++-+  Imm encoded in rs2 slot
    [InlineData("ADDI.T X1, X2, X3",       "0000+-0000+000000+00000000--++-+")]
    [InlineData("ADDI.T X1, X2, 3",        "0000+-0000+000000+00000000--++-+")]  // 3 == X3
    // NOP.T and MV.T (pseudo-instructions sharing opcode ++-+ and func 00--)
    [InlineData("NOP.T",                   "00000000000000000000000000--++-+")]
    [InlineData("MV.T X1, X2",            "0000+-00000000000+00000000--++-+")]
    // B-type branch  opcode=++00  offset in rd2 slot
    [InlineData("BEQ.T X1, X2, 0",        "00000+0000+-00000000000000--++00")]
    // D-type (3 sources + dest)  opcode=+++-
    [InlineData("MAJV.T X1, X2, X3, X4",  "0000+-0000+000000+0000++00--+++-")]
    // X-type (dual dest + dual imm)  opcode=+++0
    [InlineData("LI2.T X1, X2, X3, X4",   "0000+00000++00000+0000+-00--+++0")]
    // G-type (24-trit immediate)  opcode=00
    [InlineData("LI.T X1, 1",             "00000000000000000000000+00000+00")]
    // Y-type (18-trit immediate + source)  opcode=0-
    [InlineData("SWA.T X1, 1",            "00000000000000000+00000+0000000-")]
    // Binary R-type  opcode=+0--
    [InlineData("ADD X1, X2, X3",         "0000+-0000+000000+00000000--+0--")]
    // Binary I-type  opcode=+0-0
    [InlineData("ADDI X1, X2, X3",        "0000+-0000+000000+00000000--+0-0")]
    // Binary B-type  opcode=+00+
    [InlineData("BEQ X1, X2, 0",          "00000+0000+-00000000000000--+00+")]
    // Binary system  opcode=+0++
    [InlineData("FENCE",                  "00000000000000000000000000--+0++")]
    [InlineData("ECALL",                  "00000000000000000000000000-0+0++")]
    public void Translate_SingleInstruction_ProducesMachineCode(string assembly, string expected)
    {
        Asm.Translate(assembly).Should().Be(expected);
    }

    // =========================================================================
    // 2. Long-immediate (G/Y-type) explicit encoding
    // =========================================================================

    [Fact]
    public void LiT_X0_Zero_IsAllZeros()
    {
        // G-type: imm24(all-zero) | rd1(X0=000000) | opcode(00)
        Asm.Translate("LI.T X0, 0").Should().Be(new string('0', 32));
    }

    [Fact]
    public void JalT_X0_Zero_EncodesCorrectly()
    {
        // G-type, opcode="-+": imm24(all-zero) | rd1(X0) | "-+"
        var mc = Asm.Translate("JAL.T X0, 0");
        mc.Should().HaveLength(32);
        mc[^2..].Should().Be("-+", because: "JAL.T has 2-trit opcode '-+'");
        mc[24..30].Should().Be("000000", because: "rd1=X0 encodes as 000000 in G-type");
    }

    [Fact]
    public void SwaT_X0_Zero_LastCharIsMinus()
    {
        // Y-type, opcode="0-": last char must be '-'
        Asm.Translate("SWA.T X0, 0")[^1].Should().Be('-', because: "SWA.T opcode is '0-'");
    }

    [Fact]
    public void LiT_NumericImmediate_EncodesCorrectly()
    {
        // 1 in 24-trit BT = "00000000000000000000000+"
        var mc = Asm.Translate("LI.T X1, 1");
        mc[..24].Should().Be("00000000000000000000000+");
    }

    // =========================================================================
    // 3. Disassembly: machine code → canonical mnemonic+operands
    // =========================================================================

    [Theory]
    // R-type
    [InlineData("0000+-0000+000000+00000000--++--",  "ADD.T X1, X2, X3")]
    [InlineData("0000+-0000+000000+00000000-0++--",  "SUB.T X1, X2, X3")]
    // I-type
    [InlineData("0000+-0000+000000+00000000--++-+",  "ADDI.T X1, X2, X3")]  // rs2 slot → X3
    // NOP and MV (pseudo)
    [InlineData("00000000000000000000000000--++-+",  "NOP.T")]
    [InlineData("0000+-00000000000+00000000--++-+",  "MV.T X1, X2")]
    // B-type
    [InlineData("00000+0000+-00000000000000--++00",  "BEQ.T X1, X2, 0")]
    // D-type
    [InlineData("0000+-0000+000000+0000++00--+++-",  "MAJV.T X1, X2, X3, X4")]
    // X-type
    [InlineData("0000+00000++00000+0000+-00--+++0",  "LI2.T X1, X2, X3, X4")]
    // G-type
    [InlineData("00000000000000000000000+00000+00",  "LI.T X1, 1")]
    // Y-type
    [InlineData("00000000000000000+00000+0000000-",  "SWA.T X1, 1")]
    // Binary
    [InlineData("0000+-0000+000000+00000000--+0--",  "ADD X1, X2, X3")]
    [InlineData("00000000000000000000000000--+0++",  "FENCE")]
    public void Disassemble_MachineCode_ReturnsCanonicalMnemonic(string machineCode, string expected)
    {
        Asm.Disassemble(machineCode).Should().Be(expected);
    }

    // =========================================================================
    // 4. Round-trip: assemble → disassemble → re-assemble → same machine code
    //    One representative per instruction in each format group.
    // =========================================================================

    [Theory]
    // Ternary R-type (opcode ++--): func distinguishes operations
    [InlineData("ADD.T X1, X2, X3")]
    [InlineData("SUB.T X1, X2, X3")]
    [InlineData("SL.T X1, X2, X3")]
    [InlineData("SR.T X1, X2, X3")]
    [InlineData("SLT.T X1, X2, X3")]
    [InlineData("OR.T X1, X2, X3")]
    [InlineData("XOR.T X1, X2, X3")]
    [InlineData("AND.T X1, X2, X3")]
    // Ternary misc (opcode ++-0)
    [InlineData("CMP.T X1, X2, X3")]
    [InlineData("STI.T X1, X2")]
    // Ternary I-type (opcode ++-+)
    [InlineData("ADDI.T X1, X2, X3")]
    [InlineData("SLI.T X1, X2, X3")]
    [InlineData("SRI.T X1, X2, X3")]
    [InlineData("SLTI.T X1, X2, X3")]
    [InlineData("ORI.T X1, X2, X3")]
    [InlineData("XORI.T X1, X2, X3")]
    [InlineData("ANDI.T X1, X2, X3")]
    // Pseudo-instructions (opcode ++-+, share func with ADDI.T)
    [InlineData("NOP.T")]
    [InlineData("MV.T X1, X2")]
    // Ternary I-type load (opcode ++0-)
    [InlineData("LW.T X1, X2, X3")]
    [InlineData("LH.T X1, X2, X3")]
    [InlineData("LT.T X1, X2, X3")]
    [InlineData("JALR.T X1, X2, X3")]
    // Ternary B-type branch (opcode ++00, offset in rd2)
    [InlineData("BEQ.T X1, X2, 0")]
    [InlineData("BNE.T X1, X2, 0")]
    [InlineData("BLT.T X1, X2, 0")]
    [InlineData("BGE.T X1, X2, 0")]
    // Ternary B-type store (opcode ++0+, offset in rd2)
    [InlineData("SW.T X1, X2, 0")]
    [InlineData("SH.T X1, X2, 0")]
    [InlineData("ST.T X1, X2, 0")]
    // D-type (opcode +++-): 4 operands
    [InlineData("MAJV.T X1, X2, X3, X4")]
    [InlineData("MINV.T X1, X2, X3, X4")]
    // X-type (opcode +++0): dual dest + dual source
    [InlineData("LI2.T X1, X2, X3, X4")]
    // G-type (2-trit opcode): 24-trit immediate
    [InlineData("LI.T X1, 1")]
    [InlineData("LWA.T X1, 1")]
    [InlineData("JAL.T X1, 1")]
    [InlineData("AIPC.T X1, 1")]
    // Y-type (2-trit opcode): 18-trit immediate + source register
    [InlineData("SWA.T X1, 1")]
    // Binary R-type (opcode +0--)
    [InlineData("ADD X1, X2, X3")]
    [InlineData("SUB X1, X2, X3")]
    [InlineData("SLL X1, X2, X3")]
    [InlineData("SRL X1, X2, X3")]
    [InlineData("SRA X1, X2, X3")]
    [InlineData("SLTU X1, X2, X3")]
    [InlineData("OR X1, X2, X3")]
    [InlineData("XOR X1, X2, X3")]
    [InlineData("AND X1, X2, X3")]
    // Binary I-type (opcode +0-0)
    [InlineData("ADDI X1, X2, X3")]
    [InlineData("SLLI X1, X2, X3")]
    [InlineData("SRLI X1, X2, X3")]
    [InlineData("SRAI X1, X2, X3")]
    [InlineData("SLTIU X1, X2, X3")]
    [InlineData("ORI X1, X2, X3")]
    [InlineData("XORI X1, X2, X3")]
    [InlineData("ANDI X1, X2, X3")]
    // Binary load (opcode +0-+)
    [InlineData("LW X1, X2, X3")]
    [InlineData("LH X1, X2, X3")]
    [InlineData("LB X1, X2, X3")]
    [InlineData("LHU X1, X2, X3")]
    [InlineData("LBU X1, X2, X3")]
    // Binary branch (opcode +00+ signed, +00- unsigned)
    [InlineData("BEQ X1, X2, 0")]
    [InlineData("BNE X1, X2, 0")]
    [InlineData("BLT X1, X2, 0")]
    [InlineData("BGE X1, X2, 0")]
    [InlineData("BLTU X1, X2, 0")]
    [InlineData("BGEU X1, X2, 0")]
    // Binary store (opcode +000)
    [InlineData("SW X1, X2, 0")]
    [InlineData("SH X1, X2, 0")]
    [InlineData("SB X1, X2, 0")]
    // Binary control / upper-imm (opcode +0+-)
    [InlineData("JAL X1, X3")]
    [InlineData("JALR X1, X2, X3")]
    [InlineData("LUI X1, X3")]
    [InlineData("AUIPC X1, X3")]
    // Binary system (opcode +0++)
    [InlineData("FENCE")]
    [InlineData("ECALL")]
    [InlineData("EBREAK")]
    public void RoundTrip_Assemble_Disassemble_Reassemble_SameMachineCode(string assembly)
    {
        var machineCode  = Asm.Translate(assembly);
        var disassembled = Asm.Disassemble(machineCode);
        var reassembled  = Asm.Translate(disassembled);

        reassembled.Should().Be(machineCode,
            because: $"re-assembling the disassembly of '{assembly}' should yield the same 32-trit code");
    }

    // =========================================================================
    // 5. Pseudo-instruction disambiguation
    // =========================================================================

    [Fact]
    public void NopT_AllDataFieldsZero()
    {
        var mc = Asm.Translate("NOP.T");
        mc.Should().HaveLength(32);
        mc[..24].Should().Be(new string('0', 24), because: "NOP.T has all register fields zero");
        mc[28..32].Should().Be("++-+", because: "NOP.T uses I-type opcode ++-+");
    }

    [Fact]
    public void MvT_SameAsAddiTWithZeroImmediate()
    {
        // MV.T rd1, rs1 ≡ ADDI.T rd1, rs1, 0 (both use opcode ++-+, func 00--, imm=0)
        Asm.Translate("MV.T X1, X2").Should().Be(Asm.Translate("ADDI.T X1, X2, 0"));
    }

    [Fact]
    public void AddiT_NumericAndRegisterFormProduceSameCode()
    {
        // 3 in 6-trit BT = "0000+0" = register X3
        Asm.Translate("ADDI.T X1, X2, 3").Should().Be(Asm.Translate("ADDI.T X1, X2, X3"));
    }

    // =========================================================================
    // 6. Instruction format structure verification
    // =========================================================================

    [Fact]
    public void StandardInstructions_Opcode_IsLastFourTrits()
    {
        // All 4-trit opcode instructions must have '+' as MST of opcode (mc[28])
        var standardMnemonics = new[] { "ADD.T X1, X2, X3", "ADDI.T X1, X2, X3", "BEQ.T X1, X2, 0", "ADD X1, X2, X3" };
        foreach (var mnemonic in standardMnemonics)
        {
            var mc = Asm.Translate(mnemonic);
            mc.Should().HaveLength(32, because: "REBEL-6 instructions are 32 trits");
            mc[28].Should().Be('+', because: $"'{mnemonic}' is a standard (4-trit opcode) instruction; opcode MST must be '+'");
        }
    }

    [Fact]
    public void LongImmediateInstructions_OpcodeMST_IsNotPlus()
    {
        // G/Y-type instructions: opcode has 2 trits; instruction[^4] ≠ '+'
        var longImmMnemonics = new[] { "LI.T X1, 1", "LWA.T X1, 1", "SWA.T X1, 1", "JAL.T X1, 1" };
        foreach (var mnemonic in longImmMnemonics)
        {
            var mc = Asm.Translate(mnemonic);
            mc[^4].Should().NotBe('+', because: $"'{mnemonic}' is a long-immediate instruction; mc[28] must not be '+'");
        }
    }

    [Fact]
    public void RType_AllDifferentFuncs_ProduceDifferentEncodings()
    {
        var ops = new[] { "ADD.T", "SUB.T", "SL.T", "SR.T", "SLT.T", "OR.T", "XOR.T", "AND.T" };
        var codes = ops.Select(op => Asm.Translate($"{op} X1, X2, X3")).ToList();
        codes.Should().OnlyHaveUniqueItems("R-type ternary ops share opcode but have distinct func values");
    }

    [Fact]
    public void BranchInstructions_BranchOffsetInRd2Slot()
    {
        // BEQ.T X1, X2, 1: offset=1 should be in rd2 slot (mc[18..24])
        var mc = Asm.Translate("BEQ.T X1, X2, 1");
        mc[18..24].Should().Be("00000+", because: "offset=1 in balanced ternary 6-trit is '00000+'");
    }

    [Fact]
    public void ITypeInstructions_ImmInRs2Slot()
    {
        // ADDI.T X1, X2, 1: immediate=1 should be in rs2 slot (mc[6..12])
        var mc = Asm.Translate("ADDI.T X1, X2, 1");
        mc[6..12].Should().Be("00000+", because: "immediate=1 in balanced ternary 6-trit is '00000+'");
    }

    [Fact]
    public void DType_AllFourOperandsEncoded()
    {
        // MAJV.T X1, X2, X3, X4: rd1=X1, rs1=X2, rs2=X3, rd2=X4
        var mc = Asm.Translate("MAJV.T X1, X2, X3, X4");
        mc[0..6].Should().Be("0000+-",   because: "rs1=X2");
        mc[6..12].Should().Be("0000+0",  because: "rs2=X3");
        mc[12..18].Should().Be("00000+", because: "rd1=X1");
        mc[18..24].Should().Be("0000++", because: "rd2=X4");
    }

    // =========================================================================
    // 7. Register encoding coverage
    // =========================================================================

    [Fact]
    public void LargeRegisterNumbers_EncodeCorrectly()
    {
        // X364 is the largest positive register; X-364 the most negative
        var mc = Asm.Translate("ADD.T X364, X-364, X0");
        mc[0..6].Should().Be("------",  because: "X-364 encodes as all-minus in 6-trit BT");
        mc[6..12].Should().Be("000000", because: "X0 encodes as all-zero");
        mc[12..18].Should().Be("++++++", because: "X364 encodes as all-plus in 6-trit BT");
    }

    [Fact]
    public void NegativeRegisterNames_SameEncodingAsPositive()
    {
        // X-1 and X1 are distinct registers; should produce different codes
        var withX1   = Asm.Translate("MV.T X0, X1");
        var withXm1  = Asm.Translate("MV.T X0, X-1");
        withX1.Should().NotBe(withXm1, because: "X1 and X-1 have different 6-trit encodings");
    }

    // =========================================================================
    // 8. Multi-instruction page assembly and address space
    // =========================================================================

    [Fact]
    public void AssembleInstructions_TwoInstructions_CorrectEncodingsAndAddresses()
    {
        const string source = """
            ADD.T X1, X2, X3
            NOP.T
            """;
        var result = Asm.AssembleInstructions(source);

        result.Should().HaveCount(2);
        result[0].MachineCode.Should().Be("0000+-0000+000000+00000000--++--");
        result[0].Address.Should().Be("------", because: "REBEL-6 address space starts at -364 = '------'");
        result[1].MachineCode.Should().Be("00000000000000000000000000--++-+");
        result[1].Address.Should().Be("-----0", because: "second address is -363 = '-----0'");
    }

    [Fact]
    public void AddressSpace_StartsAtNegative364_EndsAtPositive364()
    {
        Rebel6Assembler.AddressSpace.Should().HaveCount(729);
        Rebel6Assembler.AddressSpace[0].Should().Be("------");
        Rebel6Assembler.AddressSpace[364].Should().Be("000000");
        Rebel6Assembler.AddressSpace[728].Should().Be("++++++");
    }

    [Fact]
    public void AssembleInstructions_DoesNotPadPage()
    {
        var result = Asm.AssembleInstructions("ADD.T X1, X2, X3");
        result.Should().HaveCount(1, because: "page is not padded when padPage=false");
    }

    // =========================================================================
    // 9. Label resolution (PC-relative branch offsets)
    // =========================================================================

    [Fact]
    public void Labels_BranchOffset_IsRelativeToBranchInstruction()
    {
        const string source = """
            start:
            NOP.T
            BEQ.T X0, X0, start
            """;
        var result = Asm.AssembleInstructions(source);

        // BEQ is instruction index 1; start is index 0; offset = 0-1 = -1
        var beqMc = result[1].MachineCode;
        beqMc[18..24].Should().Be("00000-", because: "PC-relative offset -1 encodes as '00000-'");
    }

    [Fact]
    public void Labels_ForwardBranch_EncodesPositiveOffset()
    {
        const string source = """
            BEQ.T X0, X0, target
            NOP.T
            target:
            NOP.T
            """;
        var result = Asm.AssembleInstructions(source);

        // BEQ is at index 0; target is at index 2; offset = 2-0 = 2
        var beqMc = result[0].MachineCode;
        beqMc[18..24].Should().Be("0000+-", because: "PC-relative offset 2 encodes as '0000+-'");
    }

    [Fact]
    public void Labels_JalT_UsesLongImmediateOffset()
    {
        const string source = """
            start:
            NOP.T
            JAL.T X0, start
            """;
        var result = Asm.AssembleInstructions(source);

        // JAL.T is at index 1; start is at index 0; offset = 0-1 = -1
        // G-type: imm24 = -1 → "00000000000000000000000-"
        var jalMc = result[1].MachineCode;
        jalMc[..24].Should().Be("00000000000000000000000-", because: "G-type offset -1 in 24-trit BT");
    }

    // =========================================================================
    // 10. DisassemblePage
    // =========================================================================

    [Fact]
    public void DisassemblePage_MultipleCodes_ReturnsOnePerCode()
    {
        string[] codes =
        [
            "0000+-0000+000000+00000000--++--",  // ADD.T X1, X2, X3
            "00000000000000000000000000--++-+",  // NOP.T
            "00000000000000000000000+00000+00",  // LI.T X1, 1
        ];
        var result = Asm.DisassemblePage(codes);

        result.Should().HaveCount(3);
        result[0].Should().Be("ADD.T X1, X2, X3");
        result[1].Should().Be("NOP.T");
        result[2].Should().Be("LI.T X1, 1");
    }

    // =========================================================================
    // 11. Error cases
    // =========================================================================

    [Fact]
    public void Error_UnknownMnemonic_ThrowsInvalidOperationException()
    {
        var act = () => Asm.Translate("BOGUS.T X1, X2");
        act.Should().Throw<InvalidOperationException>().WithMessage("*BOGUS.T*");
    }

    [Fact]
    public void Error_WrongOperandCount_ThrowsInvalidOperationException()
    {
        var act = () => Asm.Translate("ADD.T X1, X2");  // ADD.T requires 3 operands
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_NopWithOperand_ThrowsInvalidOperationException()
    {
        var act = () => Asm.Translate("NOP.T X1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_BranchOffsetOutOfRange_ThrowsInvalidOperationException()
    {
        // 400 is outside 6-trit range [-364, +364]
        var act = () => Asm.Translate("BEQ.T X1, X2, 400");
        act.Should().Throw<InvalidOperationException>().WithMessage("*400*");
    }

    [Fact]
    public void Error_Disassemble_WrongLength_ThrowsInvalidOperationException()
    {
        var act = () => Asm.Disassemble("00000000000000000000");  // 20 trits, not 32
        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    [Fact]
    public void Error_Disassemble_InvalidCharacters_ThrowsInvalidOperationException()
    {
        var act = () => Asm.Disassemble(new string('x', 32));
        act.Should().Throw<InvalidOperationException>();
    }

    // =========================================================================
    // 12. Binary vs ternary instruction disambiguation
    // =========================================================================

    [Fact]
    public void TernaryAndBinaryAdd_ProduceDifferentOpcodes()
    {
        var ternaryAdd = Asm.Translate("ADD.T X1, X2, X3");
        var binaryAdd  = Asm.Translate("ADD X1, X2, X3");

        ternaryAdd[28..32].Should().Be("++--", because: "ADD.T uses ternary-base opcode ++--");
        binaryAdd[28..32].Should().Be("+0--",  because: "ADD uses binary-base opcode +0--");
        ternaryAdd.Should().NotBe(binaryAdd);
    }

    [Fact]
    public void BinarySystemInstructions_AllDataFieldsZero()
    {
        foreach (var mnemonic in new[] { "FENCE", "ECALL", "EBREAK" })
        {
            var mc = Asm.Translate(mnemonic);
            mc[..24].Should().Be(new string('0', 24),
                because: $"{mnemonic} is SYS-format: all register fields fixed to zero");
            mc[28..32].Should().Be("+0++", because: $"{mnemonic} uses binary-system opcode +0++");
        }
    }
}
