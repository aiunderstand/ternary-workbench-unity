using TernaryWorkbench.Core;

// -----------------------------------------------------------------------
// TernaryWorkbench CLI
//
// Usage: twb --from <radix> --to <radix> <value> [--lsd-first] [--bct]
//
// Radix aliases (case-insensitive):
//   bin / base2           → Base2Unsigned
//   bin1c / base2c1       → Base2Signed1C
//   bin2c / base2c        → Base2Signed2C
//   oct  / base8          → Base8Unsigned
//   hex  / base16         → Base16Unsigned
//   base64                → Base64Rfc4648
//   ter  / base3          → Base3Unbalanced
//   ter2c / base3c2       → Base3Signed2C
//   ter3c / base3c3       → Base3Signed3C
//   balanced / base3b     → Base3Balanced
//   bsdpnx / base3pnx     → Base3BsdPnx
//   base9                 → Base9Unbalanced
//   base27                → Base27Unbalanced
//   dec  / base10         → Base10
// -----------------------------------------------------------------------

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

Radix? fromRadix = null, toRadix = null;
bool lsdFirst = false, bct = false;
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
        case "--bct":
            bct = true;
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
    var opts = new OutputOptions(LsdFirst: lsdFirst, BctEncoding: bct);
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
    "bin1c"    or "base2c1"   => Radix.Base2Signed1C,
    "bin2c"    or "base2c"    => Radix.Base2Signed2C,
    "oct"      or "base8"     => Radix.Base8Unsigned,
    "hex"      or "base16"    => Radix.Base16Unsigned,
    "base64"                  => Radix.Base64Rfc4648,
    "ter"      or "base3"     => Radix.Base3Unbalanced,
    "ter2c"    or "base3c2"   => Radix.Base3Signed2C,
    "ter3c"    or "base3c3"   => Radix.Base3Signed3C,
    "balanced" or "base3b"    => Radix.Base3Balanced,
    "bsdpnx"   or "base3pnx"  => Radix.Base3BsdPnx,
    "base9"                   => Radix.Base9Unbalanced,
    "base27"                  => Radix.Base27Unbalanced,
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
          bin / base2         Binary (unsigned)
          bin1c / base2c1     Binary (1's complement)
          bin2c / base2c      Binary (2's complement)
          oct / base8         Octal
          hex / base16        Hexadecimal
          base64              Base-64 (RFC 4648)
          ter / base3         Ternary (unbalanced)
          ter2c / base3c2     Ternary (2's complement)
          ter3c / base3c3     Ternary (3's complement)
          balanced / base3b   Ternary (balanced)
          bsdpnx / base3pnx   Ternary (BSD-PNX)
          base9               Nonary (unbalanced)
          base27              Heptavintimal (D.W. Jones)
          dec / base10        Decimal (signed)

        OPTIONS
          --lsd-first         Output digits from least significant to most significant
          --bct               BCT encoding (BSD-PNX: 10=+, 01=-, 11=0)
                              Only meaningful with --to balanced
          --help / -h         Show this help

        EXAMPLES
          twb --from dec --to balanced 42
          twb --from balanced --to dec "+---0"
          twb --from dec --to balanced --bct 42
          twb --from bin --to dec 1010
          twb --from dec --to hex 255
        """);
}
