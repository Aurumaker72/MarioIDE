﻿namespace MarioIDE.Mupen;

/// <summary>
///   Attribute used to specify a string encoding and byte size.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class StringEncodingAttribute : Attribute
{
    /// <summary>
    ///   Sets the string encoding type for reading and writing
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    ///   The size of the string in bytes
    /// </summary>
    public int ByteSize { get; }

    /// <summary>
    ///   Attribute used to specify a string encoding and byte size.
    /// </summary>
    /// <param name="encoding">The type of encoding of the string.</param>
    /// <param name="byteSize">The number of bytes of the string.</param>
    public StringEncodingAttribute(Encoding encoding, int byteSize)
    {
        Encoding = encoding;
        ByteSize = byteSize;
    }
}