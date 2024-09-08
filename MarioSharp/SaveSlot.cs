using MarioSharp.Internal;

namespace MarioSharp;

/// <summary>
/// A save state for a game instance
/// </summary>
public class SaveSlot : IDisposable
{
    /// <summary>
    /// The frame index into the save slot
    /// </summary>
    public int Frame { get; internal set; }

    /// <summary>
    /// The data segment bytes
    /// </summary>
    internal byte[] DataSegment { get; }

    /// <summary>
    /// The bss segment bytes
    /// </summary>
    internal byte[] BssSegment { get; }

    /// <summary>
    /// A save slot for the gameinstance
    /// </summary>
    /// <param name="gameInstance"></param>
    public SaveSlot(GameInstance gameInstance)
    {
        Frame = -1;
        DataSegment = new byte[gameInstance.DataSection.Size];
        BssSegment = new byte[gameInstance.BssSection.Size];
        PreAllocateArray(DataSegment, DataSegment.Length);
        PreAllocateArray(BssSegment, BssSegment.Length);
    }

    /// <summary>
    /// Preallocate the memory for the save slot to speed up the initial 'Save' call
    /// </summary>
    /// <param name="array"></param>
    /// <param name="size"></param>
    private static unsafe void PreAllocateArray(byte[] array, int size)
    {
        fixed (byte* ptr = array)
        {
            Natives.MemSet((IntPtr)ptr, 0, size);
            Natives.VirtualLock((IntPtr)ptr, (UIntPtr)size);
        }
    }

    /// <summary>
    /// Dispose of the memory
    /// </summary>
    /// <param name="array"></param>
    /// <param name="size"></param>
    private static unsafe void DisposeArray(byte[] array, int size)
    {
        fixed (byte* ptr = array)
        {
            Natives.MemSet((IntPtr)ptr, 0, size);
            Natives.VirtualUnlock((IntPtr)ptr, (UIntPtr)size);
        }
    }

    /// <summary>
    /// Return false if memory is uninitialized or invalidated
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => Frame != -1;

    /// <summary>
    /// Invalidate the save slot
    /// </summary>
    public void Invalidate() => Frame = -1;

    /// <summary>
    /// Dispose of the save slot
    /// </summary>
    public void Dispose()
    {
        DisposeArray(DataSegment, DataSegment.Length);
        DisposeArray(BssSegment, BssSegment.Length);
        Frame = -1;
    }
}