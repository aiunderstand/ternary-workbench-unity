namespace TernaryWorkbench.Core;

/// <summary>
/// Represents a single completed conversion, used both for the history panel
/// and as the unit of serialisation in the CSV export/import format.
/// </summary>
/// <param name="Input">The original input string as entered by the user.</param>
/// <param name="Output">The converted output string.</param>
/// <param name="FromRadix">The source radix.</param>
/// <param name="ToRadix">The target radix.</param>
/// <param name="LsdFirst">
/// <see langword="true"/> when the output was emitted least-significant digit first (LSD);
/// <see langword="false"/> for the default most-significant digit first (MSD).
/// </param>
/// <param name="FixedOutputLength">
/// The fixed output word length that was applied, or <see langword="null"/> when
/// variable-length output was used.
/// </param>
public record ConversionRecord(
    string Input,
    string Output,
    Radix FromRadix,
    Radix ToRadix,
    bool LsdFirst,
    int? FixedOutputLength);
