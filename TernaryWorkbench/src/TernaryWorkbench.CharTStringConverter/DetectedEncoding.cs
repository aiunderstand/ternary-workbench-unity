namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// Result of attempting to identify which charT encoding a ternary input uses.
/// </summary>
public enum DetectedEncoding
{
    /// <summary>The input decodes cleanly with <see cref="CharTu8Codec"/> but not with <see cref="CharTCu8Codec"/>.</summary>
    CharTU8,

    /// <summary>The input decodes cleanly with <see cref="CharTCu8Codec"/> but not with <see cref="CharTu8Codec"/>.</summary>
    CharTCU8,

    /// <summary>The input decodes cleanly with both codecs — the encoding is ambiguous.</summary>
    Both,

    /// <summary>The input fails to decode cleanly with either codec.</summary>
    Unknown,
}

/// <summary>
/// Extension helpers for <see cref="DetectedEncoding"/>.
/// </summary>
public static class DetectedEncodingExtensions
{
    /// <summary>Returns a human-readable display string for the detected encoding.</summary>
    public static string ToDisplayString(this DetectedEncoding encoding) => encoding switch
    {
        DetectedEncoding.CharTU8  => "charT_u8",
        DetectedEncoding.CharTCU8 => "charTC_u8",
        DetectedEncoding.Both     => "ambiguous (both codecs valid)",
        DetectedEncoding.Unknown  => "unknown",
        _                         => encoding.ToString(),
    };
}
