using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using XNAnimation;
using XNAnimation.Controllers;
using XNAnimation.Effects;

namespace WorldTest
{
    public class NavMeshNode
    {
        private int index;
        private bool obstacle;

        private Vector3 v0;
        private Vector3 v1;
        private Vector3 v2;
        private Vector3 centroid;
        private List<int> adjacent_polygons;

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public bool Obstacle
        {
            get { return obstacle; }
            set { obstacle = value; }
        }

        public Vector3 V0
        {
            get { return v0; }
            set { v0 = value; }
        }

        public Vector3 V1
        {
            get { return v1; }
            set { v1 = value; }
        }

        public Vector3 V2
        {
            get { return v2; }
            set { v2 = value; }
        }

        public Vector3 Centroid
        {
            get { return centroid; }
            set { centroid = value; }
        }

        public List<int> Adjacent
        {
            get { return adjacent_polygons; }
            set { adjacent_polygons = value; }
        }
    }

    public class Path<NavMeshNode> : IEnumerable<NavMeshNode>
    {
        public NavMeshNode LastStep { get; private set; }
        public Path<NavMeshNode> PreviousSteps { get; private set; }
        public double TotalCost { get; private set; }

        private Path(NavMeshNode lastStep, Path<NavMeshNode> previousSteps, double totalCost)
        {
            LastStep = lastStep;
            PreviousSteps = previousSteps;
            TotalCost = totalCost;
        }

        public Path(NavMeshNode start) : this(start, null, 0) { }

        public Path<NavMeshNode> AddStep(NavMeshNode step, double stepCost)
        {
            return new Path<NavMeshNode>(step, this, TotalCost + stepCost);
        }

        public IEnumerator<NavMeshNode> GetEnumerator()
        {
            for (Path<NavMeshNode> p = this; p != null; p = p.PreviousSteps)
                yield return p.LastStep;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class PriorityQueue<P, V>
    {
        private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();

        public void Enqueue(P priority, V value)
        {
            Queue<V> q;
            if (!list.TryGetValue(priority, out q))
            {
                q = new Queue<V>();
                list.Add(priority, q);
            }
            q.Enqueue(value);
        }

        public V Dequeue()
        {
            // will throw if there isn’t any first element!
            var pair = list.First();
            var v = pair.Value.Dequeue();
            if (pair.Value.Count == 0) // nothing left of the top priority.
                list.Remove(pair.Key);
            return v;
        }

        public bool IsEmpty
        {
            get { return !list.Any(); }
        }
    }

    public class NavigationMesh
    {
        /// <summary>
        /// File name to read from to construct the navigation mesh.
        /// </summary>
        private string fileName;

        /// <summary>
        /// Big Data Structure... this will be shared by all the enemies
        /// and they will use it to run a dynamic A* search to find the 
        /// optimal path to the player.
        /// </summary>
        public List<NavMeshNode> NavMesh;

        #region Constructor

        public NavigationMesh(string fileName)
        {
            this.fileName = fileName;
        }

        #endregion

        #region Load
        #endregion

        public bool intersect_RayTriangle(Ray R, NavMeshNode node)
        {
            //Vector3 u, v, n;             // triangle vectors
            //Vector3 dir, w0, w;          // ray vectors

            Vector3 intersection = Vector3.Zero;
            float r, a, b;             // params to calc ray-plane intersect
         
            // get triangle edge vectors and plane normal
            Vector3 u = node.V1 - node.V0;
            Vector3 v = node.V2 - node.V0;
            Vector3 n = Vector3.Cross(u,v);             // cross product
            if (n == Vector3.Zero)            // triangle is degenerate
                return false;                 // do not deal with this case

            Vector3 w0 = R.Position - node.V0;
            a = -Vector3.Dot(n, w0);
            b = Vector3.Dot(n, R.Direction);
            if (Math.Abs(b) < 0.00001f)
            {     // ray is parallel to triangle plane
                if (a == 0) {}//return true;                // ray lies in triangle plane
                else return false;             // ray disjoint from plane
            }

            // get intersect point of ray with triangle plane
            r = a / b;
            if (r < 0.0f)                   // ray goes away from triangle
                return false;                  // => no intersect
            // for a segment, also test if (r > 1.0) => no intersect

            intersection = R.Position + r * R.Direction;           // intersect point of ray and plane
            
            // is I inside T?
            float uu, uv, vv, wu, wv, D;
            uu = Vector3.Dot(u, u);
            uv = Vector3.Dot(u, v);
            vv = Vector3.Dot(v, v);
            Vector3 w = intersection - node.V0;
            wu = Vector3.Dot(w, u);
            wv = Vector3.Dot(w, v);
            D = uv * uv - uu * vv;

            // get and test parametric coords
            float s, t;
            s = (uv * wv - vv * wu) / D;
            if (s < 0.0 || s > 1.0)        // I is outside T
                return false;
            t = (uv * wu - uu * wv) / D;
            if (t < 0.0 || (s + t) > 1.0)  // I is outside T
                return false;

            return true;                      // I is in T
        }



        /*private ArrayList LoadFromOBJ(string filename, Matrix worldMatrix)
        {
            ArrayList positionList = new ArrayList(); // List of vertices in order of OBJ file
            ArrayList normalList = new ArrayList();
            ArrayList textureCoordList = new ArrayList();

            // OBJ indices start with 1, not 0, so we add a dummy value in the 0 slot
            positionList.Add(new Vector3());
            normalList.Add(new Vector3());
            textureCoordList.Add(new Vector3());

            ArrayList triangleList = new ArrayList(); // List of triangles (every 3 vertices is a triangle)

            VertexPositionNormalTexture currentVertex;

            if (filename == null || filename == "")
            {
                return triangleList;
            }

            FileStream objFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader objFileReader = new StreamReader(objFile);

            string line = objFileReader.ReadLine();
            string[] splitLine;

            string[] splitVertex;

            float textureScaleFactor = 1.0f;

            while (line != null)
            {
                if (line == "" || line == "\n")
                {
                    line = objFileReader.ReadLine();
                    continue;
                }

                char[] splitChars = { ' ' };
                splitLine = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                if (splitLine[0] == "v") // Position
                {
                    Vector3 position = new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3]));


                    positionList.Add(Vector3.Transform(position, worldMatrix));
                }
                else if (splitLine[0] == "vn") // Normal
                {
                    Vector3 normal = new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3]));
                    normalList.Add(Vector3.TransformNormal(normal, worldMatrix));
                }
                else if (splitLine[0] == "vt") // Texture Coordinate
                {
                    textureCoordList.Add(new Vector3((float)Convert.ToDouble(splitLine[1]) * textureScaleFactor, (float)Convert.ToDouble(splitLine[2]) * textureScaleFactor, (float)Convert.ToDouble(splitLine[3])));
                }
                else if (splitLine[0] == "f") // Face (each vertex is Position/Texture/Normal)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        splitVertex = splitLine[i].Split('/');
                        if (splitVertex[0] != "")
                        {
                            currentVertex.Position = (Vector3)positionList[Convert.ToInt32(splitVertex[0])];
                        }
                        else
                        {
                            currentVertex.Position = new Vector3(0.0f);
                        }

                        if (splitVertex[2] != "")
                        {
                            currentVertex.Normal = (Vector3)normalList[Convert.ToInt32(splitVertex[2])];
                        }
                        else
                        {
                            currentVertex.Normal = new Vector3(0.0f);
                        }

                        if (splitVertex[1] != "")
                        {
                            currentVertex.TextureCoordinate = new Vector2(((Vector3)textureCoordList[Convert.ToInt32(splitVertex[1])]).X, ((Vector3)textureCoordList[Convert.ToInt32(splitVertex[1])]).Y);
                        }
                        else
                        {
                            currentVertex.TextureCoordinate = new Vector2(0.0f);
                        }

                        triangleList.Add(currentVertex);
                    }
                }
                else // Unused line format, skipping
                {

                }

                line = objFileReader.ReadLine();
            }

            return triangleList;
        }*/

    }
}
