using MarioIDE.Core.Logic;
using MarioSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarioIDE.Logic;

internal class SaveSlotInfo
{
    public SaveSystemRegion Region { get; }
    public SaveSlot SaveSlot { get; }
    public int Index { get; }

    public SaveSlotInfo(SaveSystemRegion region, SaveSlot saveSlot, int index)
    {
        Region = region;
        SaveSlot = saveSlot;
        Index = index;
    }

    public int DesiredFrame => Region.FirstFrame + Index * Region.SlotPadding;
    public bool NeedUpdate => !SaveSlot.IsValid() || SaveSlot.Frame != DesiredFrame;
}

internal class SaveSystemRegion : IDisposable
{
    public int FirstFrame { get; private set; }
    public int SlotPadding { get; private set; }
    public List<SaveSlot> Slots { get; } = new();

    private readonly SaveSlotCollection _saveSlots;
    private readonly SaveSlot _tempFrame;
    private int _regionSize;

    public SaveSystemRegion(ISaveSystem saveSystem, int maxAllocatedMegabytes)
    {
        _saveSlots = new SaveSlotCollection(saveSystem.GameInstance, (int)(maxAllocatedMegabytes / (saveSystem.GameInstance.SaveSlotSize / 1024.0 / 1024.0)));
        _tempFrame = new SaveSlot(saveSystem.GameInstance);
        Slots.Add(_tempFrame);
        Slots.AddRange(_saveSlots.Values);
    }

    public IEnumerable<SaveSlotInfo> GetSaveSlotsInfo()
    {
        return _saveSlots.Values.Select((s, i) => new SaveSlotInfo(this, s, i));
    }

    public void Update(int firstFrame, int lastFrame)
    {
        _regionSize = lastFrame - firstFrame;
        SlotPadding = Math.Max(1, _regionSize / _saveSlots.Length);
        FirstFrame = firstFrame;
    }

    public void InvalidateFrames(int startFrame)
    {
        _saveSlots.InvalidateSlots(startFrame);
        if (_tempFrame.Frame >= startFrame)
        {
            _tempFrame.Invalidate();
        }
    }

    public float GetProgress()
    {
        int result = 0;
        for (int i = 0; i < _saveSlots.Length; i++)
        {
            SaveSlot slot = _saveSlots[i];
            if (slot.IsValid() && slot.Frame == FirstFrame + i * SlotPadding)
            {
                result++;
            }
        }

        return _saveSlots.Length == 0 || result == 0 ? 0 : result / (float)_saveSlots.Length;
    }

    public void Dispose()
    {
        _saveSlots?.Dispose();
        _tempFrame?.Dispose();
        Slots.Clear();
    }
}