using TernaryWorkbench.Core;

// -----------------------------------------------------------------------
// TernaryWorkbench CLI
//
// Usage: twb --from <radix> --to <radix> <value> [--lsd-first] [--bcd]
//
// Radix aliases (case-insensitive):
//   bin / base2           → Base2Unsigned
//   bin2c / base2c        → Base2Signed2C
//   ter / base3           → Base3Unbalanced
//   balanced / base3b     → Base3Balanced
//   base9                 → Base9Unbalanced
//   base27                → Base27Unbalanced
//   base81                → Base81Unbalanced
//   dec / base10          → Base10
// -----------------------------------------------------------------------

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

Radix? fromRadix = null, toRadix = null;
bool lsdFirst = false, bcd = false;
string? value = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--from":
            fromRadix = ParseRadix(NextArg(args, ref i, "--from"));
            break;
        case "--to":
            toRadix = ParseRadix(NextArg(args, ref i, "--to"));
            break;
        case "--lsd-first":
        case "--lsd":
            lsdFirst = true;
            break;
        case "--bcd":
            bcd = true;
            break;
        default:
            if (value is not null)
            {
                Error($"Unexpected argument: {args[i]}");
                return 1;
            }
            value = args[i];
            break;
    }
}

if (fromRadix is null || toRadix is null || value is null)
{
    Error("Required: --from <radix>, --to <radix>, and a value to convert.");
    PrintHelp();
    return 1;
}

try
{
    var opts = new OutputOptions(LsdFirst: lsdFirst, BcdEncoding: bcd);
    string result = RadixConverter.Convert(value, fromRadix.Value, toRadix.Value, opts);
    Console.WriteLine(result);
    return 0;
}
catch (FormatException ex)
{
    Error($"Conversion error: {ex.Message}");
    return 2;
}
catch (OverflowException ex)
{
    Error($"Overflow error: {ex.Message}");
    return 2;
}

// -----------------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------------

static string NextArg(string[] args, ref int i, string flag)
{
    if (++i >= args.Length)
    {
        Error($"Flag {flag} requires an argument.");
        Environment.Exit(1);
    }
    return args[i];
}

static Radix ParseRadix(string s) => s.ToLowerInvariant() switch
{
    "bin"      or "base2"     => Radix.Base2Unsigned,
    "bin2c"    or "base2c"    => Radix.Base2Signed2C,
    "ter"      or "base3"     => Radix.Base3Unbalanced,
    "balanced" or "base3b"    => Radix.Base3Balanced,
    "base9"                   => Radix.Base9Unbalanced,
    "base27"                  => Radix.Base27Unbalanced,
    "base81"                  => Radix.Base81Unbalanced,
    "dec"      or "base10"    => Radix.Base10,
    _ => throw new ArgumentException($"Unknown radix '{s}'. Run with --help for valid names.")
};

static void Error(string msg) => Console.Error.WriteLine($"Error: {msg}");

static void PrintHelp()
{
    Console.WriteLine("""
        TernaryWorkbench CLI — radix converter

        USAGE
          twb --from <radix> --to <radix> <value> [options]

        RADIX NAMES
          bin / base2       Binary (unsigned)
          bin2c / base2c    Binary (two's complement, signed)
          ter / base3       Ternary (unbalanced)
          balanced / base3b Balanced ternary (-, 0, +)
          base9             Nonary (base 9, unsigned)
          base27            Base 27 (0–9, A–Q)
          base81            Base 81
          dec / base10      Decimal (signed)

        OPTIONS
          --lsd-first       Output digits from least significant to most significant
          --bcd             BCD encoding: encode each decimal digit separately
                            (only useful with --from dec and a base-3/9/27/81 target)
          --help / -h       Show this help

        EXAMPLES
          twb --from dec --to balanced 42
          twb --from balanced --to dec "+---0"
          twb --from dec --to base3 --bcd 42
          twb --from bin --to dec 1010
        """);
}
