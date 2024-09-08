using System.Numerics;
using MarioSharp.Structs;
using MarioSharp.Structs.Math;
using Vector3 = OpenTK.Vector3;

namespace MarioSharp;

public class GameMemory
{
    public IntPtr SSurfacePool => _gameInstance.Read<IntPtr>("sSurfacePool");

    public short GCurentLevelNum
    {
        get => _gameInstance.Read<short>("gCurrLevelNum");
        set => _gameInstance.Write("gCurrLevelNum", value);
    }

    public int GGlobalTimer
    {
        get => _gameInstance.Read<int>("gGlobalTimer");
        set => _gameInstance.Write("gGlobalTimer", value);
    }

    // Whatever
    // public ushort GRandomSeed16 => _gameInstance.Read<ushort>("gRandomSeed16");
    
    public int GSurfacesAllocated
    {
        get => _gameInstance.Read<int>("gSurfacesAllocated");
        set => _gameInstance.Write("gSurfacesAllocated", value);
    }

    public MarioState GMarioStates
    {
        get => _gameInstance.Read<MarioState>("gMarioStates");
        set => _gameInstance.Write<MarioState>("gMarioStates", value);
    }

    public LakituState GLakituState
    {
        get => _gameInstance.Read<LakituState>("gLakituState");
        set => _gameInstance.Write<LakituState>("gLakituState", value);
    }

    public OverrideCamera GOverrideCamera
    {
        get => _gameInstance.Read<OverrideCamera>("gOverrideCamera");
        set => _gameInstance.Write<OverrideCamera>("gOverrideCamera", value);
    }

    public float GOverrideFarClip
    {
        get => _gameInstance.Read<float>("gOverrideFarClip");
        set => _gameInstance.Write<float>("gOverrideFarClip", value);
    }

    public Matrix4x4 GMatrixPerspectiveOverride
    {
        get => _gameInstance.Read<Matrix4x4>("gMatrixPerspectiveOverride");
        set => _gameInstance.Write<Matrix4x4>("gMatrixPerspectiveOverride", value);
    }

    public bool GOverridePerspectiveMatrix
    {
        get => _gameInstance.Read<bool>("gOverridePerspectiveMatrix");
        set => _gameInstance.Write<bool>("gOverridePerspectiveMatrix", value);
    }

    public Matrix4x4 GMatrixOverride
    {
        get => _gameInstance.Read<Matrix4x4>("gMatrixOverride");
        set => _gameInstance.Write<Matrix4x4>("gMatrixOverride", value);
    }

    public bool GOverrideMatrix
    {
        get => _gameInstance.Read<bool>("gOverrideMatrix");
        set => _gameInstance.Write<bool>("gOverrideMatrix", value);
    }

    public bool GOverrideCulling
    {
        get => _gameInstance.Read<bool>("gOverrideCulling");
        set => _gameInstance.Write<bool>("gOverrideCulling", value);
    }

    public bool GHideHud
    {
        get => _gameInstance.Read<bool>("gHideHud");
        set => _gameInstance.Write<bool>("gHideHud", value);
    }

    public Matrix4x4 GMatrix => _gameInstance.Read<Matrix4x4>("gMatrix");
    public Matrix4x4 GMatrixPerspective => _gameInstance.Read<Matrix4x4>("gMatrixPerspective");

    private delegate void GeoAddChildDelegate(IntPtr parent, IntPtr child);
    private delegate void InitGraphNodeObjectDelegate(IntPtr pool, IntPtr graphNode, IntPtr sharedChild, Vector3 pos, Vec3S angle, Vector3 scale);
    private delegate ushort Atan2SDelegate(float z, float x);

    private readonly GeoAddChildDelegate _geoAddChild;
    private readonly InitGraphNodeObjectDelegate _initGraphNodeObject;
    private readonly Atan2SDelegate _atan;

    private readonly GameInstance _gameInstance;

    public GameMemory(GameInstance gameInstance)
    {
        _gameInstance = gameInstance;
        _geoAddChild = _gameInstance.GetDelegateFromFuncName<GeoAddChildDelegate>("geo_add_child");
        _initGraphNodeObject = _gameInstance.GetDelegateFromFuncName<InitGraphNodeObjectDelegate>("init_graph_node_object");
        _atan = _gameInstance.GetDelegateFromFuncName<Atan2SDelegate>("atan2s");
    }

    public IntPtr GetLoadedGraphNodePtr(int index)
    {
        return _gameInstance.Read<IntPtr>(_gameInstance.GetAddress("gLoadedGraphNodes")) + index * 0x8;
    }

    public IntPtr GetObjectPtr(int index)
    {
        return _gameInstance.GetAddress("gObjectPool") + index * 0x570;
    }

    public void InitGraphNodeObject(IntPtr graphNodeObject)
    {
        _initGraphNodeObject(IntPtr.Zero, graphNodeObject, IntPtr.Zero, Vector3.Zero, Vec3S.Zero, Vector3.Zero);
    }

    public void GeoAddChild(IntPtr parent, IntPtr child)
    {
        _geoAddChild(parent, child);
    }

    public short GetCameraYaw()
    {
        GameMemory memory = _gameInstance.Memory;
        MarioState marioState = memory.GMarioStates;

        short cameraYaw = 0;
        if (marioState.areaPointer != IntPtr.Zero)
        {
            Area area = _gameInstance.Read<Area>(marioState.areaPointer);
            if (area.CameraPtr != IntPtr.Zero)
            {
                Camera camera = _gameInstance.Read<Camera>(area.CameraPtr);
                cameraYaw = camera.Yaw;
            }
        }

        return cameraYaw;
    }

    public ushort ATan(float z, float x)
    {
        return _atan(z, x);
    }
}