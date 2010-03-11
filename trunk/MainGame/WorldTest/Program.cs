using System;

namespace WorldTest
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (WorldTest game = new WorldTest())
            {
                game.Run();
            }
        }
    }
}

