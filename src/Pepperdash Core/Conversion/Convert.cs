using System.Text;

namespace PepperDash.Core.Conversion
{
    public class EncodingHelper
    {
        public static string ConvertUtf8ToAscii(string utf8String)
        {
            return Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(utf8String), 0, utf8String.Length);
        }

        public static string ConvertUtf8ToUtf16(string utf8String)
        {
            return Encoding.Unicode.GetString(Encoding.UTF8.GetBytes(utf8String), 0, utf8String.Length);
        }

    }
}