using System;                         // Import basic system functionalities
using OpenTK;                          // Import OpenTK library for graphics, math, and windowing
using OpenTK.Windowing.Desktop;        // Import OpenTK classes for desktop window creation and management

namespace WindowEngine
{
    // Game class inherits from GameWindow, which provides a window and a game loop
    public class Game : GameWindow
    {
        // Constructor for the Game class
        public Game()
            // Call the base class constructor (GameWindow) with default settings
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            // You could initialize game-specific variables or settings here
            // Example: setting window title, size, or other properties
        }

        // Additional methods like OnLoad, OnUpdateFrame, OnRenderFrame, etc., can be added here
        // to define game behavior, input handling, and rendering logic
    }
}