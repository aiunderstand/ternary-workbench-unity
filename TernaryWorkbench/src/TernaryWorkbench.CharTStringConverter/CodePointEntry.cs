namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// A single entry in a charT_u8 or charTC_u8 code-point table.
/// </summary>
/// <param name="CodePoint">Ternary code-point number (0-based, ascending from all-Minus payload).</param>
/// <param name="TrytePattern">Canonical 6-character tryte string, e.g. "+0----".</param>
/// <param name="UnicodeCodePoint">
/// Corresponding Unicode scalar value, or null if this code point has no Unicode equivalent.
/// </param>
/// <param name="Description">Short human-readable description of the character.</param>
public sealed record CodePointEntry(
    int     CodePoint,
    string  TrytePattern,
    int?    UnicodeCodePoint,
    string  Description);
