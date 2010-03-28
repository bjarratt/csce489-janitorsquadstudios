﻿using System;
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

        public const int MAX_RECURSIONS = 5;

        private List<Light> lights;

        private string visibleMeshFilename;
        private VertexBuffer terrainVertexBuffer;
        private VertexDeclaration vertexDeclaration;
        private int vertexCount;

        private string collisionMeshFilename;
        private List<CollisionPolygon> collisionMesh;
        private Vector3 collisionMeshOffset;
        private VertexBuffer collisionVertexBuffer;
        private VertexDeclaration collisionVertexDeclaration;
        private int collisionVertexCount;

        private Effect cel_effect;
        private Texture2D m_celMap;
        private Texture2D terrainTexture;

        #endregion

        #region Constructor

        /// <summary>
        /// StaticGeometry constructor
        /// </summary>
        /// <param name="device">GraphicsDevice to draw to</param>
        /// <param name="visibleMeshFilename">OBJ file to read visible mesh from</param>
        /// <param name="collisionMeshFilename">OBJ file to read collision mesh from</param>
        /// <param name="collisionMeshOffset">Offset applied to all collision mesh vertices (for alignment)</param>
        public StaticGeometry(string visibleMeshFilename, string collisionMeshFilename, Vector3 collisionMeshOffset, ref List<Light> lights)
        {
            this.lights = lights;
            this.visibleMeshFilename = visibleMeshFilename;
            this.collisionMeshFilename = collisionMeshFilename;
            this.collisionMeshOffset = collisionMeshOffset;
        }

        #endregion

        #region Load

        public void Load(GraphicsDevice device, ref ContentManager content, Matrix worldMatrix)
        {
            // 
            // Initialize terrain vertex buffer
            //

            VertexPositionNormalTexture[] terrainVertices = (VertexPositionNormalTexture[])this.LoadFromOBJ(visibleMeshFilename, worldMatrix, false).ToArray(typeof(VertexPositionNormalTexture));
            this.terrainVertexBuffer = new VertexBuffer(device, terrainVertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.terrainVertexBuffer.SetData(terrainVertices);

            this.vertexCount = terrainVertices.Length;

            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);


            //
            // Initialize collision vertex buffer and collision mesh
            //

            VertexPositionNormalTexture[] collisionVertices = (VertexPositionNormalTexture[])this.LoadFromOBJ(collisionMeshFilename, worldMatrix, true).ToArray(typeof(VertexPositionNormalTexture));
            this.collisionVertexBuffer = new VertexBuffer(device, collisionVertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.collisionVertexBuffer.SetData(collisionVertices);

            this.collisionVertexCount = collisionVertices.Length;

            this.collisionVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);

            cel_effect = content.Load<Effect>("CelShade");
            m_celMap = content.Load<Texture2D>("Toon");
            terrainTexture = content.Load<Texture2D>("tex");
        }

        private ArrayList LoadFromOBJ(string filename, Matrix worldMatrix, bool isCollisionMesh)
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

            //float largestYValue = -10000.0f;
            //float smallestYValue = 10000.0f;
            //float largestXValue = -10000.0f;
            //float smallestXValue = 10000.0f;
            //float largestZValue = -10000.0f;
            //float smallestZValue = 10000.0f;

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
                            if (isCollisionMesh)
                            {
                                currentVertex.Position += this.collisionMeshOffset;
                            }
                            /*
                            if (currentVertex.Position.Y > largestYValue)
                            {
                                largestYValue = currentVertex.Position.Y;
                            }
                            else if (currentVertex.Position.Y < smallestYValue)
                            {
                                smallestYValue = currentVertex.Position.Y;
                            }
                            if (currentVertex.Position.X > largestXValue)
                            {
                                largestXValue = currentVertex.Position.X;
                            }
                            else if (currentVertex.Position.X < smallestXValue)
                            {
                                smallestXValue = currentVertex.Position.X;
                            }
                            if (currentVertex.Position.Z > largestZValue)
                            {
                                largestZValue = currentVertex.Position.Z;
                            }
                            else if (currentVertex.Position.Z < smallestZValue)
                            {
                                smallestZValue = currentVertex.Position.Z;
                            }*/
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
                                currentPolygon.v1 = currentVertex.Position;// +this.collisionMeshOffset;
                            }
                            else if (i == 2)
                            {
                                currentPolygon.v2 = currentVertex.Position;// +this.collisionMeshOffset;
                            }
                            else if (i == 3)
                            {
                                currentPolygon.v3 = currentVertex.Position;// +this.collisionMeshOffset;

                                polygonVector1 = currentPolygon.v1 - currentPolygon.v2;
                                polygonVector2 = currentPolygon.v3 - currentPolygon.v2;

                                Vector3.Cross(ref polygonVector1, ref polygonVector2, out currentPolygon.normal);
                                currentPolygon.normal.Normalize();

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
                        this.closestCollisionPoint = this.collisionPoint +(-this.collisionMesh[i].normal * 0.1f);

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

        /// <summary>
        /// Determine whether or not a particle emitter collided with the geometry.
        /// </summary>
        /// <param name="originalPosition">The original position of the particle emitter</param>
        /// <param name="velocityVector">The velocity of the particle emitter</param>
        /// <param name="radius">Effective radius of the emitter</param>
        /// <returns>This returns either true or false in one pass with no recursion.</returns>
        public bool EmitterCollideWithGeometry(Vector3 originalPosition, Vector3 velocityVector, double radius)
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
                return false; // No collisions, just return false.
            }
            else
            {
                return true;
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draws the terrain stored in the StaticGeometry vertex buffer
        /// </summary>
        /// <param name="device">The GraphicsDevice to draw to</param>
        /// <param name="drawCollisionMesh">Set to true to draw a wireframe of the collision mesh</param>
        public void Draw(GraphicsDevice device, bool drawCollisionMesh, ref GameCamera camera)
        {
            CullMode previousCullMode = device.RenderState.CullMode;
            device.RenderState.CullMode = CullMode.CullClockwiseFace;

            cel_effect.CurrentTechnique = cel_effect.CurrentTechnique = cel_effect.Techniques["StaticModel"];
            cel_effect.Parameters["matW"].SetValue(Matrix.CreateScale(1.0f));
            cel_effect.Parameters["matVP"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
            cel_effect.Parameters["matVI"].SetValue(Matrix.Invert(camera.GetViewMatrix()));
            //cel_effect.Parameters["shadowMap"].SetValue(shadowRenderTarget.GetTexture());
            cel_effect.Parameters["diffuseMap0"].SetValue(terrainTexture);
            cel_effect.Parameters["CelMap"].SetValue(m_celMap);
            cel_effect.Parameters["ambientLightColor"].SetValue(new Vector3(0.1f));
            cel_effect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3(1.0f));
            cel_effect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.1f));
            cel_effect.Parameters["material"].StructureMembers["specularPower"].SetValue(20);
            cel_effect.Parameters["diffuseMapEnabled"].SetValue(true);
            cel_effect.Parameters["lights"].Elements[0].StructureMembers["color"].SetValue(lights[0].color);
            cel_effect.Parameters["lights"].Elements[0].StructureMembers["position"].SetValue(lights[0].position);

            this.cel_effect.Begin();
            foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.VertexDeclaration = this.vertexDeclaration;
                device.Vertices[0].SetSource(this.terrainVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.vertexCount / 3);

                if (drawCollisionMesh)
                {
                    FillMode oldFillMode = device.RenderState.FillMode;
                    device.RenderState.FillMode = FillMode.WireFrame;
                    device.VertexDeclaration = this.collisionVertexDeclaration;
                    device.Vertices[0].SetSource(this.collisionVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.collisionVertexCount / 3);
                    device.RenderState.FillMode = oldFillMode;
                }

                pass.End();
            }
            this.cel_effect.End();
            device.RenderState.CullMode = previousCullMode;
        }

        #endregion
    }
}
