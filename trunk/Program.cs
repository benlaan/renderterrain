using System;

namespace Laan.DLOD
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TerrainViewer game = new TerrainViewer())
            {
                game.Run();
            }
        }
    }
}

