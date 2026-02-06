using System;
using System.Runtime.InteropServices;

namespace ErenshorGlider.Input;

/// <summary>
/// Provides Windows API-level keyboard input simulation using SendInput.
/// This sends input events directly to the operating system, which will be
/// received by whatever window has focus (including the game).
/// </summary>
public static class WindowsInputSimulator
{
    #region P/Invoke Declarations

    /// <summary>
    /// Sends input to the operating system.
    /// </summary>
    /// <param name="nInputs">Number of inputs in the array.</param>
    /// <param name="pInputs">Array of INPUT structures.</param>
    /// <param name="cbSize">Size of INPUT structure in bytes.</param>
    /// <returns>Number of inputs successfully sent.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    #endregion

    #region Structures

    /// <summary>
    /// Structure for input data sent to SendInput.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        /// <summary>
        /// The type of input (keyboard, mouse, hardware).
        /// </summary>
        public uint Type;

        /// <summary>
        /// Union of keyboard, mouse, and hardware input data.
        /// We use Keyboard as the primary field.
        /// </summary>
        public Data UnionData;

        /// <summary>
        /// Simplified data union - we only need keyboard input.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct Data
        {
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
        }
    }

    /// <summary>
    /// Keyboard input structure for SendInput.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        /// <summary>
        /// Virtual-key code (VK_ constant).
        /// </summary>
        public ushort wVk;

        /// <summary>
        /// Hardware scan code. Use 0 for SendInput.
        /// </summary>
        public ushort wScan;

        /// <summary>
        /// Flags for key event (keydown, keyup, etc.).
        /// </summary>
        public uint dwFlags;

        /// <summary>
        /// Time stamp for event, or 0 for default.
        /// </summary>
        public uint time;

        /// <summary>
        /// Extra info, typically 0.
        /// </summary>
        public IntPtr dwExtraInfo;
    }

    #endregion

    #region Constants

    /// <summary>
    /// Input type: keyboard.
    /// </summary>
    private const uint INPUT_KEYBOARD = 1;

    /// <summary>
    /// Key press flag (key down).
    /// </summary>
    private const uint KEYEVENTF_KEYDOWN = 0x0000;

    /// <summary>
    /// Key release flag (key up).
    /// </summary>
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Unicode character flag (not used - we use virtual key codes).
    /// </summary>
    private const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>
    /// Scan code flag (not used).
    /// </summary>
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    #endregion

    /// <summary>
    /// Simulates pressing a key down.
    /// </summary>
    /// <param name="keyCode">The key code to press.</param>
    /// <returns>True if the input was sent successfully.</returns>
    public static bool PressKey(KeyCode keyCode)
    {
        ushort virtualKey = KeyCodeToVirtualKey(keyCode);
        return SendKeyEvent(virtualKey, KEYEVENTF_KEYDOWN);
    }

    /// <summary>
    /// Simulates releasing a key.
    /// </summary>
    /// <param name="keyCode">The key code to release.</param>
    /// <returns>True if the input was sent successfully.</returns>
    public static bool ReleaseKey(KeyCode keyCode)
    {
        ushort virtualKey = KeyCodeToVirtualKey(keyCode);
        return SendKeyEvent(virtualKey, KEYEVENTF_KEYUP);
    }

    /// <summary>
    /// Simulates a full key press (down then up) with a delay.
    /// </summary>
    /// <param name="keyCode">The key code to press.</param>
    /// <param name="durationMs">Duration to hold the key in milliseconds.</param>
    /// <returns>True if both events were sent successfully.</returns>
    public static bool PressAndReleaseKey(KeyCode keyCode, int durationMs = 50)
    {
        ushort virtualKey = KeyCodeToVirtualKey(keyCode);

        if (!SendKeyEvent(virtualKey, KEYEVENTF_KEYDOWN))
            return false;

        if (durationMs > 0)
        {
            System.Threading.Thread.Sleep(durationMs);
        }

        return SendKeyEvent(virtualKey, KEYEVENTF_KEYUP);
    }

    /// <summary>
    /// Sends a keyboard event to the operating system.
    /// </summary>
    /// <param name="virtualKey">Virtual key code.</param>
    /// <param name="flags">Event flags (key down or key up).</param>
    /// <returns>True if successful.</returns>
    private static bool SendKeyEvent(ushort virtualKey, uint flags)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].Type = INPUT_KEYBOARD;
        inputs[0].UnionData.Keyboard.wVk = virtualKey;
        inputs[0].UnionData.Keyboard.wScan = 0;
        inputs[0].UnionData.Keyboard.dwFlags = flags;
        inputs[0].UnionData.Keyboard.time = 0;
        inputs[0].UnionData.Keyboard.dwExtraInfo = IntPtr.Zero;

        uint result = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

        // Check for error
        if (result == 0)
        {
            int errorCode = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"SendInput failed with error code: {errorCode}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts KeyCode enum to Windows Virtual Key code.
    /// </summary>
    /// <param name="keyCode">The KeyCode enum value.</param>
    /// <returns>Windows Virtual Key code.</returns>
    private static ushort KeyCodeToVirtualKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            // Letter keys
            KeyCode.W => 0x57,        // VK_W
            KeyCode.A => 0x41,        // VK_A
            KeyCode.S => 0x53,        // VK_S
            KeyCode.D => 0x44,        // VK_D
            KeyCode.Q => 0x51,        // VK_Q
            KeyCode.E => 0x45,        // VK_E

            // Special keys
            KeyCode.Space => 0x20,    // VK_SPACE
            KeyCode.Tab => 0x09,      // VK_TAB
            KeyCode.Enter => 0x0D,    // VK_RETURN
            KeyCode.Escape => 0x1B,   // VK_ESCAPE

            // Arrow keys
            KeyCode.LeftArrow => 0x25, // VK_LEFT
            KeyCode.RightArrow => 0x27, // VK_RIGHT
            KeyCode.UpArrow => 0x26,   // VK_UP
            KeyCode.DownArrow => 0x28, // VK_DOWN

            // Number keys (top row)
            KeyCode.Alpha0 => 0x30,   // VK_0
            KeyCode.Alpha1 => 0x31,   // VK_1
            KeyCode.Alpha2 => 0x32,   // VK_2
            KeyCode.Alpha3 => 0x33,   // VK_3
            KeyCode.Alpha4 => 0x34,   // VK_4
            KeyCode.Alpha5 => 0x35,   // VK_5
            KeyCode.Alpha6 => 0x36,   // VK_6
            KeyCode.Alpha7 => 0x37,   // VK_7
            KeyCode.Alpha8 => 0x38,   // VK_8
            KeyCode.Alpha9 => 0x39,   // VK_9

            // Function keys
            KeyCode.F1 => 0x70,       // VK_F1
            KeyCode.F2 => 0x71,       // VK_F2
            KeyCode.F3 => 0x72,       // VK_F3
            KeyCode.F4 => 0x73,       // VK_F4
            KeyCode.F5 => 0x74,       // VK_F5
            KeyCode.F6 => 0x75,       // VK_F6
            KeyCode.F7 => 0x76,       // VK_F7
            KeyCode.F8 => 0x77,       // VK_F8
            KeyCode.F9 => 0x78,       // VK_F9
            KeyCode.F10 => 0x79,      // VK_F10
            KeyCode.F11 => 0x7A,      // VK_F11
            KeyCode.F12 => 0x7B,      // VK_F12

            _ => 0
        };
    }

    /// <summary>
    /// Gets the Windows Virtual Key code for a KeyCode.
    /// Useful for testing and verification.
    /// </summary>
    /// <param name="keyCode">The KeyCode enum value.</param>
    /// <returns>Windows Virtual Key code.</returns>
    public static ushort GetVirtualKey(KeyCode keyCode) => KeyCodeToVirtualKey(keyCode);
}
