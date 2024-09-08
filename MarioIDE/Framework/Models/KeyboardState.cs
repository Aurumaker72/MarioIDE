using System.Collections.Generic;
using System.Windows.Forms;

namespace MarioIDE.Framework.Models;

public class KeyboardState
{
    private readonly Dictionary<Keys, bool> _keysState = new();

    private readonly Dictionary<Keys, bool> _keysPressed = new();
    private Dictionary<Keys, bool> _lastKeysPressed = new();

    private readonly object _lock = new();

    public bool IsKeyDown(Keys key)
    {
        lock (_lock)
        {
            return GetState(_keysState, key);
        }
    }

    public bool IsKeyPressed(Keys key)
    {
        lock (_lock)
        {
            return GetState(_lastKeysPressed, key);
        }
    }

    internal bool SetKeyState(Keys key, bool state)
    {
        lock (_lock)
        {
            if (state) _keysPressed[key] = true;
            return _keysState[key] = state;
        }
    }

    public void Update()
    {
        lock (_lock)
        {
            _lastKeysPressed = new Dictionary<Keys, bool>(_keysPressed);
            _keysPressed.Clear();
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _keysState.Clear();
        }
    }

    private bool GetState(IReadOnlyDictionary<Keys, bool> dict, Keys key)
    {
        lock (_lock)
        {
            return dict.TryGetValue(key, out bool value) && value;
        }
    }
}