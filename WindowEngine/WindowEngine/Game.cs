using System;                         // Import basic system functionalities
using OpenTK;                          // Import OpenTK library for graphics, math, and windowing
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;        // Import OpenTK classes for desktop window creation and management
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

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

            // Centers the game window on the screen with a specified size
            // 'new Vector2i(1280, 768)' defines the width and height of the window in pixels
            // 'CenterWindow' is likely a custom or utility method that positions the window 
            // so it appears in the middle of the user's display instead of at the default top-left
            this.CenterWindow(new Vector2i(1280, 768));


        }

        // Additional methods like OnLoad, OnUpdateFrame, OnRenderFrame, etc., can be added here
        // to define game behavior, input handling, and rendering logic

        // Called when the game window is closing or resources need to be released
        protected override void OnUnload()
        {
            // Call the base class method to ensure any built-in cleanup is performed
            base.OnUnload();

            // You can release your custom resources here, e.g., textures, shaders, buffers
            // Example: texture.Dispose(); shader.Dispose();
            // This ensures there are no memory leaks or dangling resources
        }

        // Called every frame to update game logic, physics, or input handling
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
          
            // Call the base class method to maintain any internal update functionality
            base.OnUpdateFrame(args);

            // args.Time provides the time elapsed since the last frame
            // You can use it to make movement or animations frame-rate independent
            // Example: position += speed * (float)args.Time;

            // Handle input, AI logic, collision detection, or any game state updates here
        }

        // Called every frame to render the game visuals
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            // Call the base class method to maintain any built-in rendering functionality
            base.OnRenderFrame(args);

            // 1️⃣ Set the background color to a teal-ish color
            GL.ClearColor(new Color4(0.2f, 0.4f, 0.5f, 1f));

            // 2️⃣ Clear the color buffer to apply the background color
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // At this point, the window is filled with the background color
            // You could draw shapes, models, or other objects here

            // Swap the buffers to display the current frame on the screen
            SwapBuffers();
        }



    }
}