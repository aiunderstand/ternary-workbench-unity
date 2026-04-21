namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// The structural role of a tryte within a charT_u8 or charTC_u8 encoded sequence,
/// determined entirely by the leading trit(s).
/// </summary>
public enum TryteRole
{
    /// <summary>
    /// A complete single-tryte symbol.
    /// Leading pattern: '+' where the remaining trits do NOT form a multi-tryte lead prefix.
    /// </summary>
    SingleTryte,

    /// <summary>
    /// Lead tryte of a 2-tryte sequence.
    /// Leading pattern: '+ -' (trits 0–1 = Plus, Minus).
    /// </summary>
    Lead2,

    /// <summary>
    /// Lead tryte of a 3-tryte sequence.
    /// Leading pattern: '+ + -' (trits 0–2 = Plus, Plus, Minus).
    /// </summary>
    Lead3,

    /// <summary>
    /// Lead tryte of a 4-tryte sequence.
    /// Leading pattern: '+ + + -' (trits 0–3 = Plus, Plus, Plus, Minus).
    /// </summary>
    Lead4,

    /// <summary>
    /// Final continuation tryte (last in any multi-tryte sequence).
    /// Leading pattern: '-' (trit 0 = Minus).
    /// Carries 5 payload trits.
    /// </summary>
    ContinuationFinal,

    /// <summary>
    /// Middle continuation tryte carrying 5 payload trits (for 3- and 4-tryte sequences).
    /// Leading pattern: '0 [non-zero]' (trit 0 = Zero, trit 1 ≠ Zero).
    /// </summary>
    ContinuationMiddle1,

    /// <summary>
    /// Middle continuation tryte carrying 4 payload trits (for 4-tryte sequences).
    /// Leading pattern: '0 0' (trits 0–1 = Zero, Zero).
    /// </summary>
    ContinuationMiddle2,

    /// <summary>
    /// The tryte pattern does not correspond to any valid role.
    /// This should not occur in well-formed input.
    /// </summary>
    Invalid,
}
