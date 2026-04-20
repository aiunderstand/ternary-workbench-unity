namespace TernaryWorkbench.Core;

/// <summary>Controls formatting of the output string.</summary>
/// <param name="LsdFirst">Emit least-significant digit first (default: MSD first).</param>
/// <param name="BcdEncoding">Encode each input decimal digit separately in the target radix
/// (only meaningful when the source is Base10 and the target is a power-of-3 base).</param>
public record OutputOptions(bool LsdFirst = false, bool BcdEncoding = false)
{
    public static readonly OutputOptions Default = new();
}
