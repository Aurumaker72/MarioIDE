using System.Runtime.InteropServices;

namespace MarioSharp.Internal;

public static class Natives
{
    [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    internal static extern IntPtr MemSet(IntPtr dest, int c, int byteCount);

    [DllImport("kernel32.dll")]
    internal static extern bool VirtualLock(IntPtr lpAddress, UIntPtr dwSize);

    [DllImport("kernel32.dll")]
    internal static extern bool VirtualUnlock(IntPtr lpAddress, UIntPtr dwSize);

    [DllImport("MarioSharp.Gfx.dll", EntryPoint = "get_opengl_api")]
    internal static extern IntPtr GetOpenGlApi();

    [DllImport("MarioSharp.Gfx.dll", EntryPoint = "clear_gl_textures")]
    internal static extern void ClearGlTextures();

    [DllImport("MarioSharp.Gfx.dll", EntryPoint = "play_audio")]
    internal static extern IntPtr PlayAudio(IntPtr createNextAudioBufferPtr, bool silent);
}