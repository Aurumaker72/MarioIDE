﻿using System.Globalization;

/* TODO: Implement:
 Mupen64 will trigger a power off/on reset when the value for the controller info is specifically set to Reserved1 = 0x01, and Reserved2 = 0x01.
*/
namespace MarioIDE.Mupen;

/// <summary>
///   A wrapper for controller input for a particular frame
/// </summary>
public class InputModel
{
    private sbyte _x;
    private sbyte _y;

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
    public ushort Buttons { get; set; }

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
    public InputModel(byte[] input)
    {
        // input = byte[4] { AA, BB, CC, DD }

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (input.Length != 4)
        {
            throw new InvalidOperationException($"{nameof(input)} should be 4 bytes, not {input.Length}");
        }

        // X = CC
        X = unchecked((sbyte)input[2]);

        // Y = DD
        Y = unchecked((sbyte)input[3]);

        // Buttons = AA BB
        Buttons = BitConverter.ToUInt16(input
            .Take(2)
            .Reverse() // Reverse because BitConverter reverses order due to being low-endien
            .ToArray(), 0);
    }

    /// <summary>
    ///   Sets a controller input to be either pressed or unpressed
    /// </summary>
    /// <param name="input">The button type to set</param>
    /// <param name="isPressed">The state the button is to be set</param>
    public void SetControllerInput(ControllerInput input, bool isPressed)
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
    public bool GetButtonState(ControllerInput input)
    {
        return ((ushort)input & Buttons) != 0;
    }

    /// <summary>
    ///   Returns a string of pressed button inputs
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ControllerInput> GetButtons()
    {
        return EnumExtensions.EnumToArray<ControllerInput>()
            .Where(input => ((ControllerInput)Buttons).HasFlag(input));
    }

    /// <summary>
    ///   Implicitly converts a <see cref="byte" /> array into an <see cref="InputModel" />.
    /// </summary>
    /// <param name="input"></param>
    public static explicit operator InputModel(byte[] input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input),
                string.Format(CultureInfo.InvariantCulture, "ArgumentIsNull", nameof(input)));
        }

        if (input.Length != 4)
        {
            throw new ArgumentException("InputArrayInvalidLength", nameof(input));
        }

        return new InputModel(input);
    }

    /// <summary>
    ///   Explicitly converts an <see cref="InputModel" /> array into a <see cref="byte" />.
    /// </summary>
    /// <param name="input"></param>
    public static explicit operator byte[](InputModel input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        byte x = unchecked((byte)input.X);
        byte y = unchecked((byte)input.Y);
        byte[] buttons = BitConverter.GetBytes(input.Buttons);

        // Buttons are reversed because BitConverter reverses order due to being low-endien
        return new[] { buttons[1], buttons[0], x, y };
    }
}