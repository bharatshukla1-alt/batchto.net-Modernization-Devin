using CardDemo.Domain;

namespace CardDemo.Data;

/// <summary>QSAM fixed-block dataset stored as text lines (records right-padded to LRECL on read, trailing blanks stripped on write).</summary>
public static class SequentialFile
{
    public static List<byte[]> Read(string path, int lrecl)
    {
        if (!File.Exists(path))
        {
            return [];
        }
        var data = File.ReadAllBytes(path);
        var records = new List<byte[]>();
        var start = 0;
        for (var i = 0; i <= data.Length; i++)
        {
            if (i == data.Length && start == data.Length)
            {
                break;
            }
            if (i == data.Length || data[i] == (byte)'\n')
            {
                var rec = Enumerable.Repeat((byte)' ', lrecl).ToArray();
                var end = i > start && data[i - 1] == (byte)'\r' ? i - 1 : i; // CRLF lines, as tools/parity/record.py strips them
                Array.Copy(data, start, rec, 0, Math.Min(end - start, lrecl));
                records.Add(rec);
                start = i + 1;
            }
        }
        return records;
    }

    public static void Write(string path, IEnumerable<byte[]> records, bool append)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, append ? FileMode.Append : FileMode.Create);
        fs.Write(Encode(records));
        fs.Flush(flushToDisk: true);
    }

    public static byte[] Encode(IEnumerable<byte[]> records)
    {
        using var ms = new MemoryStream();
        foreach (var rec in records)
        {
            var len = rec.Length;
            while (len > 0 && rec[len - 1] == (byte)' ')
            {
                len--;
            }
            ms.Write(rec, 0, len);
            ms.WriteByte((byte)'\n');
        }
        return ms.ToArray();
    }

    public static void WriteBinary(string path, IEnumerable<byte[]> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, records.SelectMany(r => r).ToArray());
    }

    public static string Text(byte[] record) => CobolCodec.Text(record);
}
