using MarioIDE.Core.Logic;
using MarioSharp;
using MarioSharp.Structs.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MarioIDE.Logic;

internal class SaveSystem : ISaveSystem, IDisposable
{
    public GameInstance GameInstance { get; private set; }
    public SaveSlot PowerOn { get; private set; }
    public SaveSlot CurrentFrame { get; private set; }
    public SaveSlot PreviousFrame { get; private set; }
    public IInputManager InputManager { get; }
    public float Progress { get; private set; }

    private const int MAX_UPDATE_TIME_MILLIS = 8;
    private readonly List<SaveSlot> _saveSlots = new();
    private readonly List<FrameData> _frames = new();
    private int _lastValidFrame = -1;

    private SaveSystemRegion _saveSystemRegionPreviousSequence;
    private SaveSystemRegion _saveSystemRegionCurrentSequence;
    private SaveSystemRegion _saveSystemRegionAll;
    private SaveSlot _tempFrame;
    private bool _initDone;

    public SaveSystem()
    {
        InputManager = new InputManager(this);
    }

    public void Init(GfxType gfxType, byte[] dllBytes)
    {
        GameInstance = new GameInstance(gfxType, dllBytes);

        PowerOn = new SaveSlot(GameInstance);
        CurrentFrame = new SaveSlot(GameInstance);
        PreviousFrame = new SaveSlot(GameInstance);
        _tempFrame = new SaveSlot(GameInstance);
        _saveSystemRegionAll = new SaveSystemRegion(this, 256);
        _saveSystemRegionCurrentSequence = new SaveSystemRegion(this, 128);
        _saveSystemRegionPreviousSequence = new SaveSystemRegion(this, 128);

        _saveSlots.Add(PowerOn);
        _saveSlots.Add(CurrentFrame);
        _saveSlots.Add(PreviousFrame);
        _saveSlots.Add(_tempFrame);
        _saveSlots.AddRange(_saveSystemRegionAll.Slots);
        _saveSlots.AddRange(_saveSystemRegionCurrentSequence.Slots);
        _saveSlots.AddRange(_saveSystemRegionPreviousSequence.Slots);

        GameInstance.Save(PowerOn);
        ResizeFramesArray();
        Reset();

        _initDone = true;
    }

    public void Reset()
    {
        InvalidateFrames(0);
        _frames.Clear();
        ResizeFramesArray();
        GameInstance.Load(PowerOn);
    }

    public FrameData GetFrameData(int frame)
    {
        if (frame < _frames.Count && frame >= 0)
        {
            return _frames[frame];
        }

        return new FrameData();
    }

    public void SetFrameData(int frame, FrameData data)
    {
        if (frame < _frames.Count && frame >= 0)
        {
            _frames[frame] = data;
        }
    }

    public void SetCurrentFrame(int frame)
    {
        if (CurrentFrame.IsValid() && CurrentFrame.Frame == frame && GameInstance.Frame == frame)
        {
            return;
        }

        int targetFrame = Math.Max(0, frame - 1);
        SaveSlot nearestSlot = GetNearestSlot(targetFrame);
        GameInstance.Load(nearestSlot);

        while (GameInstance.Frame < targetFrame)
        {
            UpdateWithInput(false, true);
            UpdateFrameData();
            SaveFrameIfNeeded();
        }

        GameInstance.Save(PreviousFrame);
        UpdateWithInput(false, false);
        UpdateFrameData();
        SaveFrameIfNeeded();
        GameInstance.Save(CurrentFrame);
    }

    public void InvalidateFrames(int startFrame)
    {
        int currentFrame = CurrentFrame.Frame;

        _saveSystemRegionAll.InvalidateFrames(startFrame);
        _saveSystemRegionCurrentSequence.InvalidateFrames(startFrame);
        _saveSystemRegionPreviousSequence.InvalidateFrames(startFrame);
        if (CurrentFrame.Frame >= startFrame) CurrentFrame.Invalidate();
        if (PreviousFrame.Frame >= startFrame) PreviousFrame.Invalidate();
        if (_tempFrame.Frame >= startFrame) _tempFrame.Invalidate();
        
        _lastValidFrame = startFrame - 1;

        SetCurrentFrame(currentFrame);
    }

    public void Tick()
    {
        if (!_initDone) return;
        InputManager.Tick();
        UpdateSaveStates();
    }

    private void SwapGroups()
    {
        SaveSystemRegion a = _saveSystemRegionCurrentSequence;
        SaveSystemRegion b = _saveSystemRegionPreviousSequence;
        _saveSystemRegionCurrentSequence = b;
        _saveSystemRegionPreviousSequence = a;
    }

    private void UpdateSaveStates()
    {
        ResizeFramesArray();

        _saveSystemRegionAll.Update(0, _frames.Count);

        int currentGroupStartFrame = Math.Max(0, (int)Math.Floor(CurrentFrame.Frame / 1000f) * 1000);
        int previousGroupStartFrame = Math.Max(0, currentGroupStartFrame - 1000);

        if (_saveSystemRegionCurrentSequence.FirstFrame == previousGroupStartFrame)
        {
            SwapGroups();
        }
        else if (_saveSystemRegionPreviousSequence.FirstFrame == currentGroupStartFrame)
        {
            SwapGroups();
        }

        _saveSystemRegionCurrentSequence.Update(currentGroupStartFrame, currentGroupStartFrame + 999);
        _saveSystemRegionPreviousSequence.Update(previousGroupStartFrame, previousGroupStartFrame + 999);

        Stopwatch sw = Stopwatch.StartNew();

        while (sw.Elapsed.TotalMilliseconds <= MAX_UPDATE_TIME_MILLIS)
        {
            SaveSlotInfo firstInvalidSlotInfo = null;
            int firstInvalidSlotDesiredFrame = int.MaxValue;

            foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionAll.GetSaveSlotsInfo())
            {
                int desiredFrame = saveSlotInfo.DesiredFrame;
                if (desiredFrame >= 0 && saveSlotInfo.NeedUpdate && desiredFrame < firstInvalidSlotDesiredFrame)
                {
                    firstInvalidSlotDesiredFrame = desiredFrame;
                    firstInvalidSlotInfo = saveSlotInfo;
                }
            }

            foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionCurrentSequence.GetSaveSlotsInfo())
            {
                int desiredFrame = saveSlotInfo.DesiredFrame;
                if (desiredFrame >= 0 && saveSlotInfo.NeedUpdate && desiredFrame < firstInvalidSlotDesiredFrame)
                {
                    firstInvalidSlotDesiredFrame = desiredFrame;
                    firstInvalidSlotInfo = saveSlotInfo;
                }
            }

            foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionPreviousSequence.GetSaveSlotsInfo())
            {
                int desiredFrame = saveSlotInfo.DesiredFrame;
                if (desiredFrame >= 0 && saveSlotInfo.NeedUpdate && desiredFrame < firstInvalidSlotDesiredFrame)
                {
                    firstInvalidSlotDesiredFrame = desiredFrame;
                    firstInvalidSlotInfo = saveSlotInfo;
                }
            }

            if (firstInvalidSlotInfo != null)
            {
                UpdateFrame(sw, firstInvalidSlotInfo, _tempFrame);
                if (sw.Elapsed.TotalMilliseconds > MAX_UPDATE_TIME_MILLIS)
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        UpdateProgress();
    }

    private void SaveFrameIfNeeded()
    {
        foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionAll.GetSaveSlotsInfo())
        {
            if (saveSlotInfo.DesiredFrame == GameInstance.Frame)
            {
                GameInstance.Save(saveSlotInfo.SaveSlot);
            }
        }

        foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionCurrentSequence.GetSaveSlotsInfo())
        {
            if (saveSlotInfo.DesiredFrame == GameInstance.Frame)
            {
                GameInstance.Save(saveSlotInfo.SaveSlot);
            }
        }

        foreach (SaveSlotInfo saveSlotInfo in _saveSystemRegionPreviousSequence.GetSaveSlotsInfo())
        {
            if (saveSlotInfo.DesiredFrame == GameInstance.Frame)
            {
                GameInstance.Save(saveSlotInfo.SaveSlot);
            }
        }
    }

    private void ResizeFramesArray()
    {
        while (_frames.Count < InputManager.GetFrameCount())
        {
            _frames.Add(new FrameData());
        }

        while (_frames.Count > InputManager.GetFrameCount())
        {
            _frames.RemoveAt(_frames.Count - 1);
        }
    }

    private void UpdateFrame(Stopwatch sw, SaveSlotInfo saveSlotInfo, SaveSlot tempSlot)
    {
        SaveSlot nearestSlot = GetNearestSlot(saveSlotInfo.DesiredFrame);
        GameInstance.Load(nearestSlot);
        UpdateFrameData();

        while (GameInstance.Frame != saveSlotInfo.DesiredFrame)
        {
            UpdateWithInput(false, true);
            UpdateFrameData();
            if (sw.Elapsed.TotalMilliseconds > MAX_UPDATE_TIME_MILLIS)
            {
                GameInstance.Save(tempSlot);
                return;
            }
        }

        GameInstance.Save(saveSlotInfo.SaveSlot);
    }

    private void UpdateProgress()
    {
        float p1 = _saveSystemRegionAll.GetProgress();
        float p2 = _saveSystemRegionCurrentSequence.GetProgress();
        float p3 = _saveSystemRegionPreviousSequence.GetProgress();
        Progress = (p1 + p2 + p3) / 3f;
    }

    private void UpdateFrameData()
    {
        if (GameInstance.Frame <= _lastValidFrame)
        {
            return;
        }

        byte area = 0;
        short level = GameInstance.Memory.GCurentLevelNum;
        if (GameInstance.Memory.GMarioStates.areaPointer != IntPtr.Zero)
        {
            area = GameInstance.Read<byte>(GameInstance.Memory.GMarioStates.areaPointer);
        }

        float spdEfficiency = -1.0f;
        
        var prevIndex = GameInstance.Frame - 1;

        if (prevIndex > 0)
        {
            // TODO: Implement this without sacrificing perf
            spdEfficiency = GameInstance.Memory.GMarioStates.GetSpeedEfficiency(0, 0);
        }
        
        SetFrameData(GameInstance.Frame, new FrameData
        {
            Level = level,
            Area = area,
            Action = GameInstance.Memory.GMarioStates.action,
            HSpeed = GameInstance.Memory.GMarioStates.forwardVel,
            YSpeed = GameInstance.Memory.GMarioStates.vel.Y,
            HSlidingSpeed = GameInstance.Memory.GMarioStates.GetHorizontalSlidingSpeed(),
            MarioPos = GameInstance.Memory.GMarioStates.pos,
            CameraYaw = GameInstance.Memory.GetCameraYaw(),
            FaceAngle = GameInstance.Memory.GMarioStates.faceAngle,
            IntendedYaw = GameInstance.Memory.GMarioStates.intendedYaw,
            RngValue = 0,
            SpdEfficiency = spdEfficiency,
            X = GameInstance.Memory.GMarioStates.pos.X,
            Y = GameInstance.Memory.GMarioStates.pos.Y,
            Z = GameInstance.Memory.GMarioStates.pos.Z,
        });
    }

    private void UpdateWithInput(bool render, bool silent)
    {
        OsContPad input = InputManager.GetFrameInput(GameInstance.Frame);
        GameInstance.Update(input, render, silent);
    }

    private SaveSlot GetNearestSlot(int frame)
    {
        FrameInfo nearestFrameInfo = new FrameInfo
        {
            Slot = null,
            Distance = int.MaxValue
        };

        foreach (SaveSlot saveSlot in _saveSlots)
        {
            nearestFrameInfo = CompareFrameInfo(nearestFrameInfo, frame, saveSlot);
            if (nearestFrameInfo.Slot != null && nearestFrameInfo.Slot.Frame == frame - 1)
            {
                return nearestFrameInfo.Slot;
            }
        }

        return nearestFrameInfo.Slot ?? PowerOn;
    }

    private static FrameInfo CompareFrameInfo(FrameInfo frameInfo, int frame, SaveSlot nextSlot)
    {
        if (nextSlot != null && nextSlot.IsValid() && nextSlot.Frame < frame)
        {
            int distance = frame - nextSlot.Frame;
            if (distance < frameInfo.Distance)
            {
                frameInfo.Distance = distance;
                frameInfo.Slot = nextSlot;
            }
        }

        return frameInfo;
    }

    private struct FrameInfo
    {
        public SaveSlot Slot { get; set; }
        public int Distance { get; set; }
    }

    public void Dispose()
    {
        GameInstance?.Dispose();
        GameInstance = null;

        _saveSystemRegionPreviousSequence?.Dispose();
        _saveSystemRegionCurrentSequence?.Dispose();
        _saveSystemRegionAll?.Dispose();
        _tempFrame?.Dispose();
        PowerOn?.Dispose();
        CurrentFrame?.Dispose();
        PreviousFrame?.Dispose();
    }
}