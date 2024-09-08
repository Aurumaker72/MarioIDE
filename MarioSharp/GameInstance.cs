using MarioSharp.Internal;
using MarioSharp.Structs.Input;
using System.Runtime.InteropServices;

namespace MarioSharp;

public class GameInstance : IDisposable
{
    /// <summary>
    /// Contains some basic memory access
    /// </summary>
    public GameMemory Memory { get; }

    /// <summary>
    /// The frame index after last call to update
    /// </summary>
    public int Frame { get; private set; }

    /// <summary>
    /// The total size a save slot takes in memory
    /// </summary>
    public int SaveSlotSize => DataSection.Size + BssSection.Size;

    /// <summary>
    /// The GFX Type
    /// </summary>
    public GfxType GfxType { get; }

    public static int LoadCount { get; private set; }
    public static int SaveCount { get; private set; }

    // Internal Fields
    internal readonly SectionInfo DataSection;
    internal readonly SectionInfo BssSection;

    // Private Fields
    private readonly DllFromMemory _dllFromMemory;
    private readonly IntPtr _createNextAudioBuffer;
    private readonly int _cpuCount = Environment.ProcessorCount;

    // Delegates
    private delegate void InitDelegate();
    private delegate void ResetTextureCacheDelegate();
    private delegate void UpdateDelegate(OsContPad input, bool render);
    private delegate void GfxInitDelegate(IntPtr rapi);
    private delegate void GfxInitDummyDelegate();
    private delegate void SetGfxDimensionsDelegate(int width, int height);

    // Functions
    private readonly ResetTextureCacheDelegate _resetTextureCache;
    private readonly UpdateDelegate _update;
    private readonly GfxInitDelegate _gfxInit;
    private readonly SetGfxDimensionsDelegate _setGfxDimensions;
    private readonly IntPtr _openGlApi;

    /// <summary>
    /// Create a new instance of super mario from a dll
    /// </summary>
    /// <param name="gfxType">The type of GFX</param>
    /// <param name="dllBytes">The game dll bytes</param>
    public GameInstance(GfxType gfxType, byte[] dllBytes)
    {
        GfxType = gfxType;

        _openGlApi = Natives.GetOpenGlApi();

        _dllFromMemory = new DllFromMemory(dllBytes);

        InitDelegate init = _dllFromMemory.GetDelegateFromFuncName<InitDelegate>("sm64_init");
        GfxInitDummyDelegate gfxInitDummy = _dllFromMemory.GetDelegateFromFuncName<GfxInitDummyDelegate>("gfx_init_dummy");
        _gfxInit = _dllFromMemory.GetDelegateFromFuncName<GfxInitDelegate>("gfx_init_external");
        _setGfxDimensions = _dllFromMemory.GetDelegateFromFuncName<SetGfxDimensionsDelegate>("set_gfx_dimensions");
        _resetTextureCache = _dllFromMemory.GetDelegateFromFuncName<ResetTextureCacheDelegate>("reset_texture_cache");
        _createNextAudioBuffer = GetAddress("create_next_audio_buffer");
        _update = _dllFromMemory.GetDelegateFromFuncName<UpdateDelegate>("sm64_update");

        Memory = new GameMemory(this);

        DataSection = _dllFromMemory.Sections.First(s => s.Name == ".data");
        BssSection = _dllFromMemory.Sections.First(s => s.Name == ".bss");

        init();
        gfxInitDummy();
    }

    /// <summary>
    /// Resize the gfx for the game instance
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void SetGfxDimensions(int width, int height)
    {
        _setGfxDimensions(width, height);
    }

    /// <summary>
    /// Unload all the gl textures
    /// </summary>
    public void ClearGlTextures()
    {
        Natives.ClearGlTextures();
    }

    private OsContPad _input = new(new byte[4]);

    /// <summary>
    /// Advance the game a frame
    /// </summary>
    /// <param name="input">The input for the frame</param>
    /// <param name="render">Render the game. No effect on headless instances</param>
    /// <param name="silent">Clear the audio if true</param>
    public void Update(OsContPad input, bool render, bool silent)
    {
        if (GfxType == GfxType.OpenGl && render)
        {
            ClearGlTextures();
            _resetTextureCache();
            _gfxInit(_openGlApi);
        }

        _input.X = input.X;
        _input.Y = input.Y;
        _input.Buttons = input.Buttons;

        _update(_input, render);

        if (GfxType == GfxType.OpenGl)
        {
            Natives.PlayAudio(_createNextAudioBuffer, silent);
        }

        Frame++;
    }

    public static void ResetCounters()
    {
        LoadCount = 0;
        SaveCount = 0;
    }

    /// <summary>
    /// Save the current game state to a slot
    /// </summary>
    /// <param name="toSlot"></param>
    public void Save(SaveSlot toSlot)
    {
        Task task1 = MultiThreadedCopy(DataSection.Address, toSlot.DataSegment, DataSection.Size, _cpuCount);
        Task task2 = MultiThreadedCopy(BssSection.Address, toSlot.BssSegment, BssSection.Size, _cpuCount);
        Task.WaitAll(task1, task2);
        //Marshal.Copy(DataSection.Address, toSlot.DataSegment, 0, DataSection.Size);
        //Marshal.Copy(BssSection.Address, toSlot.BssSegment, 0, BssSection.Size);
        toSlot.Frame = Frame;
        SaveCount++;
    }

    /// <summary>
    /// Load a save state into game instance
    /// </summary>
    /// <param name="fromSlot"></param>
    public void Load(SaveSlot fromSlot)
    {
        Task task1 = MultiThreadedCopy(fromSlot.DataSegment, DataSection.Address, DataSection.Size, _cpuCount);
        Task task2 = MultiThreadedCopy(fromSlot.BssSegment, BssSection.Address, BssSection.Size, _cpuCount);
        Task.WaitAll(task1, task2);
        //Marshal.Copy(fromSlot.DataSegment, 0, DataSection.Address, DataSection.Size);
        //Marshal.Copy(fromSlot.BssSegment, 0, BssSection.Address, BssSection.Size);
        Frame = fromSlot.Frame;
        LoadCount++;
    }

    private static Task MultiThreadedCopy(byte[] source, IntPtr destination, int length, int threadCount)
    {
        int i = 0;
        int offset = 0;
        Task[] tasks = new Task[threadCount];

        foreach (int groupSize in DistributeInteger(length, threadCount))
        {
            int o = offset;
            int g = groupSize;

            tasks[i++] = Task.Factory.StartNew(() =>
            {
                Marshal.Copy(source, o, destination + o, g);
            });

            offset += groupSize;
        }

        return Task.WhenAll(tasks);
    }

    private static Task MultiThreadedCopy(IntPtr source, byte[] destination, int length, int threadCount)
    {
        int i = 0;
        int offset = 0;
        Task[] tasks = new Task[threadCount];

        foreach (int groupSize in DistributeInteger(length, threadCount))
        {
            int o = offset;
            int g = groupSize;

            tasks[i++] = Task.Factory.StartNew(() =>
            {
                Marshal.Copy(source + o, destination, o, g);
            });

            offset += groupSize;
        }

        return Task.WhenAll(tasks);
    }

    private static IEnumerable<int> DistributeInteger(int total, int divider)
    {
        if (divider == 0)
            yield return 0;
        else
        {
            int rest = total % divider;
            double result = total / (double)divider;
            for (int i = 0; i < divider; i++)
                if (rest-- > 0)
                    yield return (int)Math.Ceiling(result);
                else
                    yield return (int)Math.Floor(result);
        }
    }

    /// <summary>
    /// Get the address of a variable in memory by its name (slow)
    /// </summary>
    /// <param name="name">The variable name</param>
    /// <returns></returns>
    public IntPtr GetAddress(string name)
    {
        return _dllFromMemory.GetPtrFromFuncName(name);
    }

    /// <summary>
    /// Get a delegate from function name  (slow)
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public TDelegate GetDelegateFromFuncName<TDelegate>(string name) where TDelegate : class
    {
        return _dllFromMemory.GetDelegateFromFuncName<TDelegate>(name);
    }

    /// <summary>
    /// Read raw byte array from memory from pointer
    /// </summary>
    /// <param name="address"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public byte[] ReadRaw(IntPtr address, int count)
    {
        return _dllFromMemory.ReadRaw(address, count);
    }

    /// <summary>
    /// Read raw byte array from memory by variable name (slow)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public byte[] ReadRaw(string name, int count)
    {
        return _dllFromMemory.ReadRaw(GetAddress(name), count);
    }

    /// <summary>
    /// Write raw byte array into memory at pointer location
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    public void WriteRaw(IntPtr address, byte[] value)
    {
        _dllFromMemory.WriteRaw(address, value);
    }

    /// <summary>
    /// Write raw byte array into memory at variable name location (slow)
    /// </summary>
    /// <param name="path"></param>
    /// <param name="value"></param>
    public void WriteRaw(string path, byte[] value)
    {
        _dllFromMemory.WriteRaw(GetAddress(path), value);
    }

    /// <summary>
    /// Read struct from memory from variable name (slow)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T Read<T>(string path)
    {
        return _dllFromMemory.Read<T>(GetAddress(path));
    }

    /// <summary>
    /// Write struct into memory with variable name (slow)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="value"></param>
    public void Write<T>(string path, T value)
    {
        _dllFromMemory.Write(GetAddress(path), value);
    }

    /// <summary>
    /// Read struct at address
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="address"></param>
    /// <returns></returns>
    public T Read<T>(IntPtr address)
    {
        return _dllFromMemory.Read<T>(address);
    }

    /// <summary>
    /// Write struct at address
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="address"></param>
    /// <param name="value"></param>
    public void Write<T>(IntPtr address, T value)
    {
        _dllFromMemory.Write(address, value);
    }

    /// <summary>
    /// Dispose of the game instance
    /// </summary>
    public void Dispose()
    {
        _dllFromMemory.Dispose();
    }
}