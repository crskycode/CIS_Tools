using System.IO;
using System.Linq;
using System.Text;

namespace ScriptTool
{
    internal static class BinaryReaderExtensions
    {
        private static void DecryptString(byte[] data)
        {
            uint key = 0x4B5AB4A5;

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(key ^ data[i]);
                key = data[i] ^ ((key << 9) | (key >> 23) & 0x1F0);
            }
        }

        public static string ReadEncryptedString(this BinaryReader reader, Encoding encoding)
        {
            int length = reader.ReadUInt16();
            var bytes = reader.ReadBytes(length);
            DecryptString(bytes);
            var buffer = bytes.TakeWhile(x => x != 0).ToArray();
            return encoding.GetString(buffer);
        }
    }
}
