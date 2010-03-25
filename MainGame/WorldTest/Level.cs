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
    struct GeometryConnector
    {
        public int index;

        // Own points
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
    }

    class Level
    {
        #region Properties

        private List<List<GeometryConnector>> adjacencyList;
        private List<StaticGeometry> levelPieces;
        private List<Light> lights;
        #endregion

        #region Constructor

        public Level(GraphicsDevice device, ref ContentManager content, ref List<Light> lights, string levelFilename)
        {
            this.lights = lights;
            this.ReadInLevel(levelFilename);

            // Load the StaticGeometry elements
            
            if (levelPieces.Count == 0)
            {
                return;
            }

            levelPieces[0].Load(device, ref content, Matrix.Identity);

            for ( int i = 1; i < adjacencyList.Count; i++ )
            {
                int currentIndex = adjacencyList[i][0].index;
                Matrix worldMatrix = Matrix.Identity;
                bool success = false;
                for ( int j = 0; j < adjacencyList[currentIndex].Count; j++ )
                {
                    if (adjacencyList[currentIndex][j].index == i)
                    {
                        Vector3 baseNormal = -Vector3.Cross(adjacencyList[i][0].p2 - adjacencyList[i][0].p1,
                                                         adjacencyList[i][0].p3 - adjacencyList[i][0].p1);
                        baseNormal.Normalize();

                        Vector3 childNormal = Vector3.Cross(adjacencyList[currentIndex][j].p2 - adjacencyList[currentIndex][j].p1,
                                                             adjacencyList[currentIndex][j].p3 - adjacencyList[currentIndex][j].p1);
                        childNormal.Normalize();

                        worldMatrix = CreateSnapMatrix(adjacencyList[i][0].p3,
                                                       baseNormal,
                                                       adjacencyList[currentIndex][j].p1,
                                                       childNormal);
                        success = true;
                    }
                }
                if (success)
                {
                    levelPieces[i].Load(device, ref content, worldMatrix);
                }
            }
        }

        private void ReadInLevel(string levelFilename)
        {
            this.adjacencyList = new List<List<GeometryConnector>>();
            this.levelPieces = new List<StaticGeometry>();

            // Open the Level file
            FileStream levelFile = new FileStream(levelFilename, FileMode.Open, FileAccess.Read);
            StreamReader levelFileReader = new StreamReader(levelFile);

            string line = levelFileReader.ReadLine();
            string[] splitLine;

            // Loop through each line
            // Line format: terrain_file.obj collision_file.obj index1 v1 v2 v3 ...
            while (line != null)
            {
                if (line == "" || line == "\n")
                {
                    line = levelFileReader.ReadLine();
                    continue;
                }

                List<GeometryConnector> currentList = new List<GeometryConnector>();

                char[] splitChars = { ' ' };
                splitLine = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                if (splitLine.Count<string>() < 2)
                {
                    line = levelFileReader.ReadLine();
                    continue; // Poorly formatted line; skip
                }

                // Populate the adjacency list from the level file
                for (int i = 2; i < splitLine.Count<string>(); i += 10)
                {
                    GeometryConnector connect = new GeometryConnector();
                    connect.index = Convert.ToInt32(splitLine[i]);
                    connect.p1 = new Vector3((float)Convert.ToDouble(splitLine[i + 1]),
                                             (float)Convert.ToDouble(splitLine[i + 2]),
                                             (float)Convert.ToDouble(splitLine[i + 3]));
                    connect.p2 = new Vector3((float)Convert.ToDouble(splitLine[i + 4]),
                                             (float)Convert.ToDouble(splitLine[i + 5]),
                                             (float)Convert.ToDouble(splitLine[i + 6]));
                    connect.p3 = new Vector3((float)Convert.ToDouble(splitLine[i + 7]),
                                             (float)Convert.ToDouble(splitLine[i + 8]),
                                             (float)Convert.ToDouble(splitLine[i + 9]));
                    currentList.Add(connect);
                }

                adjacencyList.Add(currentList);

                // Load the StaticGeometry from file name
                StaticGeometry levelPiece = new StaticGeometry(splitLine[0], splitLine[1], Vector3.Zero, ref lights);

                levelPieces.Add(levelPiece);

                line = levelFileReader.ReadLine();
            }
        }

        #endregion

        private Matrix CreateSnapMatrix(Vector3 basePoint, Vector3 baseNormal, Vector3 childPoint, Vector3 childNormal)
        {
            Quaternion rotationQuat = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.Acos(Vector3.Dot(baseNormal, childNormal)));
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotationQuat);
            Vector3 rotatedChildPoint = Vector3.Transform(childPoint, rotationMatrix);
            Vector3 translation = basePoint - rotatedChildPoint;
            rotationMatrix.Translation = translation;

            return rotationMatrix;
        }

        #region Collision Detection

        public Vector3 CollideWith(Vector3 originalPosition, Vector3 velocityVector, double radius)
        {
            Vector3 newPosition = levelPieces[0].CollideWith(originalPosition, velocityVector, radius, StaticGeometry.MAX_RECURSIONS);
            //if (newPosition == originalPosition + velocityVector)
            //{
            //    newPosition = levelPieces[1].CollideWith(newPosition, velocityVector, radius, StaticGeometry.MAX_RECURSIONS);
            //}
            return newPosition;
        }

        #endregion

        #region Draw

        public void Draw(GraphicsDevice device, ref GameCamera camera)
        {
            //int currentLocationIndex = 0;
            //levelPieces[currentLocationIndex].Draw(device, true, ref camera);

            int currentLocationIndex = 0;
            levelPieces[currentLocationIndex].Draw(device, true, ref camera);

            for (int i = 0; i < adjacencyList[currentLocationIndex].Count; i++)
            {
                levelPieces[adjacencyList[currentLocationIndex][i].index].Draw(device, true, ref camera);
            }
            //levelPieces[1].Draw(device, true, ref camera);
        }

        #endregion

    }
}
