using System;
using System.Linq;
using System.Text;

namespace AndroidUsbSerialDriver.Util
{
    public class FormatConverter
    {
        public static string ByteArrayToHexString(byte[] byteData)
        {
            return BitConverter.ToString(byteData).Replace("-", " ");
        }

        public static string ByteArrayToString(byte[] byteData)
        {
            return Encoding.GetEncoding("gb2312").GetString(byteData);
        }

        public static byte[] HexStringToByteArray(string hexData)
        {
            return Enumerable.Range(0, hexData.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexData.Substring(x, 2), 16))
                .ToArray();
        }

        public static byte[] StringToByteArray(string stringData)
        {
            return Encoding.GetEncoding("gb2312").GetBytes(stringData);
        }
    }
}