namespace TernaryWorkbench.Core;

public enum Radix
{
    Base2Unsigned,
    Base2Signed2C,
    Base3Unbalanced,
    Base3Balanced,
    Base9Unbalanced,
    Base27Unbalanced,
    Base81Unbalanced,
    Base10,
}

public static class RadixExtensions
{
    public static string DisplayName(this Radix r) => r switch
    {
        Radix.Base2Unsigned   => "Binary (unsigned)",
        Radix.Base2Signed2C   => "Binary (two's complement)",
        Radix.Base3Unbalanced => "Ternary",
        Radix.Base3Balanced   => "Balanced ternary",
        Radix.Base9Unbalanced => "Nonary (base 9)",
        Radix.Base27Unbalanced => "Base 27",
        Radix.Base81Unbalanced => "Base 81",
        Radix.Base10          => "Decimal",
        _ => r.ToString()
    };

    /// <summary>True for the radices where BCD output makes sense (powers of 3 targets).</summary>
    public static bool SupportsBcd(this Radix r) => r is
        Radix.Base3Unbalanced or Radix.Base9Unbalanced or
        Radix.Base27Unbalanced or Radix.Base81Unbalanced;
}
