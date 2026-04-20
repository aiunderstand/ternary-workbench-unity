namespace TernaryWorkbench.Core;

/// <summary>Controls formatting of the output string.</summary>
/// <param name="LsdFirst">Emit least-significant digit first (default: MSD first).</param>
/// <param name="BctEncoding">Encode each balanced ternary trit as a 2-bit BSD-PNX pattern
/// (10=+, 01=-, 11=0). Only meaningful when the target radix is Base3Balanced.</param>
public record OutputOptions(bool LsdFirst = false, bool BctEncoding = false)
{
    public static readonly OutputOptions Default = new();
}
