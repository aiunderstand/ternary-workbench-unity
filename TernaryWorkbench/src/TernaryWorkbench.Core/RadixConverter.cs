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

        // BCD path: handle each decimal digit individually then concatenate
        if (options.BcdEncoding && from == Radix.Base10 && to.SupportsBcd())
            return ConvertBcd(input, to, options);

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
            Radix.Base2Unsigned   => ParseUnsigned(input, 2),
            Radix.Base2Signed2C   => ParseTwosComplement(input),
            Radix.Base3Unbalanced => ParseUnsigned(input, 3),
            Radix.Base3Balanced   => ParseBalancedTernary(input),
            Radix.Base9Unbalanced => ParseUnsigned(input, 9),
            Radix.Base27Unbalanced => ParseBase27(input),
            Radix.Base81Unbalanced => ParseBase81(input),
            Radix.Base10          => ParseDecimal(input),
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
            Radix.Base2Unsigned   => FormatUnsigned(value, 2),
            Radix.Base2Signed2C   => FormatTwosComplement(value),
            Radix.Base3Unbalanced => FormatUnsigned(value, 3),
            Radix.Base3Balanced   => FormatBalancedTernary(value),
            Radix.Base9Unbalanced => FormatUnsigned(value, 9),
            Radix.Base27Unbalanced => FormatBase27(value),
            Radix.Base81Unbalanced => FormatBase81(value),
            Radix.Base10          => value.ToString(),
            _ => throw new NotSupportedException($"Unknown radix: {radix}")
        };

        if (options.LsdFirst)
        {
            // For Base10, a leading '-' is a sign prefix, not a digit — keep it in front.
            // For all other radices the leading character is always a digit.
            bool hasMinus = result.StartsWith('-') && radix == Radix.Base10;
            string digits = hasMinus ? result[1..] : result;
            char[] arr = digits.ToCharArray();
            Array.Reverse(arr);
            result = hasMinus ? "-" + new string(arr) : new string(arr);
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // BCD encoding
    // -------------------------------------------------------------------------

    /// <summary>Encode each decimal digit of <paramref name="decInput"/> individually.</summary>
    private static string ConvertBcd(string decInput, Radix to, OutputOptions options)
    {
        // Accept optional leading minus; encode absolute digits then prefix '-'
        bool negative = decInput.StartsWith('-');
        string digits = negative ? decInput[1..] : decInput;

        if (string.IsNullOrEmpty(digits) || !digits.All(char.IsAsciiDigit))
            throw new FormatException($"BCD input must be a decimal string, got: '{decInput}'");

        // Width per digit in the target radix
        int digitWidth = to switch
        {
            Radix.Base3Unbalanced  => 3,  // each 0-9 needs up to 3 base-3 digits
            Radix.Base9Unbalanced  => 2,  // each 0-9 needs up to 2 base-9 digits
            Radix.Base27Unbalanced => 1,  // 0-9 fits in 1 base-27 digit
            Radix.Base81Unbalanced => 1,  // 0-9 fits in 1 base-81 digit
            _ => throw new NotSupportedException($"BCD not supported for {to}")
        };

        var sb = new StringBuilder();
        if (negative) sb.Append('-');

        bool lsd = options.LsdFirst;
        if (lsd)
        {
            // For LSD-first we process digit positions right-to-left,
            // and also reverse each encoded digit group
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i] - '0';
                string encoded = FormatFixedWidth(new BigInteger(d), to, digitWidth);
                // Reverse within this group
                char[] group = encoded.ToCharArray();
                Array.Reverse(group);
                sb.Append(group);
            }
        }
        else
        {
            foreach (char c in digits)
            {
                int d = c - '0';
                string encoded = FormatFixedWidth(new BigInteger(d), to, digitWidth);
                sb.Append(encoded);
            }
        }

        return sb.ToString();
    }

    /// <summary>Format <paramref name="value"/> in <paramref name="radix"/> with exactly
    /// <paramref name="width"/> digits (zero-padded on the left).</summary>
    private static string FormatFixedWidth(BigInteger value, Radix radix, int width)
    {
        string s = Format(value, radix, OutputOptions.Default);
        return s.PadLeft(width, '0');
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

    private static BigInteger ParseTwosComplement(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        if (input[0] != '0' && input[0] != '1')
            throw new FormatException($"Two's complement string must start with 0 or 1, got: '{input[0]}'");

        bool negative = input[0] == '1';
        int L = input.Length;

        if (!negative)
        {
            // Positive: drop the leading 0 and parse as unsigned binary
            return ParseUnsigned(L == 1 ? "0" : input[1..], 2);
        }
        else
        {
            // Negative: value = parsed_as_unsigned - 2^L
            BigInteger raw = BigInteger.Zero;
            foreach (char c in input)
            {
                if (c != '0' && c != '1') throw new FormatException($"Invalid binary digit: '{c}'");
                raw = raw * 2 + (c - '0');
            }
            return raw - BigInteger.Pow(2, L);
        }
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

    private static BigInteger ParseBase81(string input)
    {
        if (input.Length == 0) throw new FormatException("Empty input");
        BigInteger result = BigInteger.Zero;
        foreach (char c in input)
            result = result * 81 + Symbols.ValueOf81(c);
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

    private static string FormatTwosComplement(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        if (value > 0)
        {
            // Positive: prepend sign bit 0, then value in binary
            int L = (int)BigInteger.Log(value, 2) + 1;  // bit length of value
            string bits = FormatUnsigned(value, 2).PadLeft(L, '0');
            return "0" + bits;
        }
        else
        {
            // Negative: find L such that -2^(L-1) represents this value without overflow
            // L is the number of bits needed to represent |value| = bit-length of |value|
            BigInteger absVal = BigInteger.Abs(value);
            int L = (int)BigInteger.Log(absVal, 2) + 1;
            BigInteger rest = value + BigInteger.Pow(2, L);
            string bits = FormatUnsigned(rest, 2).PadLeft(L, '0');
            return "1" + bits;
        }
    }

    private static string FormatBalancedTernary(BigInteger value)
    {
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger v = value;

        while (v != BigInteger.Zero)
        {
            // rem ∈ {0, 1, 2}
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

    private static string FormatBase81(BigInteger value)
    {
        if (value < 0) throw new OverflowException($"Cannot format negative value {value} as base-81");
        if (value == BigInteger.Zero) return "0";

        var digits = new List<char>();
        BigInteger v = value;
        while (v > 0)
        {
            digits.Add(Symbols.Base81Alphabet[(int)(v % 81)]);
            v /= 81;
        }
        digits.Reverse();
        return new string(digits.ToArray());
    }
}
