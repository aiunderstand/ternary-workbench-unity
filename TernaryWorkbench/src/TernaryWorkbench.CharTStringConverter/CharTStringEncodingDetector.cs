namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Attempts to identify which charT string encoding a ternary input uses by
/// decoding it with both <see cref="CharTu8Codec"/> and <see cref="CharTCu8Codec"/>
/// and observing which succeeds without errors.
/// </summary>
/// <remarks>
/// The heuristic is conservative: a codec is considered to have "succeeded" only
/// when its error list is completely empty.  CRC warnings from <see cref="CharTCu8Codec"/>
/// are treated as errors here (non-zero error count), so a corrupted charTC_u8
/// stream does not falsely report as <see cref="DetectedEncoding.CharTCU8"/>.
/// </remarks>
public static class CharTStringEncodingDetector
{
    /// <summary>
    /// Detects the most likely charT encoding for the given ternary input.
    /// </summary>
    /// <param name="ternaryInput">
    /// Ternary input string in the same format accepted by the two codecs
    /// (symbols on separate lines or separated by <c>|</c>; trytes within
    /// a symbol space-separated or concatenated).
    /// </param>
    /// <returns>
    /// <see cref="DetectedEncoding.Unknown"/>   if the input is null/whitespace or both codecs fail.<br/>
    /// <see cref="DetectedEncoding.CharTU8"/>   if only <see cref="CharTu8Codec"/> succeeds.<br/>
    /// <see cref="DetectedEncoding.CharTCU8"/>  if only <see cref="CharTCu8Codec"/> succeeds.<br/>
    /// <see cref="DetectedEncoding.Both"/>      if both codecs succeed (ambiguous input).
    /// </returns>
    public static DetectedEncoding Detect(string? ternaryInput)
    {
        if (string.IsNullOrWhiteSpace(ternaryInput))
            return DetectedEncoding.Unknown;

        bool u8Ok  = CharTu8Codec.Decode(ternaryInput).Errors.Count  == 0;
        bool ucOk  = CharTCu8Codec.Decode(ternaryInput).Errors.Count == 0;

        return (ucOk, u8Ok) switch
        {
            (true,  true)  => DetectedEncoding.Both,
            (true,  false) => DetectedEncoding.CharTCU8,
            (false, true)  => DetectedEncoding.CharTU8,
            _              => DetectedEncoding.Unknown,
        };
    }
}
