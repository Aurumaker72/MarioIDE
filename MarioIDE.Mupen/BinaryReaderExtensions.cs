using System.Globalization;

namespace MarioIDE.Mupen;

internal static class BinaryReaderExtensions
{
    // Read a single byte from a given offset of a binary stream.
    public static byte ReadByte(this BinaryReader reader, long offset)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        return reader.ReadByte();
    }

    // Reads a number of bytes from a given offset of a binary stream.
    public static byte[] ReadBytes(this BinaryReader reader, long offset, int length)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        return reader.ReadBytes(length);
    }

    // Reads 4 bytes from a given offset of a binary stream and returns it as an <see cref="uint" />.
    public static uint ReadUInt32(this BinaryReader reader, long offset)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        return BitConverter.ToUInt32(reader.ReadBytes(offset, 4), 0);
    }

    // Reads 4 bytes from a given offset of a binary stream and returns it as an <see cref="int" />.
    public static int ReadInt32(this BinaryReader reader, long offset)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        return BitConverter.ToInt32(reader.ReadBytes(offset, 4), 0);
    }

    // Reads 2 bytes from a given offset of a binary stream and returns it as an <see cref="ushort" />.
    public static ushort ReadUInt16(this BinaryReader reader, long offset)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        return BitConverter.ToUInt16(reader.ReadBytes(offset, 2), 0);
    }

    // Reads a number of bytes from a given offset of a binary stream and returns it as a string with a specific encoding.
    public static string ReadString(this BinaryReader reader, long offset, int length, Encoding encoding)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", reader));
        }

        return reader.ReadBytes(offset, length).Encode(encoding);
    }
}