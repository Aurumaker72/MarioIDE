using MarioSharp;
using System;
using System.Collections.Generic;

namespace MarioIDE.Logic;

internal class SaveSlotCollection : IDisposable
{
    public int Length => _saveSlots.Length;
    public SaveSlot this[int index] => _saveSlots[index];
    public IReadOnlyList<SaveSlot> Values => _saveSlots;

    private readonly SaveSlot[] _saveSlots;

    public SaveSlotCollection(GameInstance gameInstance, int length)
    {
        _saveSlots = new SaveSlot[length];
        for (int i = 0; i < _saveSlots.Length; i++)
        {
            _saveSlots[i] = new SaveSlot(gameInstance);
        }
    }

    public void InvalidateSlots(int startFrame)
    {
        foreach (SaveSlot saveSlot in _saveSlots)
            if (saveSlot.Frame >= startFrame)
                saveSlot.Invalidate();
    }

    public void Dispose()
    {
        foreach (SaveSlot saveSlot in _saveSlots)
        {
            saveSlot.Dispose();
        }
    }
}