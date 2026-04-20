using System.Numerics;

namespace TernaryWorkbench.Core;

/// <summary>Symbol alphabets and digit-to-value mappings for each radix.</summary>
public static class Symbols
{
    // Balanced ternary uses -, 0, + representing -1, 0, +1
    public static readonly char[] Balanced3 = { '-', '0', '+' };

    // Base-27: digits 0-26 as 0-9, A-Q (D.W. Jones' Heptavintimal)
    public const string Base27Alphabet = "0123456789ABCDEFGHIJKLMNOPQ";

    // Base-64 RFC 4648: A-Z, a-z, 0-9, +, /  (64 symbols, A=0 .. /=63)
    public const string Base64Rfc4648Alphabet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    static Symbols()
    {
        if (Base27Alphabet.Length != 27)
            throw new InvalidOperationException($"Base27 alphabet has {Base27Alphabet.Length} chars, expected 27");
        if (Base64Rfc4648Alphabet.Length != 64)
            throw new InvalidOperationException($"Base64 RFC 4648 alphabet has {Base64Rfc4648Alphabet.Length} chars, expected 64");
    }

    public static int ValueOf27(char c)
    {
        int i = Base27Alphabet.IndexOf(c);
        if (i < 0) throw new FormatException($"Invalid base-27 digit: '{c}'");
        return i;
    }

    public static int ValueOf64(char c)
    {
        int i = Base64Rfc4648Alphabet.IndexOf(c);
        if (i < 0) throw new FormatException($"Invalid base-64 (RFC 4648) digit: '{c}'");
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
