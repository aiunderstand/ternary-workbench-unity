using System.Numerics;

namespace TernaryWorkbench.Core;

/// <summary>Symbol alphabets and digit-to-value mappings for each radix.</summary>
public static class Symbols
{
    // Balanced ternary uses -, 0, + representing -1, 0, +1
    public static readonly char[] Balanced3 = { '-', '0', '+' };

    // Base-27: digits 0-26 as 0-9, A-Q
    public const string Base27Alphabet = "0123456789ABCDEFGHIJKLMNOPQ";

    // Base-81: digits 0-80 as 0-9, A-Z, a-z, then 19 special chars
    // Avoids + and - (used by balanced ternary), avoids / (path separator),
    // avoids ' " ` (quoting chars), avoids @ < > \ ^ (shell/regex specials).
    // Selected: ! # $ % & ( ) * _ ` { | } ~ [ ] ^ : ;
    public const string Base81Alphabet =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "!#$%&()*_`{|}~[]^:;";

    static Symbols()
    {
        // Validate lengths at startup
        if (Base27Alphabet.Length != 27)
            throw new InvalidOperationException($"Base27 alphabet has {Base27Alphabet.Length} chars, expected 27");
        if (Base81Alphabet.Length != 81)
            throw new InvalidOperationException($"Base81 alphabet has {Base81Alphabet.Length} chars, expected 81");
    }

    public static int ValueOf27(char c)
    {
        int i = Base27Alphabet.IndexOf(c);
        if (i < 0) throw new FormatException($"Invalid base-27 digit: '{c}'");
        return i;
    }

    public static int ValueOf81(char c)
    {
        int i = Base81Alphabet.IndexOf(c);
        if (i < 0) throw new FormatException($"Invalid base-81 digit: '{c}'");
        return i;
    }

    public static int ValueOfBalanced(char c) => c switch
    {
        '-' => -1,
        '0' => 0,
        '+' => 1,
        _ => throw new FormatException($"Invalid balanced ternary digit: '{c}'")
    };
}
