namespace MarioIDE.Mupen;

/// <summary>
///   Static class string encoder helper
/// </summary>
internal static class StringEncoder
{
    /// <summary>
    ///   Encode a byte array with a particular <see cref="Encoding" />.
    /// </summary>
    /// <param name="bytes">Byte array to be encoded</param>
    /// <param name="encoding">The encoding type.</param>
    /// <returns>Encoded string</returns>
    public static string Encode(this byte[] bytes, Encoding encoding)
    {
        switch (encoding)
        {
            case Encoding.ASCII:
                return bytes.EncodeAscii();
            case Encoding.UTF8:
                return bytes.EncodeUtf8();
            default:
                throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "InvalidEncodingType");
        }
    }

    private static string EncodeAscii(this byte[] bytes)
    {
        return System.Text.Encoding.ASCII.GetString(bytes);
    }

    private static string EncodeUtf8(this byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}