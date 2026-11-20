using System.Globalization;
using System.Text;

namespace CardDemo.Domain;

/// <summary>Byte-level codecs for COBOL USAGE DISPLAY (zoned, ASCII overpunch), COMP-3 and COMP fields.</summary>
public static class CobolCodec
{
    public static readonly Encoding Latin1 = Encoding.Latin1;
    private const string Positive = "{ABCDEFGHI";
    private const string Negative = "}JKLMNOPQR";

    public static string Text(ReadOnlySpan<byte> raw) => Latin1.GetString(raw);

    public static decimal Decode(ReadOnlySpan<byte> raw, FieldSpec f) => f.Kind switch
    {
        FieldKind.Zoned => DecodeZoned(raw, f.Scale),
        FieldKind.Packed => DecodePacked(raw, f.Scale),
        FieldKind.Binary => DecodeBinary(raw, f.Scale, f.Signed),
        _ => throw new InvalidOperationException($"{f.Name} is not numeric"),
    };

    public static void Encode(Span<byte> target, FieldSpec f, decimal value)
    {
        switch (f.Kind)
        {
            case FieldKind.Zoned: EncodeZoned(target, value, f.Scale, f.Signed); break;
            case FieldKind.Packed: EncodePacked(target, value, f.Scale, f.Signed); break;
            case FieldKind.Binary: EncodeBinary(target, value, f.Scale, f.Signed); break;
            default: throw new InvalidOperationException($"{f.Name} is not numeric");
        }
    }

    public static decimal DecodeZoned(ReadOnlySpan<byte> raw, int scale)
    {
        var chars = Latin1.GetString(raw).ToCharArray();
        if (chars.All(c => c == ' '))
        {
            return 0m;
        }
        var sign = 1;
        var last = chars[^1];
        int idx;
        if ((idx = Positive.IndexOf(last, StringComparison.Ordinal)) >= 0)
        {
            chars[^1] = (char)('0' + idx);
        }
        else if ((idx = Negative.IndexOf(last, StringComparison.Ordinal)) >= 0)
        {
            chars[^1] = (char)('0' + idx);
            sign = -1;
        }
        var digits = new string(chars).Replace(' ', '0');
        var units = decimal.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
        return sign * units / Pow10(scale);
    }

    /// <summary>MOVE to a zoned field: high-order digits beyond the PIC are truncated, as COBOL does without ON SIZE ERROR.</summary>
    public static void EncodeZoned(Span<byte> target, decimal value, int scale, bool signed)
    {
        var digits = Units(value, scale, target.Length);
        if (signed)
        {
            var table = value < 0 ? Negative : Positive;
            digits = digits[..^1] + table[digits[^1] - '0'];
        }
        Latin1.GetBytes(digits, target);
    }

    public static decimal DecodePacked(ReadOnlySpan<byte> raw, int scale)
    {
        decimal units = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            units = units * 10 + (raw[i] >> 4);
            if (i < raw.Length - 1)
            {
                units = units * 10 + (raw[i] & 0x0F);
            }
        }
        var signNibble = raw[^1] & 0x0F;
        return (signNibble is 0x0B or 0x0D ? -units : units) / Pow10(scale);
    }

    public static void EncodePacked(Span<byte> target, decimal value, int scale, bool signed)
    {
        var digits = Units(value, scale, target.Length * 2 - 1);
        var nibbles = digits.Select(c => c - '0').Append(signed ? (value < 0 ? 0x0D : 0x0C) : 0x0F).ToArray();
        for (var i = 0; i < target.Length; i++)
        {
            target[i] = (byte)((nibbles[2 * i] << 4) | nibbles[2 * i + 1]);
        }
    }

    public static decimal DecodeBinary(ReadOnlySpan<byte> raw, int scale, bool signed)
    {
        long v = signed && (raw[0] & 0x80) != 0 ? -1 : 0;
        foreach (var b in raw)
        {
            v = (v << 8) | b;
        }
        return v / Pow10(scale);
    }

    public static void EncodeBinary(Span<byte> target, decimal value, int scale, bool signed)
    {
        _ = signed;
        var v = (long)decimal.Truncate(value * Pow10(scale));
        for (var i = target.Length - 1; i >= 0; i--)
        {
            target[i] = (byte)(v & 0xFF);
            v >>= 8;
        }
    }

    /// <summary>COMPUTE without ROUNDED: excess decimal places are truncated toward zero.</summary>
    public static decimal Truncate(decimal value, int scale) => decimal.Truncate(value * Pow10(scale)) / Pow10(scale);

    /// <summary>GnuCOBOL DISPLAY of a signed USAGE DISPLAY item: all digits followed by '+' or '-'.</summary>
    public static string DisplaySigned(decimal value, int digits, int scale) => Units(value, scale, digits) + (value < 0 ? "-" : "+");

    /// <summary>DISPLAY of an unsigned numeric item (e.g. PIC 9(09)).</summary>
    public static string DisplayUnsigned(decimal value, int digits) => Units(value, 0, digits);

    /// <summary>Numeric-edited picture with a fixed leading sign symbol, e.g. -ZZZ,ZZZ,ZZZ.ZZ or +ZZZ,ZZZ,ZZZ.ZZ.</summary>
    public static string Edit(decimal value, string picture)
    {
        var signSymbol = picture[0];
        var body = picture[1..];
        var scale = body.Length - body.IndexOf('.', StringComparison.Ordinal) - 1;
        var digitCount = body.Count(c => c == 'Z' || c == '9');
        var digits = Units(value, scale, digitCount);
        if (value == 0 && !body.Contains('9', StringComparison.Ordinal))
        {
            return new string(' ', picture.Length);
        }
        var sb = new StringBuilder(picture.Length);
        sb.Append(value < 0 ? '-' : signSymbol == '+' ? '+' : ' ');
        var d = 0;
        var suppressing = true;
        foreach (var c in body)
        {
            if (c is 'Z' or '9')
            {
                var digit = digits[d++];
                if (suppressing && digit == '0' && c == 'Z' && d <= digitCount - scale)
                {
                    sb.Append(' ');
                    continue;
                }
                suppressing = false;
                sb.Append(digit);
            }
            else if (c == '.')
            {
                suppressing = false;
                sb.Append('.');
            }
            else
            {
                sb.Append(suppressing ? ' ' : c);
            }
        }
        return sb.ToString();
    }

    private static string Units(decimal value, int scale, int length)
    {
        var units = decimal.Truncate(Math.Abs(value) * Pow10(scale));
        var text = units.ToString("0", CultureInfo.InvariantCulture).PadLeft(length, '0');
        return text[^length..];
    }

    private static decimal Pow10(int scale)
    {
        decimal p = 1;
        for (var i = 0; i < scale; i++)
        {
            p *= 10;
        }
        return p;
    }
}
