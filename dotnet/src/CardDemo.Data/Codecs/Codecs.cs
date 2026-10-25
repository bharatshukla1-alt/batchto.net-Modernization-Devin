using System;
using System.Text;
using CardDemo.Domain.Attributes;

namespace CardDemo.Data.Codecs
{
    [Trace("Mmp.Runtime.Codecs", Ir = "codec:ebcdic")]
    public static class EbcdicCodec
    {
        // Static constructor to register code page provider
        static EbcdicCodec()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static Encoding Ebcdic => Encoding.GetEncoding(37);

        public static string ReadString(ReadOnlySpan<byte> span)
        {
            return Ebcdic.GetString(span).TrimEnd();
        }

        public static void WriteString(string value, Span<byte> destination)
        {
            destination.Fill(0x40); // EBCDIC space padding (0x40)
            byte[] bytes = Ebcdic.GetBytes(value);
            int copyLen = Math.Min(bytes.Length, destination.Length);
            bytes.AsSpan(0, copyLen).CopyTo(destination);
        }
    }

    [Trace("Mmp.Runtime.Codecs", Ir = "codec:comp3")]
    public static class Comp3Codec
    {
        public static decimal ReadDecimal(ReadOnlySpan<byte> span, int scale)
        {
            long unscaled = 0;
            int length = span.Length;

            for (int i = 0; i < length; i++)
            {
                byte b = span[i];
                int high = (b >> 4) & 0x0F;
                int low = b & 0x0F;

                if (i == length - 1)
                {
                    unscaled = (unscaled * 10) + high;
                    // Low nibble: 0x0D or 0x0B is negative
                    if (low == 0x0D || low == 0x0B)
                    {
                        unscaled = -unscaled;
                    }
                }
                else
                {
                    unscaled = (unscaled * 100) + (high * 10) + low;
                }
            }

            return unscaled / (decimal)Math.Pow(10, scale);
        }
    }
}
