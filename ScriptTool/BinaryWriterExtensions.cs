using System;
using System.IO;
using System.Text;

namespace ScriptTool
{
    internal static class BinaryWriterExtensions
    {
        private static void EncryptString(byte[] data)
        {
            uint key = 0x4B5AB4A5;

            for (var i = 0; i < data.Length; i++)
            {
                var b = data[i];
                data[i] = (byte)(key ^ data[i]);
                key = b ^ ((key << 9) | (key >> 23) & 0x1F0);
            }
        }

        public static void WriteEncryptedString(this BinaryWriter writer, string s, Encoding encoding)
        {
            var data = encoding.GetBytes(s);
            var buffer = new byte[data.Length + 1];
            Array.Copy(data, buffer, data.Length);
            EncryptString(buffer);
            writer.Write(Convert.ToUInt16(buffer.Length));
            writer.Write(buffer);
        }
    }
}
