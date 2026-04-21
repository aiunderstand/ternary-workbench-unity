using System.Text;

namespace TernaryWorkbench.CharTStringConverter;

/// <summary>
/// An immutable 6-trit balanced ternary word (one tryte).
/// Trits are stored MST-first (index 0 = most significant).
/// </summary>
public readonly struct Tryte : IEquatable<Tryte>
{
    private readonly Trit[] _trits; // always length 6, never null after construction

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Number of trits in one tryte.</summary>
    public const int Length = 6;

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    /// <summary>Constructs a Tryte from six trits, MST first.</summary>
    /// <exception cref="ArgumentException">Array length ≠ 6.</exception>
    public Tryte(Trit[] trits)
    {
        if (trits is null || trits.Length != Length)
            throw new ArgumentException($"A tryte requires exactly {Length} trits.", nameof(trits));
        _trits = (Trit[])trits.Clone();
    }

    /// <summary>Constructs a Tryte from six explicit trits.</summary>
    public Tryte(Trit t0, Trit t1, Trit t2, Trit t3, Trit t4, Trit t5)
        : this(new[] { t0, t1, t2, t3, t4, t5 }) { }

    // -------------------------------------------------------------------------
    // Indexer
    // -------------------------------------------------------------------------

    /// <summary>Returns the trit at position <paramref name="index"/> (0 = MST).</summary>
    public Trit this[int index]
    {
        get
        {
            if (_trits is null) return Trit.Minus; // default(Tryte) → all minus
            if ((uint)index >= Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0,{Length - 1}].");
            return _trits[index];
        }
    }

    // -------------------------------------------------------------------------
    // Parsing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a compact 6-character string (e.g. "+-0---") or a spaced
    /// 11-character string (e.g. "+ - 0 - - -") into a <see cref="Tryte"/>.
    /// </summary>
    /// <exception cref="FormatException">Input is not a valid tryte string.</exception>
    public static Tryte Parse(string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        s = s.Trim();

        // Strip internal spaces: accept "+-0---" and "+ - 0 - - -"
        string compact = s.Replace(" ", "");
        if (compact.Length != Length)
            throw new FormatException(
                $"A tryte string must contain exactly {Length} trit characters (spaces are ignored); got {compact.Length} in '{s}'.");

        var trits = new Trit[Length];
        for (int i = 0; i < Length; i++)
            trits[i] = TritHelper.Parse(compact[i]);

        return new Tryte(trits);
    }

    /// <summary>
    /// Tries to parse a tryte string; returns false and sets <paramref name="tryte"/>
    /// to default if parsing fails.
    /// </summary>
    public static bool TryParse(string? s, out Tryte tryte)
    {
        try
        {
            if (s is null) { tryte = default; return false; }
            tryte = Parse(s);
            return true;
        }
        catch
        {
            tryte = default;
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Serialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the canonical compact string representation, e.g. "+-0---".
    /// When <paramref name="spaces"/> is true, returns a spaced form "+-0---" → "+ - 0 - - -".
    /// </summary>
    public string ToBalancedString(bool spaces = false)
    {
        var trits = _trits ?? new Trit[Length]; // default(Tryte) → all Minus
        if (!spaces)
        {
            var sb = new StringBuilder(Length);
            foreach (var t in trits) sb.Append(TritHelper.ToChar(t));
            return sb.ToString();
        }
        else
        {
            var parts = new string[Length];
            for (int i = 0; i < Length; i++)
                parts[i] = TritHelper.ToChar(trits[i]).ToString();
            return string.Join(" ", parts);
        }
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The balanced sum of all six trit values (each in {−1, 0, +1}).
    /// Range: [−6, +6].
    /// </summary>
    public int BalancedSum
    {
        get
        {
            var trits = _trits ?? new Trit[Length];
            int sum = 0;
            foreach (var t in trits) sum += (int)t;
            return sum;
        }
    }

    /// <summary>
    /// Returns the structural role of this tryte within a charT_u8 / charTC_u8 sequence,
    /// determined solely by the leading trit(s).
    /// </summary>
    public TryteRole GetRole()
    {
        Trit t0 = this[0];
        Trit t1 = this[1];
        Trit t2 = this[2];
        Trit t3 = this[3];

        // Continuation trytes
        if (t0 == Trit.Minus) return TryteRole.ContinuationFinal;
        if (t0 == Trit.Zero && t1 == Trit.Zero) return TryteRole.ContinuationMiddle2;
        if (t0 == Trit.Zero) return TryteRole.ContinuationMiddle1;

        // t0 == Plus from here on
        if (t1 == Trit.Minus) return TryteRole.Lead2;
        if (t1 == Trit.Plus && t2 == Trit.Minus) return TryteRole.Lead3;
        if (t1 == Trit.Plus && t2 == Trit.Plus && t3 == Trit.Minus) return TryteRole.Lead4;

        // All remaining Plus-leading patterns are single trytes
        return TryteRole.SingleTryte;
    }

    // -------------------------------------------------------------------------
    // Equality
    // -------------------------------------------------------------------------

    public bool Equals(Tryte other)
    {
        for (int i = 0; i < Length; i++)
            if (this[i] != other[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Tryte t && Equals(t);

    public override int GetHashCode()
    {
        var h = new HashCode();
        for (int i = 0; i < Length; i++) h.Add(this[i]);
        return h.ToHashCode();
    }

    public static bool operator ==(Tryte left, Tryte right) => left.Equals(right);
    public static bool operator !=(Tryte left, Tryte right) => !left.Equals(right);

    public override string ToString() => ToBalancedString();
}
