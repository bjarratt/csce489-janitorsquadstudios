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

    struct CollisionPolygon
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;
        public Vector3 normal;
    }

    class Level
    {
        #region Properties

        private List<List<GeometryConnector>> adjacencyList;
        private List<StaticGeometry> levelPieces;

        private Effect cel_effect;
        private Texture2D m_celMap;
        private Texture2D terrainTexture;

        // Collision mesh variables
        private List<CollisionPolygon> collisionMesh;
        private Vector3 collisionMeshOffset;
        private VertexBuffer collisionVertexBuffer;
        private VertexDeclaration collisionVertexDeclaration;
        private int collisionVertexCount;

        // Navigation mesh variables
        private List<NavMeshNode> navigationMesh;
        private Vector3 navigationMeshOffset;
        private VertexBuffer navigationVertexBuffer;
        private VertexDeclaration navigationVertexDeclaration;
        private int navigationVertexCount;

        public const int MAX_COLLISIONS = 5;

        #endregion

        #region Constructor

        public Level(GraphicsDevice device, ref ContentManager content, string levelFilename)
        {
            this.collisionMesh = new List<CollisionPolygon>();
            this.collisionMeshOffset = Vector3.Zero;

            List<VertexPositionNormalTexture> collisionVertices = new List<VertexPositionNormalTexture>(); // Stores collision vertices for VertexBuffer creation

            this.navigationMesh = new List<NavMeshNode>();

            this.ReadInLevel(levelFilename);

            //
            // Prepare navigation mesh
            //

            this.navigationMeshOffset = Vector3.Zero;
            List<VertexPositionNormalTexture> navigationVertices = new List<VertexPositionNormalTexture>();

            //
            // Load the StaticGeometry elements
            //
            
            if (levelPieces.Count == 0)
            {
                return;
            }

            // Load the first level piece
            levelPieces[0].Load(device, ref content, Matrix.Identity);
            collisionVertices.AddRange(this.LoadCollisionMesh(levelPieces[0].CollisionMeshFilename, Matrix.Identity, Vector3.Zero, ref this.collisionMesh));
            navigationVertices.AddRange(this.LoadNavigationMesh(levelPieces[0].NavigationMeshFilename, Matrix.Identity, Vector3.Zero));

            // Load the remaining level pieces
            for ( int i = 1; i < adjacencyList.Count; i++ )
            {
                int currentIndex = adjacencyList[i][0].index;
                Matrix worldMatrix = Matrix.Identity;

                // Find the anchor of the current piece to load the current piece in the right configuration
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

                        // Load the StaticGeometry piece
                        levelPieces[i].Load(device, ref content, worldMatrix);

                        // Add the piece's collision mesh to the level's collision mesh
                        collisionVertices.AddRange(this.LoadCollisionMesh(levelPieces[i].CollisionMeshFilename, worldMatrix, Vector3.Zero, ref this.collisionMesh));

                        // Add the piece's navigation mesh to the level's navigation mesh
                        navigationVertices.AddRange(this.LoadNavigationMesh(levelPieces[i].NavigationMeshFilename, worldMatrix, Vector3.Zero));
                        break;
                    }
                }
            }

            //
            // Initialize collision vertex buffer and collision mesh
            //

            VertexPositionNormalTexture[] collisionVerticesArray = collisionVertices.ToArray();
            this.collisionVertexBuffer = new VertexBuffer(device, collisionVerticesArray.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.collisionVertexBuffer.SetData(collisionVerticesArray);

            this.collisionVertexCount = collisionVerticesArray.Length;

            this.collisionVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);

            //
            // Initialize navigation vertex buffer and navigation mesh
            //

            VertexPositionNormalTexture[] navigationVerticesArray = navigationVertices.ToArray();
            this.navigationVertexBuffer = new VertexBuffer(device, navigationVerticesArray.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.navigationVertexBuffer.SetData(navigationVerticesArray);

            this.navigationVertexCount = navigationVerticesArray.Length;

            this.navigationVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Read in a level from the given filename
        /// </summary>
        /// <param name="levelFilename">Name of level file</param>
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

                if (splitLine.Count<string>() < 2) // Poorly formatted line; skip
                {
                    line = levelFileReader.ReadLine();
                    continue;
                }

                // Populate the adjacency list from the level file
                for (int i = 3; i < splitLine.Count<string>(); i += 10)
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
                StaticGeometry levelPiece = new StaticGeometry(splitLine[0], splitLine[1], splitLine[2]); // splitLine[1] is collisionMeshFilename

                levelPieces.Add(levelPiece);

                // Read the next line
                line = levelFileReader.ReadLine();
            }
        }

        /// <summary>
        /// Create a matrix to rotate/translate the child to line up with the base
        /// </summary>
        /// <param name="basePoint">Point in base plane</param>
        /// <param name="baseNormal">Normal to base plane</param>
        /// <param name="childPoint">Point in child plane</param>
        /// <param name="childNormal">Normal to child plane</param>
        /// <returns></returns>
        private Matrix CreateSnapMatrix(Vector3 basePoint, Vector3 baseNormal, Vector3 childPoint, Vector3 childNormal)
        {
            float baseDotChild = Vector3.Dot(baseNormal, childNormal);
            MathHelper.Clamp(baseDotChild, -1.0f, 1.0f);
            Quaternion rotationQuat = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.Acos(MathHelper.Clamp(baseDotChild, -1.0f, 1.0f)));
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotationQuat);
            Vector3 rotatedChildPoint = Vector3.Transform(childPoint, rotationMatrix);
            Vector3 translation = basePoint - rotatedChildPoint;
            rotationMatrix.Translation = translation;

            return rotationMatrix;
        }

        #endregion

        #region Load

        public void Load(GraphicsDevice device, ref ContentManager content)
        {
            cel_effect = content.Load<Effect>("CelShade");
            m_celMap = content.Load<Texture2D>("Toon2");
            terrainTexture = content.Load<Texture2D>("tex_small");
        }

        /// <summary>
        /// Read a collision mesh in from an OBJ file
        /// </summary>
        /// <param name="filename">Collision mesh filename</param>
        /// <param name="worldMatrix">Matrix to transform mesh by</param>
        /// <param name="collisionPolygons">Ref to collision or navigation mesh</param>
        /// <returns>List of mesh vertices</returns>
        private List<VertexPositionNormalTexture> LoadCollisionMesh(string filename, Matrix worldMatrix, Vector3 meshOffset, ref List<CollisionPolygon> collisionPolygons)
        {
            ArrayList positionList = new ArrayList(); // List of vertices in order of OBJ file
            ArrayList normalList = new ArrayList();
            ArrayList textureCoordList = new ArrayList();

            // OBJ indices start with 1, not 0, so we add a dummy value in the 0 slot
            positionList.Add(new Vector3());
            normalList.Add(new Vector3());
            textureCoordList.Add(new Vector3());

            List<VertexPositionNormalTexture> triangleList = new List<VertexPositionNormalTexture>(); // List of vertices (every 3 vertices is a triangle)

            VertexPositionNormalTexture currentVertex;

            //Vector3 largestValues = new Vector3(-10000);
            //Vector3 smallestValues = new Vector3(10000);

            // Variables used for collision meshes
            CollisionPolygon currentPolygon;
            currentPolygon.v1 = Vector3.Zero;
            currentPolygon.v2 = Vector3.Zero;
            currentPolygon.v3 = Vector3.Zero;
            currentPolygon.normal = Vector3.Zero;
            Vector3 polygonVector1;
            Vector3 polygonVector2;

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

            // Read OBJ file line-by-line
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
                    //if (position.X > largestValues.X)
                    //{
                    //    largestValues = position;
                    //}

                    //if (position.X < smallestValues.X)
                    //{
                    //    smallestValues = position;
                    //}
 
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
                    for (int i = 1; i < 4; i++) // Read each of the three vertices
                    {
                        splitVertex = splitLine[i].Split('/');

                        //
                        // Set the vertices for the mesh (to be returned by this function)
                        //

                        if (splitVertex[0] != "")
                        {
                            currentVertex.Position = (Vector3)positionList[Convert.ToInt32(splitVertex[0])];
                            currentVertex.Position += meshOffset;
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

                        //
                        // Set the CollisionPolygons for the mesh
                        //

                        if (i == 1)
                        {
                            currentPolygon.v1 = currentVertex.Position;
                        }
                        else if (i == 2)
                        {
                            currentPolygon.v2 = currentVertex.Position;
                        }
                        else if (i == 3)
                        {
                            currentPolygon.v3 = currentVertex.Position;

                            polygonVector1 = currentPolygon.v1 - currentPolygon.v2;
                            polygonVector2 = currentPolygon.v3 - currentPolygon.v2;

                            Vector3.Cross(ref polygonVector1, ref polygonVector2, out currentPolygon.normal);
                            currentPolygon.normal.Normalize();

                            collisionPolygons.Add(currentPolygon);
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
        }

        private List<VertexPositionNormalTexture> LoadNavigationMesh(string filename, Matrix worldMatrix, Vector3 meshOffset)
        {
            List<NavMeshVertex> positionList = new List<NavMeshVertex>(); // List of vertices in order of OBJ file
            ArrayList normalList = new ArrayList();

            // OBJ indices start with 1, not 0, so we add a dummy value in the 0 slot
            positionList.Add(new NavMeshVertex());
            normalList.Add(new Vector3());

            List<VertexPositionNormalTexture> triangleList = new List<VertexPositionNormalTexture>(); // List of vertices (every 3 vertices is a triangle)

            VertexPositionNormalTexture currentVertex;

            NavMeshVertex currentNavVertex = new NavMeshVertex();

            if (filename == null || filename == "")
            {
                return triangleList;
            }

            FileStream objFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader objFileReader = new StreamReader(objFile);

            string line = objFileReader.ReadLine();
            string[] splitLine;

            string[] splitVertex;

            // Read OBJ file line-by-line
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
                    currentNavVertex.position = Vector3.Transform(position, worldMatrix);
                    currentNavVertex.adjacentFaces = new List<int>();
                    positionList.Add(currentNavVertex);
                }
                else if (splitLine[0] == "vn") // Normal
                {
                    Vector3 normal = new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3]));
                    normalList.Add(Vector3.TransformNormal(normal, worldMatrix));
                }
                else if (splitLine[0] == "f") // Face (each vertex is Position/Texture/Normal)
                {
                    NavMeshNode currentFace = new NavMeshNode();
                    currentFace.adjacent_polygons = new List<int>();

                    for (int i = 1; i < 4; i++) // Read each of the three vertices
                    {
                        int currentVertexIndex = -1;

                        splitVertex = splitLine[i].Split('/');

                        //
                        // Set the vertices for the mesh (to be returned by this function)
                        //

                        if (splitVertex[0] != "")
                        {
                            currentVertexIndex = Convert.ToInt32(splitVertex[0]);
                            currentVertex.Position = positionList[currentVertexIndex].position;
                            currentVertex.Position += meshOffset;
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

                        currentVertex.TextureCoordinate = new Vector2(0.0f);

                        //
                        // Set the NavMeshNode for the mesh
                        //

                        if (currentVertexIndex > 0)
                        {
                            // Add face to vertex's list
                            positionList[currentVertexIndex].adjacentFaces.Add(navigationMesh.Count);
                        }

                        if (i == 1)
                        {
                            currentFace.V0 = currentVertex.Position;
                        }
                        else if (i == 2)
                        {
                            currentFace.V1 = currentVertex.Position;
                        }
                        else if (i == 3)
                        {
                            currentFace.V2 = currentVertex.Position;

                            currentFace.Centroid = (currentFace.V0 + currentFace.V1 + currentFace.V2) / 3.0f;
                            currentFace.Index = navigationMesh.Count;
                            navigationMesh.Add(currentFace);
                        }

                        triangleList.Add(currentVertex);
                    }
                }
                else // Unused line format, skipping
                {

                }

                line = objFileReader.ReadLine();
            }

            // Set face adjacency info from vertices
            for (int i = 1; i < positionList.Count; i++)
            {
                for (int j = 0; j < positionList[i].adjacentFaces.Count; j++)
                {
                    this.navigationMesh[positionList[i].adjacentFaces[j]].adjacent_polygons.AddRange(positionList[i].adjacentFaces);
                }
            }

            // Remove duplicate face adjacency entries
            for (int i = 0; i < this.navigationMesh.Count; i++)
            {
                this.navigationMesh[i].adjacent_polygons = new List<int>(this.navigationMesh[i].adjacent_polygons.Distinct<int>());
            }

            return triangleList;
        }
        #endregion

        #region CollisionDetection

        private bool pointInsidePolygon(Vector3 point, CollisionPolygon polygon)
        {
            Vector3 vec1;
            Vector3 vec2;
            
            double currentAngle = 0.0f;
            double angle1;
            double angle2;
            double angle3;

            vec1 = polygon.v1 - point;
            vec2 = polygon.v2 - point;

            vec1.Normalize();
            vec2.Normalize();

            angle1 = Math.Acos(Vector3.Dot(vec1, vec2)) * Vector3.Dot(polygon.normal, Vector3.Cross(vec1, vec2));
            currentAngle += angle1;

            vec1 = vec2;
            vec2 = polygon.v3 - point;

            vec1.Normalize();
            vec2.Normalize();

            angle2 = Math.Acos(Vector3.Dot(vec1, vec2)) * Vector3.Dot(polygon.normal, Vector3.Cross(vec1, vec2));
            currentAngle += angle2;

            vec1 = vec2;
            vec2 = polygon.v1 - point;

            vec1.Normalize();
            vec2.Normalize();

            angle3 = Math.Acos(Vector3.Dot(vec1, vec2)) * Vector3.Dot(polygon.normal, Vector3.Cross(vec1, vec2));
            currentAngle += angle3;

            if (Math.Abs(currentAngle) > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region CollideWidth Variables

        private Vector3 newPosition;

        private Vector3 collisionPoint;
        private Vector3 newVelocityVector;
        private float closestT;
        private Vector3 closestVelocityVector;
        private Vector3 closestCollisionPoint;
        private float distToPlane;

        private const float minVelocityVectorLen = 2.0f;
        private const float acceptableFloatError = 0.01f;

        #endregion

        /// <summary>
        /// Calculate result of collisions with collision mesh
        /// </summary>
        /// <param name="originalPosition">Starting point</param>
        /// <param name="velocityVector">Change of position this frame</param>
        /// <param name="radius">Radius of bounding sphere</param>
        /// <param name="remainingRecursions">Maximum number of collisions. Prevents stack overflow.</param>
        /// <returns>Position after collisions</returns>
        public Vector3 CollideWith(Vector3 originalPosition, Vector3 velocityVector, double radius, int remainingRecursions)
        {
            if (remainingRecursions == 0 || velocityVector == Vector3.Zero)
            {
                return originalPosition;
            }

            bool firstTimeThrough = true;
            this.closestT = -1;
            this.newPosition = originalPosition + velocityVector;
            this.newVelocityVector = velocityVector;
            this.newVelocityVector.Normalize();

            int i;

            //
            // Check collision with each polygon in collision mesh
            //
            for (i = 0; i < this.collisionMesh.Count; i++)
            {
                this.distToPlane = Vector3.Dot(originalPosition - this.collisionMesh[i].v1, -this.collisionMesh[i].normal);

                // Parametric value for when the current polygon will be hit (0 to 1 means it will be hit this frame)
                float tValue = ((float)radius + Vector3.Dot(-this.collisionMesh[i].normal, this.collisionMesh[i].v1 - originalPosition)) / Vector3.Dot(velocityVector, -this.collisionMesh[i].normal);

                if (tValue < 0 || tValue > 1) // If polygon is not hit this frame
                {
                    if (this.distToPlane > 0 && this.distToPlane < radius && pointInsidePolygon(originalPosition + (velocityVector * tValue), this.collisionMesh[i]))
                    {
                        tValue = 0; // Sphere is embedded, so don't proceed
                    }
                    else
                    {
                        continue;
                    }
                }

                this.collisionPoint = originalPosition + (velocityVector * tValue);

                float scaleFactor = Vector3.Dot(this.newPosition - this.collisionPoint, this.collisionMesh[i].normal);

                Vector3 tempPoint = collisionPoint + (this.collisionMesh[i].normal * scaleFactor);

                if (this.newPosition == tempPoint)
                {
                    this.newVelocityVector = Vector3.Zero;
                }
                else
                {
                    this.newVelocityVector = this.newPosition - tempPoint;
                    this.newVelocityVector.Normalize();
                    this.newVelocityVector *= (tValue * velocityVector.Length());
                }

                if (firstTimeThrough || tValue < this.closestT)
                {
                    if (pointInsidePolygon(this.collisionPoint, this.collisionMesh[i]))
                    {
                        // If the collision point is inside the current polygon and it has the smallest tValue so far,
                        // record the collision info.
                        this.closestT = tValue;
                        this.closestVelocityVector = this.newVelocityVector;
                        this.closestCollisionPoint = this.collisionPoint + (-this.collisionMesh[i].normal * 0.1f);

                        firstTimeThrough = false;
                    }
                }
            }

            if (firstTimeThrough)
            {
                return this.newPosition; // No collisions, just apply velocity vector
            }
            else
            {
                return CollideWith(this.closestCollisionPoint, this.closestVelocityVector, radius, remainingRecursions - 1); // Recursively collide
            }
        }

        public bool EmitterCollideWith(Vector3 originalPosition, Vector3 velocityVector, double radius, out Vector3 collisionPoint)
        {
            bool collide = this.EmitterCollideWithGeometry(originalPosition, velocityVector, radius, out collisionPoint);

            return collide;
        }

        /// <summary>
        /// Determine whether or not a particle emitter collided with the geometry.
        /// </summary>
        /// <param name="originalPosition">The original position of the particle emitter</param>
        /// <param name="velocityVector">The velocity of the particle emitter</param>
        /// <param name="radius">Effective radius of the emitter</param>
        /// <returns>This returns either true or false in one pass with no recursion.</returns>
        public bool EmitterCollideWithGeometry(Vector3 originalPosition, Vector3 velocityVector, double radius, out Vector3 outCollisionPoint)
        {

            bool firstTimeThrough = true;
            this.closestT = -1;
            this.newPosition = originalPosition + velocityVector;
            this.newVelocityVector = velocityVector;
            this.newVelocityVector.Normalize();

            int i;

            for (i = 0; i < this.collisionMesh.Count; i++)
            {
                this.distToPlane = Vector3.Dot(originalPosition - this.collisionMesh[i].v1, -this.collisionMesh[i].normal);

                float tValue = ((float)radius + Vector3.Dot(-this.collisionMesh[i].normal, this.collisionMesh[i].v1 - originalPosition)) / Vector3.Dot(velocityVector, -this.collisionMesh[i].normal);

                if (tValue < 0 || tValue > 1)
                {
                    if (this.distToPlane > 0 && this.distToPlane < radius && pointInsidePolygon(originalPosition + (velocityVector * tValue), this.collisionMesh[i]))
                    {
                        tValue = 0; // Sphere is embedded, so don't proceed
                    }
                    else
                    {
                        continue;
                    }
                }

                this.collisionPoint = originalPosition + (velocityVector * tValue);

                float scaleFactor = Vector3.Dot(this.newPosition - this.collisionPoint, this.collisionMesh[i].normal);

                Vector3 tempPoint = collisionPoint + (this.collisionMesh[i].normal * scaleFactor);

                if (this.newPosition == tempPoint)
                {
                    this.newVelocityVector = Vector3.Zero;
                }
                else
                {
                    this.newVelocityVector = this.newPosition - tempPoint;
                    this.newVelocityVector.Normalize();
                    this.newVelocityVector *= (tValue * velocityVector.Length());
                }

                if (firstTimeThrough || tValue < this.closestT)
                {
                    if (pointInsidePolygon(this.collisionPoint /*+ ((float)radius * this.collisionMesh[i].normal)*/, this.collisionMesh[i]))
                    {
                        this.closestT = tValue;
                        this.closestVelocityVector = this.newVelocityVector;
                        this.closestCollisionPoint = this.collisionPoint + (-this.collisionMesh[i].normal * 0.1f);

                        firstTimeThrough = false;
                    }
                }
            }

            if (firstTimeThrough)
            {
                outCollisionPoint = this.newPosition;
                return false; // No collisions, just return false.
            }
            else
            {
                outCollisionPoint = this.closestCollisionPoint;
                return true;
            }
        }

        #endregion

        #region Pathfinding

        public Path<NavMeshNode> FindPath(int startIndex, int destinationIndex)
        {
            NavMeshNode start = navigationMesh[startIndex];
            NavMeshNode destination = navigationMesh[destinationIndex];

            var closed = new HashSet<NavMeshNode>();
            var queue = new PriorityQueue<double, Path<NavMeshNode>>();
            queue.Enqueue(0, new Path<NavMeshNode>(start));
            while (!queue.IsEmpty)
            {
                var path = queue.Dequeue();
                if (closed.Contains(path.LastStep))
                    continue;
                if (path.LastStep.Equals(destination))
                    return path;
                closed.Add(path.LastStep);
                for ( int i = 0; i < path.LastStep.adjacent_polygons.Count; i++ )
                {
                    // There is an obstacle in Nav_Mesh.NavMesh[n]... avoid it.
                    if (navigationMesh[path.LastStep.adjacent_polygons[i]].Obstacle == true) continue;

                    double d = (double)Vector3.Distance(path.LastStep.Centroid, navigationMesh[path.LastStep.adjacent_polygons[i]].Centroid);
                    double e = (double)Vector3.Distance(navigationMesh[path.LastStep.adjacent_polygons[i]].Centroid, destination.Centroid);

                    var newPath = path.AddStep(navigationMesh[path.LastStep.adjacent_polygons[i]], d);
                    queue.Enqueue(newPath.TotalCost + e, newPath);
                }
            }
            return null;
        }

        public int NavigationIndex(Vector3 position)
        {
            return this.NavigationIndex(position, -1);
        }

        public int NavigationIndex(Vector3 position, int currentLocation)
        {
            if (currentLocation < 0)
            {
                for (int i = 0; i < navigationMesh.Count; i++)
                {
                    if (RayTriangleIntersect(new Ray(position, Vector3.Down), i))
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < navigationMesh[currentLocation].adjacent_polygons.Count; i++)
                {
                    if (RayTriangleIntersect(new Ray(position, Vector3.Down), i))
                    {
                        return navigationMesh[currentLocation].adjacent_polygons[i];
                    }
                }
            }

            return -1;
        }

        public bool RayTriangleIntersect(Ray R, int nodeIndex)
        {
            //Vector3 u, v, n;             // triangle vectors
            //Vector3 dir, w0, w;          // ray vectors

            if (nodeIndex < 0)
            {
                return false;
            }

            NavMeshNode node = this.navigationMesh[nodeIndex];

            Vector3 intersection = Vector3.Zero;
            float r, a, b;             // params to calc ray-plane intersect

            // get triangle edge vectors and plane normal
            Vector3 u = node.V1 - node.V0;
            Vector3 v = node.V2 - node.V0;
            Vector3 n = Vector3.Cross(u, v);             // cross product
            if (n == Vector3.Zero)            // triangle is degenerate
                return false;                 // do not deal with this case

            Vector3 w0 = R.Position - node.V0;
            a = -Vector3.Dot(n, w0);
            b = Vector3.Dot(n, R.Direction);
            if (Math.Abs(b) < 0.00001f)
            {     // ray is parallel to triangle plane
                if (a == 0) { }//return true;                // ray lies in triangle plane
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

        public Vector3 TravelPoint(int firstPolyIndex, int secondPolyIndex)
        {
            return (navigationMesh[firstPolyIndex].Centroid + navigationMesh[secondPolyIndex].Centroid) * 0.5f;
        }

        #endregion

        #region Draw

        public void Draw(GraphicsDevice device, ref GameCamera camera, bool drawCollisionMesh,
                         bool drawNavigationMesh, ref List<Light> lights, Dimension currentDimension,
                         Vector3 playerPosition)
        {
            int currentLocationIndex = 0;

            CullMode previousCullMode = device.RenderState.CullMode;
            device.RenderState.CullMode = CullMode.CullClockwiseFace;

            cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_OneLight"];
            cel_effect.Parameters["matW"].SetValue(Matrix.Identity);
            cel_effect.Parameters["matVP"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
            cel_effect.Parameters["matVI"].SetValue(Matrix.Invert(camera.GetViewMatrix()));
            //cel_effect.Parameters["shadowMap"].SetValue(shadowRenderTarget.GetTexture());
            cel_effect.Parameters["diffuseMap0"].SetValue(terrainTexture);
            cel_effect.Parameters["CelMap"].SetValue(m_celMap);
            cel_effect.Parameters["ambientLightColor"].SetValue(new Vector3(0.0f));
            cel_effect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3(1.0f));
            cel_effect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.1f));
            cel_effect.Parameters["material"].StructureMembers["specularPower"].SetValue(20);
            //cel_effect.Parameters["diffuseMapEnabled"].SetValue(true);
            cel_effect.Parameters["playerPosition"].SetValue(playerPosition);
            cel_effect.Parameters["transitionRadius"].SetValue(GameplayScreen.transitionRadius);
            cel_effect.Parameters["waveRadius"].SetValue(GameplayScreen.transitionRadius - GameplayScreen.WAVE_FRONT_SIZE);

            for (int i = 0; i < lights.Count; i++)
            {
                if ((i + 1) > GameplayScreen.MAX_LIGHTS)
                {
                    break;
                }
                cel_effect.Parameters["lights"].Elements[i].StructureMembers["color"].SetValue(lights[i].color * (1 - lights[i].currentExplosionTick));
                cel_effect.Parameters["lights"].Elements[i].StructureMembers["position"].SetValue(lights[i].position);
                cel_effect.Parameters["lightRadii"].Elements[i].SetValue(lights.ElementAt(i).attenuationRadius);
            }

            string techniqueModifier = "";

            if (currentDimension == Dimension.FIRST)
            {
                techniqueModifier = "";
            }
            else
            {
                techniqueModifier = "_Gray";
            }

            if (GameplayScreen.transitioning)
            {
                cel_effect.Parameters["transitioning"].SetValue(1);
            }
            else
            {
                cel_effect.Parameters["transitioning"].SetValue(0);
            }

            switch (lights.Count)
            {
                case 0:
                    break;
                case 1: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_OneLight" + techniqueModifier];
                    break;
                case 2: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_TwoLight" + techniqueModifier];
                    break;
                case 3: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_ThreeLight" + techniqueModifier];
                    break;
                case 4: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_FourLight" + techniqueModifier];
                    break;
                case 5: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_FiveLight" + techniqueModifier];
                    break;
                case 6: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_SixLight" + techniqueModifier];
                    break;
                case 7: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_SevenLight" + techniqueModifier];
                    break;
                case 8: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_EightLight" + techniqueModifier];
                    break;
                default: cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_EightLight" + techniqueModifier];
                    break;
            }

            this.cel_effect.Begin();
            foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                levelPieces[currentLocationIndex].Draw(device, ref camera);

                pass.End();
            }

            for (int i = 0; i < adjacencyList[currentLocationIndex].Count; i++)
            {
                foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    levelPieces[adjacencyList[currentLocationIndex][i].index].Draw(device, ref camera);

                    pass.End();
                }
            }
            this.cel_effect.End();

            if (drawCollisionMesh || drawNavigationMesh)
            {
                device.RenderState.CullMode = CullMode.None;
                cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel_Wireframe"];
                this.cel_effect.Begin();
                FillMode oldFillMode = device.RenderState.FillMode;
                device.RenderState.FillMode = FillMode.WireFrame;

                foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    if (drawCollisionMesh)
                    {
                        device.VertexDeclaration = this.collisionVertexDeclaration;
                        device.Vertices[0].SetSource(this.collisionVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.collisionVertexCount / 3);
                    }

                    if (drawNavigationMesh)
                    {
                        device.VertexDeclaration = this.navigationVertexDeclaration;
                        device.Vertices[0].SetSource(this.navigationVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                        device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.navigationVertexCount / 3);
                    }

                    pass.End();
                }
                this.cel_effect.End();
                device.RenderState.FillMode = oldFillMode;
            }

            device.RenderState.CullMode = previousCullMode;
        }

        #endregion

    }
}
