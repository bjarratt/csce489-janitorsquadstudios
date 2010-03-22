using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace WorldTest
{
    class Level
    {
        #region Properties
        private List<List<Int32>> adjacencyList;
        private List<StaticGeometry> levelPieces;
        #endregion

        #region Constructor

        public Level(GraphicsDevice device, ref ContentManager content, string levelFilename)
        {
            this.adjacencyList = new List<List<int>>();
            this.levelPieces = new List<StaticGeometry>();

            // Open the Level file
            FileStream levelFile = new FileStream(levelFilename, FileMode.Open, FileAccess.Read);
            StreamReader levelFileReader = new StreamReader(levelFile);

            string line = levelFileReader.ReadLine();
            string[] splitLine;

            // Loop through each line
            // Line format: terrain_file.obj collision_file.obj index1 index2 index3 ...
            while (line != null)
            {
                if (line == "" || line == "\n")
                {
                    line = levelFileReader.ReadLine();
                    continue;
                }

                List<Int32> currentList = new List<Int32>();

                char[] splitChars = { ' ' };
                splitLine = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                if (splitLine.Count<string>() < 2)
                {
                    continue; // Poorly formatted line; skip
                }

                // Populate the adjacency list from the level file
                for (int i = 2; i < splitLine.Count<string>(); i++)
                {
                    currentList.Add(Convert.ToInt32(splitLine[i]));
                }

                adjacencyList.Add(currentList);

                // Load the StaticGeometry from file name

                StaticGeometry levelPiece = new StaticGeometry(device, splitLine[0], splitLine[1], Vector3.Zero, ref content);
                levelPieces.Add(levelPiece);
            }
        }

        #endregion

        #region Draw

        public void Draw(GraphicsDevice device, ref GameCamera camera)
        {
            int currentLocationIndex = 0;
            levelPieces[currentLocationIndex].Draw(device, true, ref camera);

            for (int i = 0; i < adjacencyList[currentLocationIndex].Count; i++)
            {
                levelPieces[adjacencyList[currentLocationIndex][i]].Draw(device, true, ref camera);
            }
        }

        #endregion
    }
}
