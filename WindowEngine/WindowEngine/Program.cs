using System;           // Import basic system functionalities like Console, Math, etc.
using WindowEngine;     // Import the WindowEngine namespace, which contains Game class and other related classes

namespace WindowEngine
{
    // Entry point of the application
    class Program
    {
        // Main method: the starting point of every C# console application
        static void Main(string[] args)
        {
            // 'using' ensures proper disposal of resources when the Game object is no longer needed
            using (Game game = new Game())
            {
                // Start the game loop
                // The Run() method usually contains the main update-render loop
                game.Run();
            } // At this point, the 'game' object is automatically disposed, freeing any resources it used
        }
    }
}