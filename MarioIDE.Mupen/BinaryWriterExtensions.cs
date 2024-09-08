using System.Globalization;

namespace MarioIDE.Mupen;

internal static class BinaryWriterExtensions
{
    public static void WriteBytes(this BinaryWriter writer, long offset, byte[] buffer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer), string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", writer));
        }

        writer.BaseStream.Seek(offset, SeekOrigin.Begin);
        writer.Write(buffer);
    }

    public static void WriteString(this BinaryWriter writer, long offset, Encoding encoding, string value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer), string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", writer));
        }

        byte[] bytes = encoding == Encoding.ASCII ? System.Text.Encoding.ASCII.GetBytes(value) : System.Text.Encoding.UTF8.GetBytes(value);
        writer.WriteBytes(offset, bytes);
    }

    public static void WriteUInt32(this BinaryWriter writer, long offset, uint value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer), string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", writer));
        }

        writer.WriteBytes(offset, BitConverter.GetBytes(value));
    }

    public static void WriteUInt16(this BinaryWriter writer, long offset, ushort value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer), string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", writer));
        }

        writer.WriteBytes(offset, BitConverter.GetBytes(value));
    }

    public static void WriteByte(this BinaryWriter writer, long offset, byte value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer), string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", writer));
        }

        writer.BaseStream.Seek(offset, SeekOrigin.Begin);
        writer.Write(value);
    }
}