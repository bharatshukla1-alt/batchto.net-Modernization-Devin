using CardDemo.Batch.Programs;
using CardDemo.Domain;
using Xunit;

namespace CardDemo.Tests;

public class CodecTests
{
    [Theory]
    [InlineData("12.34", "0123D")]
    [InlineData("-12.34", "0123M")]
    [InlineData("0", "0000{")]
    [InlineData("-0.10", "0001}")]
    public void Zoned_overpunch_round_trips(string value, string encoded)
    {
        var buf = new byte[5];
        CobolCodec.EncodeZoned(buf, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), 2, true);
        Assert.Equal(encoded, CobolCodec.Text(buf));
        Assert.Equal(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), CobolCodec.DecodeZoned(buf, 2));
    }

    [Fact]
    public void Packed_decimal_matches_ibm_comp3()
    {
        var buf = new byte[7];
        CobolCodec.EncodePacked(buf, 2525.00m, 2, true);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02, 0x52, 0x50, 0x0C }, buf);
        CobolCodec.EncodePacked(buf, -2500.00m, 2, true);
        Assert.Equal(0x0D, buf[6] & 0x0F);
        Assert.Equal(-2500.00m, CobolCodec.DecodePacked(buf, 2));
    }

    [Theory]
    [InlineData("1000.00", "15.99", "13.32")]
    [InlineData("-1000.00", "15.99", "-13.32")]
    [InlineData("100.30", "15.00", "1.25")]
    public void Interest_is_truncated_not_rounded(string bal, string rate, string expected)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal(decimal.Parse(expected, inv), Cbact04c.MonthlyInterest(decimal.Parse(bal, inv), decimal.Parse(rate, inv)));
    }

    [Fact]
    public void Numeric_edit_suppresses_leading_zeros()
    {
        var s = CobolCodec.Edit(1234.50m, "-ZZZ,ZZZ,ZZZ.ZZ");
        Assert.Equal(15, s.Length);
        Assert.Equal("1,234.50", s.Trim());
        Assert.StartsWith("-", CobolCodec.Edit(-5m, "-ZZZ,ZZZ,ZZZ.ZZ"), StringComparison.Ordinal);
        Assert.StartsWith("+", CobolCodec.Edit(5m, "+ZZZ,ZZZ,ZZZ.ZZ"), StringComparison.Ordinal);
    }

    [Fact]
    public void Display_signed_uses_trailing_sign()
    {
        Assert.Equal("000000019400+", CobolCodec.DisplaySigned(194.00m, 12, 2));
        Assert.Equal("000000000150-", CobolCodec.DisplaySigned(-1.50m, 12, 2));
    }

    [Fact]
    public void Cobdatft_converts_iso_to_yyyymmdd()
    {
        Assert.Equal("20250520", CobDatFt.Convert("2025-05-20", '2', '2').TrimEnd());
        Assert.Equal("2025-05-20", CobDatFt.Convert("20250520", '1', '1').TrimEnd());
    }

    [Fact]
    public void Generated_layouts_have_copybook_lengths()
    {
        Assert.Equal(300, Layouts.Account.Length);
        Assert.Equal(150, Layouts.Card.Length);
        Assert.Equal(50, Layouts.CardXref.Length);
        Assert.Equal(350, Layouts.Transaction.Length);
        Assert.Equal(430, Layouts.RejectRecord.Length);
    }
}
