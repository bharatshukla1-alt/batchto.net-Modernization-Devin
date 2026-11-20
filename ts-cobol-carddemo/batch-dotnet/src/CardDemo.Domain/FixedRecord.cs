namespace CardDemo.Domain;

/// <summary>A fixed-length record buffer addressed through its copybook layout (the COBOL record area).</summary>
public sealed class FixedRecord
{
    public FixedRecord(RecordLayout layout, byte[]? bytes = null)
    {
        Layout = layout;
        Bytes = new byte[layout.Length];
        if (bytes is null)
        {
            Array.Copy(layout.Initialize(), Bytes, layout.Length);
        }
        else
        {
            Array.Fill(Bytes, (byte)' ');
            Array.Copy(bytes, Bytes, Math.Min(bytes.Length, layout.Length));
        }
    }

    public RecordLayout Layout { get; }
    public byte[] Bytes { get; }

    public string Text(string field)
    {
        var f = Layout[field];
        return CobolCodec.Text(Bytes.AsSpan(f.Offset, f.Length));
    }

    public decimal Number(string field)
    {
        var f = Layout[field];
        return CobolCodec.Decode(Bytes.AsSpan(f.Offset, f.Length), f);
    }

    public FixedRecord Set(string field, string value)
    {
        var f = Layout[field];
        if (f.Kind != FieldKind.Text)
        {
            // MOVE alphanumeric to numeric: digits are right-justified and zero-filled.
            CobolCodec.Latin1.GetBytes(value.Trim().PadLeft(f.Length, '0')[^f.Length..], Bytes.AsSpan(f.Offset, f.Length));
            return this;
        }
        CobolCodec.Latin1.GetBytes(value.PadRight(f.Length)[..f.Length], Bytes.AsSpan(f.Offset, f.Length));
        return this;
    }

    public FixedRecord Set(string field, decimal value)
    {
        var f = Layout[field];
        CobolCodec.Encode(Bytes.AsSpan(f.Offset, f.Length), f, value);
        return this;
    }

    /// <summary>Raw bytes of a field as text - used for keys and group moves.</summary>
    public string Raw(string field) => Text(field);

    public string Slice(int offset, int length) => CobolCodec.Text(Bytes.AsSpan(offset, length));

    public override string ToString() => CobolCodec.Text(Bytes);
}
