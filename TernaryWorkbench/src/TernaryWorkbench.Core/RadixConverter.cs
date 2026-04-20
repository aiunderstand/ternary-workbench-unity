using System.Numerics;
using System.Text;

namespace TernaryWorkbench.Core;

/// <summary>
/// Converts numeric strings between supported radices using BigInteger arithmetic.
/// All public methods are thread-safe (stateless).
/// </summary>
public static class RadixConverter
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Convert <paramref name="input"/> from <paramref name="from"/> to
    /// <paramref name="to"/> radix, applying <paramref name="options"/>.</summary>
    /// <exception cref="FormatException">Input is not valid for the source radix.</exception>
    public static string Convert(
        string input,
        Radix from,
        Radix to,
        OutputOptions? options = null)
    {
        options ??= OutputOptions.Default;
        input = input.Trim();

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // BCT path: encode balanced ternary trits as 2-bit BSD-PNX patterns
        if (options.BctEncoding && to == Radix.Base3Balanced)
            return ConvertBct(input, from, options);

        BigInteger value = Parse(input, from);
        return Format(value, to, options);
    }

    // -------------------------------------------------------------------------
    // Parsing
    // -------------------------------------------------------------------------

    public static BigInteger Parse(string input, Radix radix)
    {
        input = input.Trim();
        return radix switch
        {
            Radix.Base2Unsigned    => ParseUnsigned(input, 2),
            Radix.Base2Signed1C    => ParseBinary1C(input),
            Radix.Base2Signed2C    => ParseTwosComplement(input),
            Radix.Base8Unsigned    => ParseUnsigned(input, 8),
            Radix.Base16Unsigned   => ParseHex(input),
            Radix.Base64Rfc4648    => ParseBase64(input),
            Radix.Base3Unbalanced  => ParseUnsigned(input, 3),
            Radix.Base3Signed2C    => ParseTernary2C(input),
            Radix.Base3Signed3C    => ParseTernary3C(input),
            Radix.Base3Balanced    => ParseBalancedTernary(input),
            Radix.Base9Unbalanced  => ParseUnsigned(input, 9),
            Radix.Base27Unbalanced => ParseBase27(input),
            Radix.Base10           => ParseDecimal(input),
            _ => throw new NotSupportedException($"Unknown radix: {radix}")
        };
    }

    // -------------------------------------------------------------------------
    // Formatting
    // -------------------------------------------------------------------------

    public static string Format(BigInteger value, Radix radix, OutputOptions? options = null)
    {
        options ??= OutputOptions.Default;
        string result = radix switch
        {
            Radix.Base2Unsigned    => FormatUnsigned(value, 2),
            Radix.Base2Signed1C    => FormatBinary1C(value),
            Radix.Base2Signed2C    => FormatTwosComplement(value),
            Radix.Base8Unsigned    => FormatUnsigned(value, 8),
            Radix.Base16Unsigned   => FormatHex(value),
            Radix.Base64Rfc4648    => FormatBase64(value),
            Radix.Base3Unbalanced  => FormatUnsigned(value, 3),
            Radix.Base3Signed2C    => FormatTernary2C(value),
            Radix.Base3Signed3C    => FormatTernary3C(value),
            Radix.Base3Balanced    => FormatBalancedTernary(value),
            Radix.Base9Unbalanced  => FormatUnsigned(value, 9),
            Radix.Base27Unbalanced => FormatBase27(value),
            Radix.Base10           => value.ToString(),
            _ => throw new NotSupportedException($"Unknown radix: {radix}")
        };

        if (options.LsdFirst)
        {
            // For Base10, a leading '-' is a sign prefix, not a digit — keep it in front.
            bool hasMinus = result.StartsWith('-') && radix == Radix.Base10;
            string digits = hasMinus ? result[1..] : result;
            char[] arr = digits.ToCharArray();
            Array.Reverse(arr);
            result = hasMinus ? "-" + new string(arr) : new string(arr);
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // BCT encoding (Binary-Coded Ternary, BSD-PNX scheme)
    // Each balanced ternary trit is encoded as 2 bits: 10=+, 01=-, 11=0, 00=illegal.
    // -------------------------------------------------------------------------

    private static string ConvertBct(string input, Radix from, OutputOptions options)
    {
        BigInteger value = Parse(input, from);
        // Always format balanced ternary MSD-first; LSD-first applied below on bit-pairs.
        string bal = Format(value, Radix.Base3Balanced);

        var sb = new StringBuilder(bal.Length * 2);
        foreach (char c in bal)
        {
            sb.Append(c switch
            {
                '+' => "10",
                '0' => "11",
                '-' => "01",
                _   => throw new FormatException($"Unexpected balanced ternary character: '{c}'")
            });
        }

        string bct = sb.ToString();

        if (options.LsdFirst && bct.Length >= 2)
        {
            // Reverse the order of 2-bit trit encodings (keep each pair's bits intact)
            int numPairs = bct.Length / 2;
            char[] chars = new char[bct.Length];
            for (int i = 0; i < numPairs; i++)
            {
                chars[i * 2]     = bct[(numPairs - 1 - i) * 2];
                chars[i * 2 + 1] = bct[(numPairs - 1 - i) * 2 + 1];
            }
            bct = new string(chars);
        }

        return bct;
    }

    // -------------------------------------------------------------------------
    // Parse helpers
    // -------------------------------------------------------------------------

    private static BigInteger ParseUnsigned(string input, int @base)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        BigInteger b = new BigInteger(@base);
        foreach (char c in input)
        {
            int digit = c - '0';
            if (digit < 0 || digit >= @base)
                throw new FormatException($"Invalid base-{@base} digit: '{c}'");
            result = result * b + digit;
        }
        return result;
    }

    private static BigInteger ParseHex(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        foreach (char c in input)
        {
            int digit;
            if (c >= '0' && c <= '9')      digit = c - '0';
            else if (c >= 'A' && c <= 'F') digit = c - 'A' + 10;
            else if (c >= 'a' && c <= 'f') digit = c - 'a' + 10;
            else throw new FormatException($"Invalid hexadecimal digit: '{c}'");
            result = result * 16 + digit;
        }
        return result;
    }

    private static BigInteger ParseBase64(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        foreach (char c in input)
            result = result * 64 + Symbols.ValueOf64(c);
        return result;
    }

    /// <summary>Binary 1's complement (diminished radix complement).
    /// Leading bit 0 = positive, 1 = negative. All-ones = −0 = 0.</summary>
    private static BigInteger ParseBinary1C(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        foreach (char c in input)
            if (c != '0' && c != '1')
                throw new FormatException($"Invalid binary digit: '{c}'");

        bool negative = input[0] == '1';
        string remaining = input.Length > 1 ? input[1..] : string.Empty;

        if (!negative)
            return remaining.Length > 0 ? ParseUnsigned(remaining, 2) : BigInteger.Zero;

        // Negative: flip remaining bits and negate (-0 becomes 0)
        string flipped = FlipBits(remaining);
        return -(flipped.Length > 0 ? ParseUnsigned(flipped, 2) : BigInteger.Zero);
    }

    private static BigInteger ParseTwosComplement(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        if (input[0] != '0' && input[0] != '1')
            throw new FormatException($"Two's complement string must start with 0 or 1, got: '{input[0]}'");

        bool negative = input[0] == '1';
        int L = input.Length;

        if (!negative)
            return ParseUnsigned(L == 1 ? "0" : input[1..], 2);
        else
        {
            BigInteger raw = BigInteger.Zero;
            foreach (char c in input)
            {
                if (c != '0' && c != '1') throw new FormatException($"Invalid binary digit: '{c}'");
                raw = raw * 2 + (c - '0');
            }
            return raw - BigInteger.Pow(2, L);
        }
    }

    /// <summary>Ternary 2's complement (diminished radix complement).
    /// Leading trit 0 = positive, 2 = negative. All-twos = −0 = 0.</summary>
    private static BigInteger ParseTernary2C(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        char signChar = input[0];
        if (signChar != '0' && signChar != '2')
            throw new FormatException(
                $"Ternary 2\u2019s complement must start with 0 (positive) or 2 (negative), got: '{signChar}'");

        foreach (char c in input)
            if (c < '0' || c > '2')
                throw new FormatException($"Invalid ternary digit: '{c}'");

        string remaining = input.Length > 1 ? input[1..] : string.Empty;

        if (signChar == '0')
            return remaining.Length > 0 ? ParseUnsigned(remaining, 3) : BigInteger.Zero;

        // Negative: flip trits (0↔2, 1→1) and negate (-0 becomes 0)
        string flipped = FlipTrits(remaining);
        return -(flipped.Length > 0 ? ParseUnsigned(flipped, 3) : BigInteger.Zero);
    }

    /// <summary>Ternary 3's complement (radix complement).
    /// The sign trit s (0, 1, or 2) has weight −s·3^(n−1); remaining trits are positive.
    /// Leading trit 0 = non-negative. Leading trit 1 or 2 = negative.</summary>
    private static BigInteger ParseTernary3C(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        int signTrit = input[0] - '0';
        if (signTrit < 0 || signTrit > 2)
            throw new FormatException($"Invalid ternary digit: '{input[0]}'");

        string remaining = input.Length > 1 ? input[1..] : string.Empty;
        foreach (char c in remaining)
            if (c < '0' || c > '2')
                throw new FormatException($"Invalid ternary digit: '{c}'");

        BigInteger rest = remaining.Length > 0 ? ParseUnsigned(remaining, 3) : BigInteger.Zero;
        int L = remaining.Length;
        return rest - (BigInteger)signTrit * BigInteger.Pow(3, L);
    }

    private static BigInteger ParseBalancedTernary(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        foreach (char c in input)
            result = result * 3 + Symbols.ValueOfBalanced(c);
        return result;
    }

    private static BigInteger ParseBase27(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        foreach (char c in input)
            result = result * 27 + Symbols.ValueOf27(c);
        return result;
    }

    private static BigInteger ParseDecimal(string input)
    {
        if (!BigInteger.TryParse(input, out BigInteger value))
            throw new FormatException($"Invalid decimal number: '{input}'");
        return value;
    }

    // -------------------------------------------------------------------------
    // Format helpers
    // -------------------------------------------------------------------------

    private static string FormatUnsigned(BigInteger value, int @base)
    {
        if (value < 0)
            throw new OverflowException($"Cannot format negative value {value} as unsigned base-{@base}");
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger b = new BigInteger(@base);
        BigInteger v = value;
        while (v > 0)
        {
            digits.Add((char)('0' + (int)(v % b)));
            v /= b;
        }
        digits.Reverse();
        return new string(digits.ToArray());
    }

    private static string FormatHex(BigInteger value)
    {
        if (value < 0)
            throw new OverflowException($"Cannot format negative value {value} as hexadecimal");
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger v = value;
        while (v > 0)
        {
            int d = (int)(v % 16);
            digits.Add(d < 10 ? (char)('0' + d) : (char)('A' + d - 10));
            v /= 16;
        }
        digits.Reverse();
        return new string(digits.ToArray());
    }

    private static string FormatBase64(BigInteger value)
    {
        if (value < 0)
            throw new OverflowException($"Cannot format negative value {value} as base-64");
        if (value == BigInteger.Zero) return "A";  // 'A' is digit 0 in RFC 4648 alphabet

        var digits = new List<char>();
        BigInteger v = value;
        while (v > 0)
        {
            digits.Add(Symbols.Base64Rfc4648Alphabet[(int)(v % 64)]);
            v /= 64;
        }
        digits.Reverse();
        return new string(digits.ToArray());
    }

    /// <summary>Binary 1's complement. Positive: "0" + bits. Negative: "1" + flipped bits.</summary>
    private static string FormatBinary1C(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        if (value > 0)
        {
            int L = (int)value.GetBitLength();
            string bits = FormatUnsigned(value, 2).PadLeft(L, '0');
            return "0" + bits;
        }
        else
        {
            BigInteger absVal = BigInteger.Abs(value);
            int L = (int)absVal.GetBitLength();
            string bits = FormatUnsigned(absVal, 2).PadLeft(L, '0');
            return "1" + FlipBits(bits);
        }
    }

    private static string FormatTwosComplement(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        if (value > 0)
        {
            int L = (int)BigInteger.Log(value, 2) + 1;
            string bits = FormatUnsigned(value, 2).PadLeft(L, '0');
            return "0" + bits;
        }
        else
        {
            BigInteger absVal = BigInteger.Abs(value);
            int L = (int)BigInteger.Log(absVal, 2) + 1;
            BigInteger rest = value + BigInteger.Pow(2, L);
            string bits = FormatUnsigned(rest, 2).PadLeft(L, '0');
            return "1" + bits;
        }
    }

    /// <summary>Ternary 2's complement. Positive: "0" + trits. Negative: "2" + flipped trits.</summary>
    private static string FormatTernary2C(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        if (value > 0)
            return "0" + FormatUnsigned(value, 3);
        else
        {
            BigInteger absVal = BigInteger.Abs(value);
            return "2" + FlipTrits(FormatUnsigned(absVal, 3));
        }
    }

    /// <summary>Ternary 3's complement. Positive: "0" + trits.
    /// Negative x: "1" + (3^L + x) formatted as L trits, where L is the
    /// smallest integer such that 3^L ≥ |x|.</summary>
    private static string FormatTernary3C(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        if (value > 0)
            return "0" + FormatUnsigned(value, 3);
        else
        {
            BigInteger absVal = BigInteger.Abs(value);
            // Find smallest L such that 3^L >= absVal
            int L = 0;
            BigInteger pow3L = BigInteger.One;
            while (pow3L < absVal) { pow3L *= 3; L++; }
            BigInteger rest = value + pow3L;  // = 3^L - absVal >= 0
            string s = rest > BigInteger.Zero
                ? FormatUnsigned(rest, 3).PadLeft(L, '0')
                : new string('0', L);
            return "1" + s;
        }
    }

    private static string FormatBalancedTernary(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger v = value;

        while (v != BigInteger.Zero)
        {
            BigInteger rem = ((v % 3) + 3) % 3;
            if (rem == 0)
            {
                digits.Add('0');
                v /= 3;
            }
            else if (rem == 1)
            {
                digits.Add('+');
                v = (v - 1) / 3;
            }
            else // rem == 2, represents -1
            {
                digits.Add('-');
                v = (v + 1) / 3;
            }
        }

        digits.Reverse();
        return new string(digits.ToArray());
    }

    private static string FormatBase27(BigInteger value)
    {
        if (value < 0) throw new OverflowException($"Cannot format negative value {value} as base-27");
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger v = value;
        while (v > 0)
        {
            digits.Add(Symbols.Base27Alphabet[(int)(v % 27)]);
            v /= 27;
        }
        digits.Reverse();
        return new string(digits.ToArray());
    }

    // -------------------------------------------------------------------------
    // Bit/trit flip helpers
    // -------------------------------------------------------------------------

    private static string FlipBits(string bits)
    {
        char[] arr = bits.ToCharArray();
        for (int i = 0; i < arr.Length; i++)
            arr[i] = arr[i] == '0' ? '1' : '0';
        return new string(arr);
    }

    private static string FlipTrits(string trits)
    {
        char[] arr = trits.ToCharArray();
        for (int i = 0; i < arr.Length; i++)
            arr[i] = (char)('2' - (arr[i] - '0')); // 0→2, 1→1, 2→0
        return new string(arr);
    }
}
