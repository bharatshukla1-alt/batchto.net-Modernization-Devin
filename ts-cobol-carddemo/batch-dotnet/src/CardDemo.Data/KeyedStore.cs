using CardDemo.Domain;

namespace CardDemo.Data;

/// <summary>
/// VSAM KSDS equivalent: records ordered by primary key (EBCDIC/ASCII byte order of the key field),
/// optional non-unique alternate index (AIX/PATH). Changes are buffered and persisted on Commit().
/// </summary>
[Trace("IDCAMS DEFINE CLUSTER / ALTERNATEINDEX")]
public sealed class KeyedStore
{
    private readonly SortedDictionary<string, byte[]> _records = new(StringComparer.Ordinal);
    private readonly IKsdsPersistence _persistence;

    public KeyedStore(string dsn, RecordLayout layout, int keyOffset, int keyLength, IKsdsPersistence persistence, (int Offset, int Length)? altKey = null)
    {
        Dsn = dsn;
        Layout = layout;
        KeyOffset = keyOffset;
        KeyLength = keyLength;
        AltKey = altKey;
        _persistence = persistence;
        foreach (var rec in persistence.Load(dsn, layout))
        {
            _records[KeyOf(rec)] = rec;
        }
    }

    public string Dsn { get; }
    public RecordLayout Layout { get; }
    public int KeyOffset { get; }
    public int KeyLength { get; }
    public (int Offset, int Length)? AltKey { get; }
    public int Count => _records.Count;

    public string KeyOf(byte[] record) => CobolCodec.Text(record.AsSpan(KeyOffset, KeyLength));

    public bool TryRead(string key, out FixedRecord record)
    {
        var found = _records.TryGetValue(key.PadRight(KeyLength)[..KeyLength], out var bytes);
        record = new FixedRecord(Layout, found ? (byte[])bytes!.Clone() : null);
        return found;
    }

    /// <summary>READ ... KEY IS alternate-key: first record in primary-key order (see GAP-003).</summary>
    public bool TryReadAlternate(string altKey, out FixedRecord record)
    {
        var (off, len) = AltKey ?? throw new InvalidOperationException($"{Dsn} has no alternate index");
        foreach (var bytes in _records.Values)
        {
            if (CobolCodec.Text(bytes.AsSpan(off, len)) == altKey)
            {
                record = new FixedRecord(Layout, (byte[])bytes.Clone());
                return true;
            }
        }
        record = new FixedRecord(Layout);
        return false;
    }

    /// <summary>WRITE: false on duplicate key (file status 22).</summary>
    public bool Write(FixedRecord record)
    {
        var key = KeyOf(record.Bytes);
        if (_records.ContainsKey(key))
        {
            return false;
        }
        _records[key] = (byte[])record.Bytes.Clone();
        return true;
    }

    /// <summary>REWRITE: false when the key does not exist (INVALID KEY).</summary>
    public bool Rewrite(FixedRecord record)
    {
        var key = KeyOf(record.Bytes);
        if (!_records.ContainsKey(key))
        {
            return false;
        }
        _records[key] = (byte[])record.Bytes.Clone();
        return true;
    }

    public void Clear() => _records.Clear();

    public IEnumerable<FixedRecord> Scan() => _records.Values.Select(b => new FixedRecord(Layout, (byte[])b.Clone())).ToList();

    public void Commit() => _persistence.Save(Dsn, Layout, _records.Values);
}
