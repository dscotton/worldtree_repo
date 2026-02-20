// src/Controller.cs
using Raylib_cs;

namespace WorldTree;

/// <summary>
/// Maps keyboard input to game actions.
/// Corresponds to worldtree/controller.py.
/// </summary>
public static class Controller
{
    private static readonly Dictionary<KeyboardKey, InputAction> KeyMap = new()
    {
        { KeyboardKey.Up,    InputAction.Up    },
        { KeyboardKey.Down,  InputAction.Down  },
        { KeyboardKey.Left,  InputAction.Left  },
        { KeyboardKey.Right, InputAction.Right },
        { KeyboardKey.Space, InputAction.Jump  },
        { KeyboardKey.M,     InputAction.Attack},
        { KeyboardKey.N,     InputAction.Shoot },
        { KeyboardKey.Enter, InputAction.Pause },
        { KeyboardKey.F1,    InputAction.Debug },
        { KeyboardKey.W,     InputAction.Up    },
        { KeyboardKey.A,     InputAction.Left  },
        { KeyboardKey.S,     InputAction.Down  },
        { KeyboardKey.D,     InputAction.Right },
    };

    public static HashSet<InputAction> GetInput()
    {
        var active = new HashSet<InputAction>();
        foreach (var (key, action) in KeyMap)
            if (Raylib.IsKeyDown(key))
                active.Add(action);
        return active;
    }

    /// <summary>
    /// Returns true if the given action's key was pressed this frame (not held).
    /// Use this for toggle actions like pause.
    /// </summary>
    public static bool IsActionJustPressed(InputAction action)
    {
        foreach (var (key, a) in KeyMap)
            if (a == action && Raylib.IsKeyPressed(key))
                return true;
        return false;
    }
}
