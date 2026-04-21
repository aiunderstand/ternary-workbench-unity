namespace TernaryWorkbench.Core;

/// <summary>Controls formatting of the output string.</summary>
/// <param name="LsdFirst">Emit least-significant digit first (default: MSD first).</param>
/// <param name="BctEncoding">Encode each balanced ternary trit as a 2-bit BSD-PNX pattern
/// (10=+, 01=-, 11=0). Only meaningful when the target radix is Base3Balanced.</param>
/// <param name="FixedInputLength">When set, the input must not exceed this many digits
/// (trits for ternary radices, bits for binary radices). Throws <see cref="OverflowException"/>
/// if the input is longer.</param>
/// <param name="FixedOutputLength">When set, the output is padded to exactly this many digits
/// and throws <see cref="OverflowException"/> if more digits are required.</param>
public record OutputOptions(
    bool LsdFirst = false,
    bool BctEncoding = false,
    int? FixedInputLength = null,
    int? FixedOutputLength = null)
{
    public static readonly OutputOptions Default = new();
}
