// src/Program.cs
using Raylib_cs;

Raylib.InitWindow(960, 720, "World Tree");
Raylib.SetTargetFPS(60);
while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    Raylib.DrawText("World Tree - Loading...", 100, 350, 24, Color.White);
    Raylib.EndDrawing();
}
Raylib.CloseWindow();
