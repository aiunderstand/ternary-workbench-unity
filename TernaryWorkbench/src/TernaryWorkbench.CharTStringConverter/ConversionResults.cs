namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Represents a single encoding or decoding error encountered during
/// charT_u8 / charTC_u8 conversion.
/// </summary>
/// <param name="SymbolIndex">Zero-based index of the symbol (sequence) where the error occurred.</param>
/// <param name="TryteIndex">Zero-based index of the specific tryte within the input stream.</param>
/// <param name="RawInput">The raw tryte string(s) involved in the error.</param>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record ConversionError(
    int    SymbolIndex,
    int    TryteIndex,
    string RawInput,
    string Message);

/// <summary>Result of encoding a UTF-8 string to charT_u8 ternary.</summary>
/// <param name="EncodedText">
/// The encoded ternary string, symbols separated by newlines,
/// trytes within a symbol separated by spaces.
/// Empty string if input was empty.
/// </param>
/// <param name="Errors">
/// Any encoding errors encountered (e.g. unsupported code points).
/// An empty list indicates a fully successful encode.
/// </param>
public sealed record EncodeResult(
    string                        EncodedText,
    IReadOnlyList<ConversionError> Errors);

/// <summary>Result of decoding a charT_u8 ternary string to UTF-8.</summary>
/// <param name="DecodedText">
/// The decoded UTF-8 string. May be partial if errors occurred; the decoder
/// re-synchronizes after each error and continues.
/// </param>
/// <param name="Errors">
/// Any decoding errors (e.g. over-long encoding, unexpected continuation, invalid tryte).
/// An empty list indicates a fully successful decode.
/// </param>
public sealed record DecodeResult(
    string                        DecodedText,
    IReadOnlyList<ConversionError> Errors);

/// <summary>
/// Extension helpers for working with <see cref="ConversionError"/> collections.
/// </summary>
public static class ConversionErrorExtensions
{
    /// <summary>
    /// Collapses a list of errors by grouping consecutive and non-consecutive duplicates
    /// that share the same <see cref="ConversionError.Message"/>, preserving the order
    /// of first occurrence.  Returns a list of <c>(Message, Count)</c> tuples.
    /// </summary>
    /// <remarks>
    /// Useful for long inputs where the same error (e.g. "Unknown single-tryte pattern")
    /// would otherwise repeat once per offending symbol.
    /// </remarks>
    public static IReadOnlyList<(string Message, int Count)> CollapseRepeated(
        this IEnumerable<ConversionError> errors)
    {
        var seen  = new Dictionary<string, int>();           // message → list index
        var result = new List<(string Message, int Count)>();

        foreach (var error in errors)
        {
            string msg = error.Message;
            if (seen.TryGetValue(msg, out int idx))
            {
                var (m, c) = result[idx];
                result[idx] = (m, c + 1);
            }
            else
            {
                seen[msg] = result.Count;
                result.Add((msg, 1));
            }
        }

        return result;
    }
}
