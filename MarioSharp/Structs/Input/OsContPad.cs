using System.Runtime.InteropServices;

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace MarioSharp.Structs.Input;

[StructLayout(LayoutKind.Sequential)]
public struct OsContPad
{
    private ushort _button;
    private sbyte _x;
    private sbyte _y;
    private readonly byte _errnum;

    /// <summary>
    ///   The analogue x coordinate
    /// </summary>
    public int X
    {
        get => Convert.ToInt32(_x);
        set => _x = Convert.ToSByte(value);
    }

    /// <summary>
    ///   The analogue y coordinate
    /// </summary>
    public int Y
    {
        get => Convert.ToInt32(_y);
        set => _y = Convert.ToSByte(value);
    }

    /// <summary>
    ///   The array of bytes representing the combination of buttons pressed
    /// </summary>
    public ushort Buttons
    {
        get => _button;
        set => _button = value;
    }

    /// <summary>
    ///   InputModel representing data contained within an .m64 file.
    /// </summary>
    /// <param name="input">
    ///   A 4-byte value containing X and Y analogue positions, and the XOR of buttons pressed.
    ///   The first two bytes are the XOR of the buttons, followed by the X and Y inputs
    ///   represented by 1-byte each.
    ///   <para />
    ///   <example>
    ///     Given the input 0xC0182541, this can be seen as:
    ///     <list type="bullet">
    ///       <item>
    ///         <term>Button Flags</term>
    ///         <description>2-bytes = C0 18</description>
    ///       </item>
    ///       <item>
    ///         <term>X Analogue</term>
    ///         <description>1-byte = 25</description>
    ///       </item>
    ///       <item>
    ///         <term>Y Analogue</term>
    ///         <description>1-byte = 41</description>
    ///       </item>
    ///     </list>
    ///   </example>
    ///   <remarks>
    ///     When reading from an .m64 file from offset 0x400, 4-bytes at a time, the following code works if the
    ///     hex input is NOT REVERSED.
    ///   </remarks>
    /// </param>
    public OsContPad(byte[] input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (input.Length != 4)
        {
            throw new InvalidOperationException($"{nameof(input)} should be 4 bytes, not {input.Length}");
        }

        _button = BitConverter.ToUInt16(input
            .Take(2)
            .Reverse() // Reverse because BitConverter reverses order due to being low-endien
            .ToArray(), 0);

        _x = Convert.ToSByte(unchecked((sbyte)input[2]));
        _y = Convert.ToSByte(unchecked((sbyte)input[3]));
        _errnum = 0;
    }

    /// <summary>
    ///   Sets a button to be either pressed or unpressed
    /// </summary>
    /// <param name="input">The button type to set</param>
    /// <param name="isPressed">The state the button is to be set</param>
    public void SetButton(Button input, bool isPressed)
    {
        if (isPressed)
        {
            Buttons |= (ushort)input;
        }
        else
        {
            Buttons &= (ushort)~input;
        }
    }

    /// <summary>
    ///   Returns the state of a particular button
    /// </summary>
    /// <param name="input"></param>
    /// <returns>True is button is pressed</returns>
    public bool GetButtonState(Button input)
    {
        return ((ushort)input & Buttons) != 0;
    }

    /// <summary>
    ///   Implicitly converts a <see cref="byte" /> array into an <see cref="OsContPad" />.
    /// </summary>
    /// <param name="input"></param>
    public static explicit operator OsContPad(byte[] input)
    {
        if (input is null || input.Length != 4)
            throw new Exception("Invalid input");
        return new OsContPad(input);
    }

    /// <summary>
    ///   Explicitly converts an <see cref="OsContPad" /> array into a <see cref="byte" />.
    /// </summary>
    /// <param name="input"></param>
    public static explicit operator byte[](OsContPad input)
    {
        byte x = unchecked((byte)input.X);
        byte y = unchecked((byte)input.Y);
        byte[] buttons = BitConverter.GetBytes(input.Buttons);

        // Buttons are reversed because BitConverter reverses order due to being low-endien
        return new[] { buttons[1], buttons[0], x, y };
    }

    public static IEnumerable<T> EnumToArray<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    /// <summary>
    ///   Join a collection of elements and return them as a comma-separated string.
    /// </summary>
    /// <typeparam name="T">The object type contained by the IEnumerable.</typeparam>
    /// <param name="collection">The collection of elements to join.</param>
    /// <param name="separator">The separator character. Default is ", ".</param>
    /// <returns>A string composed of the collection elements with separators where appropriate.</returns>
    public static string Join<T>(IEnumerable<T> collection, string separator = ", ")
    {
        return string.Join(separator, collection);
    }

    /// <summary>
    ///   Returns a string of pressed button inputs
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Button> GetButtons()
    {
        ushort button = _button;
        return EnumToArray<Button>().Where(input => ((Button)button).HasFlag(input));
    }

    /// <summary>
    ///   Override to return string of analogue inputs and buttons pressed
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{(X, Y)} {Join(GetButtons())}";
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public bool Equals(OsContPad other)
    {
        return _button == other._button && _x == other._x && _y == other._y && _errnum == other._errnum;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _button.GetHashCode();
            hashCode = (hashCode * 397) ^ _x.GetHashCode();
            hashCode = (hashCode * 397) ^ _y.GetHashCode();
            hashCode = (hashCode * 397) ^ _errnum.GetHashCode();
            return hashCode;
        }
    }
}