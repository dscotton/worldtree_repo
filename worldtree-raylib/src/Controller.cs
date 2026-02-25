// src/Controller.cs
using Raylib_cs;

namespace WorldTree;

/// <summary>
/// Maps keyboard and gamepad input to game actions.
/// Corresponds to worldtree/controller.py.
/// </summary>
public static class Controller
{
    private const int GamepadId    = 0;
    private const float AxisDeadZone = 0.3f;

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

    // Physical-position layout (Xbox / PlayStation / Switch Pro all share the same positions).
    // RightFaceDown = A / × / B,  RightFaceLeft = X / □ / Y,  RightFaceUp = Y / △ / X
    private static readonly Dictionary<GamepadButton, InputAction> GamepadMap = new()
    {
        { GamepadButton.LeftFaceUp,    InputAction.Up     }, // D-pad
        { GamepadButton.LeftFaceDown,  InputAction.Down   },
        { GamepadButton.LeftFaceLeft,  InputAction.Left   },
        { GamepadButton.LeftFaceRight, InputAction.Right  },
        { GamepadButton.RightFaceDown, InputAction.Jump   }, // A / ×
        { GamepadButton.RightFaceLeft, InputAction.Attack }, // X / □
        { GamepadButton.RightFaceUp,   InputAction.Shoot  }, // Y / △
        { GamepadButton.MiddleRight,   InputAction.Pause  }, // Start / Options
    };

    public static HashSet<InputAction> GetInput()
    {
        var active = new HashSet<InputAction>();

        // Keyboard
        foreach (var (key, action) in KeyMap)
            if (Raylib.IsKeyDown(key))
                active.Add(action);

        // Gamepad buttons + left stick
        if (Raylib.IsGamepadAvailable(GamepadId))
        {
            foreach (var (button, action) in GamepadMap)
                if (Raylib.IsGamepadButtonDown(GamepadId, button))
                    active.Add(action);

            float axisX = Raylib.GetGamepadAxisMovement(GamepadId, GamepadAxis.LeftX);
            float axisY = Raylib.GetGamepadAxisMovement(GamepadId, GamepadAxis.LeftY);
            if (axisX < -AxisDeadZone) active.Add(InputAction.Left);
            if (axisX >  AxisDeadZone) active.Add(InputAction.Right);
            if (axisY < -AxisDeadZone) active.Add(InputAction.Up);
            if (axisY >  AxisDeadZone) active.Add(InputAction.Down);
        }

        return active;
    }

    /// <summary>
    /// Returns true if the given action was freshly pressed this frame (not held).
    /// Analog stick movement is intentionally excluded — use GetInput() for axes.
    /// </summary>
    public static bool IsActionJustPressed(InputAction action)
    {
        foreach (var (key, a) in KeyMap)
            if (a == action && Raylib.IsKeyPressed(key))
                return true;

        if (Raylib.IsGamepadAvailable(GamepadId))
            foreach (var (button, a) in GamepadMap)
                if (a == action && Raylib.IsGamepadButtonPressed(GamepadId, button))
                    return true;

        return false;
    }
}
