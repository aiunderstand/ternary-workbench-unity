namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// A single balanced ternary digit with value −1, 0, or +1,
/// represented by the characters '−', '0', and '+'.
/// </summary>
public enum Trit
{
    Minus = -1,
    Zero  =  0,
    Plus  =  1,
}

/// <summary>Stateless helpers for converting between <see cref="Trit"/> values and characters.</summary>
public static class TritHelper
{
    /// <summary>
    /// Parses a single character ('−', '0', or '+') into a <see cref="Trit"/>.
    /// </summary>
    /// <exception cref="FormatException">Character is not a valid trit symbol.</exception>
    public static Trit Parse(char c) => c switch
    {
        '-' => Trit.Minus,
        '0' => Trit.Zero,
        '+' => Trit.Plus,
        _   => throw new FormatException($"Invalid trit character: '{c}'. Expected '-', '0', or '+'.")
    };

    /// <summary>Returns the canonical character representation of a trit.</summary>
    public static char ToChar(Trit t) => t switch
    {
        Trit.Minus => '-',
        Trit.Zero  => '0',
        Trit.Plus  => '+',
        _          => throw new ArgumentOutOfRangeException(nameof(t), t, "Unknown Trit value.")
    };

    /// <summary>
    /// Maps a trit to its positional digit value (0, 1, or 2) used when
    /// computing a code-point number from payload trits.
    /// Minus → 0, Zero → 1, Plus → 2.
    /// </summary>
    public static int ToPositionalDigit(Trit t) => t switch
    {
        Trit.Minus => 0,
        Trit.Zero  => 1,
        Trit.Plus  => 2,
        _          => throw new ArgumentOutOfRangeException(nameof(t), t, "Unknown Trit value.")
    };

    /// <summary>
    /// Maps a positional digit (0, 1, or 2) back to a <see cref="Trit"/>.
    /// </summary>
    public static Trit FromPositionalDigit(int digit) => digit switch
    {
        0 => Trit.Minus,
        1 => Trit.Zero,
        2 => Trit.Plus,
        _ => throw new ArgumentOutOfRangeException(nameof(digit), digit, "Positional digit must be 0, 1, or 2.")
    };
}
