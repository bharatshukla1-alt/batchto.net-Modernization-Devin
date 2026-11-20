namespace CardDemo.Domain;

public enum FieldKind { Text, Zoned, Packed, Binary }

public sealed record FieldSpec(string Name, int Offset, int Length, FieldKind Kind, int Scale, bool Signed, int Digits)
{
    public bool IsFiller => Name.StartsWith("FILLER", StringComparison.Ordinal);
}

public sealed class RecordLayout
{
    private readonly Dictionary<string, FieldSpec> _byName;

    public RecordLayout(string copybook, int length, IReadOnlyList<FieldSpec> fields)
    {
        Copybook = copybook;
        Length = length;
        Fields = fields;
        _byName = fields.Where(f => !f.IsFiller).GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.First());
    }

    public string Copybook { get; }
    public int Length { get; }
    public IReadOnlyList<FieldSpec> Fields { get; }

    public FieldSpec this[string name] =>
        _byName.TryGetValue(name, out var f) ? f : throw new KeyNotFoundException($"{Copybook} has no field {name}");

    /// <summary>INITIALIZE semantics: text = spaces, numeric = zero.</summary>
    public byte[] Initialize()
    {
        var buf = Enumerable.Repeat((byte)' ', Length).ToArray();
        foreach (var f in Fields.Where(f => f.Kind != FieldKind.Text && !f.IsFiller))
        {
            CobolCodec.Encode(buf.AsSpan(f.Offset, f.Length), f, 0m);
        }
        return buf;
    }
}
