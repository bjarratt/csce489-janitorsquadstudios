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
    struct CollisionPolygon
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;
        public Vector3 normal;
    }

    /// <summary>
    /// Used to store terrain and its corresponding collision mesh
    /// </summary>
    class StaticGeometry
    {
        #region Properties

        public const int MAX_RECURSIONS = 10;
        private VertexBuffer terrainVertexBuffer;
        private VertexDeclaration vertexDeclaration;
        private int vertexCount;

        private List<CollisionPolygon> collisionMesh;
        private Vector3 collisionMeshOffset;
        private VertexBuffer collisionVertexBuffer;
        private VertexDeclaration collisionVertexDeclaration;
        private int collisionVertexCount;

        #endregion

        #region Constructor

        /// <summary>
        /// StaticGeometry constructor
        /// </summary>
        /// <param name="device">GraphicsDevice to draw to</param>
        /// <param name="visibleMeshFilename">OBJ file to read visible mesh from</param>
        /// <param name="collisionMeshFilename">OBJ file to read collision mesh from</param>
        /// <param name="collisionMeshOffset">Offset applied to all collision mesh vertices (for alignment)</param>
        public StaticGeometry(GraphicsDevice device, string visibleMeshFilename, string collisionMeshFilename, Vector3 collisionMeshOffset)
        {
            // 
            // Initialize terrain vertex buffer
            //

            VertexPositionNormalTexture[] terrainVertices = (VertexPositionNormalTexture[])this.LoadFromOBJ(visibleMeshFilename, false).ToArray(typeof(VertexPositionNormalTexture));
            this.terrainVertexBuffer = new VertexBuffer(device, terrainVertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.terrainVertexBuffer.SetData(terrainVertices);

            this.vertexCount = terrainVertices.Length;

            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);


            //
            // Initialize collision vertex buffer and collision mesh
            //

            VertexPositionNormalTexture[] collisionVertices = (VertexPositionNormalTexture[])this.LoadFromOBJ(collisionMeshFilename, true).ToArray(typeof(VertexPositionNormalTexture));
            this.collisionVertexBuffer = new VertexBuffer(device, collisionVertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.collisionVertexBuffer.SetData(collisionVertices);

            this.collisionVertexCount = collisionVertices.Length;

            this.collisionVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);

            this.collisionMeshOffset = collisionMeshOffset;

        }

        #endregion

        #region Load

        private ArrayList LoadFromOBJ(string filename, bool isCollisionMesh)
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

            // Variables used for collision meshes
            CollisionPolygon currentPolygon;
            currentPolygon.v1 = Vector3.Zero;
            currentPolygon.v2 = Vector3.Zero;
            currentPolygon.v3 = Vector3.Zero;
            currentPolygon.normal = Vector3.Zero;
            Vector3 polygonVector1;
            Vector3 polygonVector2;
            if (isCollisionMesh)
            {
                this.collisionMesh = new List<CollisionPolygon>();
            }

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
                    positionList.Add(new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3])));
                }
                else if (splitLine[0] == "vn") // Normal
                {
                    normalList.Add(new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3])));
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

                        if (isCollisionMesh)
                        {
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

                                currentPolygon.v1 += this.collisionMeshOffset;
                                currentPolygon.v2 += this.collisionMeshOffset;
                                currentPolygon.v3 += this.collisionMeshOffset;

                                collisionMesh.Add(currentPolygon);
                            }
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
            this.newPosition = originalPosition + velocityVector;
            this.newVelocityVector = velocityVector;
            this.newVelocityVector.Normalize();

            for (int i = 0; i < this.collisionMesh.Count; i++)
            {
                float tValue = ((float)radius + Vector3.Dot(-this.collisionMesh[i].normal, this.collisionMesh[i].v1 - originalPosition)) / Vector3.Dot(velocityVector, -this.collisionMesh[i].normal);

                if (tValue < 0 || tValue > 1)
                {
                    continue;
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
                return this.newPosition; // No collisions, just apply velocity vector
            }
            else
            {
                return CollideWith(this.closestCollisionPoint, this.closestVelocityVector, radius, remainingRecursions - 1);
            }
        }

        #endregion

        #region Draw

        public void Draw(GraphicsDevice device, bool drawCollisionMesh)
        {
            device.VertexDeclaration = this.vertexDeclaration;
            device.Vertices[0].SetSource(this.terrainVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.vertexCount / 3);

            if (drawCollisionMesh)
            {
                FillMode oldMode = device.RenderState.FillMode;
                device.RenderState.FillMode = FillMode.WireFrame;
                device.VertexDeclaration = this.collisionVertexDeclaration;
                device.Vertices[0].SetSource(this.collisionVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.collisionVertexCount / 3);
                device.RenderState.FillMode = oldMode;
            }
        }

        #endregion
    }
}
