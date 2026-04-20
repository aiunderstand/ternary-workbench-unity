namespace TernaryWorkbench.Core;

public enum Radix
{
    Base2Unsigned,     // Binary (unsigned)
    Base2Signed1C,     // Binary (1's complement)
    Base2Signed2C,     // Binary (2's complement)
    Base8Unsigned,     // Octal
    Base16Unsigned,    // Hexadecimal
    Base64Rfc4648,     // Base-64 (RFC 4648)
    Base3Unbalanced,   // Ternary (unbalanced)
    Base3Signed2C,     // Ternary (2's complement) — diminished radix complement
    Base3Signed3C,     // Ternary (3's complement) — radix complement
    Base3Balanced,     // Ternary (balanced)
    Base9Unbalanced,   // Nonary (unbalanced)
    Base27Unbalanced,  // Heptavintimal (D.W. Jones)
    Base10,            // Decimal
}

public static class RadixExtensions
{
    public static string DisplayName(this Radix r) => r switch
    {
        Radix.Base2Unsigned    => "Binary (unsigned)",
        Radix.Base2Signed1C    => "Binary (1\u2019s complement)",
        Radix.Base2Signed2C    => "Binary (2\u2019s complement)",
        Radix.Base8Unsigned    => "Octal",
        Radix.Base16Unsigned   => "Hexadecimal",
        Radix.Base64Rfc4648    => "Base-64 (RFC 4648)",
        Radix.Base3Unbalanced  => "Ternary (unbalanced)",
        Radix.Base3Signed2C    => "Ternary (2\u2019s complement)",
        Radix.Base3Signed3C    => "Ternary (3\u2019s complement)",
        Radix.Base3Balanced    => "Ternary (balanced)",
        Radix.Base9Unbalanced  => "Nonary (unbalanced)",
        Radix.Base27Unbalanced => "Heptavintimal (D.W. Jones)",
        Radix.Base10           => "Decimal",
        _ => r.ToString()
    };

    /// <summary>True when BCT (Binary-Coded Ternary, BSD-PNX) output is supported.
    /// BCT encodes each balanced ternary trit as two bits: 10=+, 01=-, 11=0.</summary>
    public static bool SupportsBct(this Radix r) => r == Radix.Base3Balanced;
}
