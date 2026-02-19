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
        { KeyboardKey.Enter, InputAction.Start },
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
}
