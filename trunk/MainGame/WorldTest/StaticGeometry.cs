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

    class StaticGeometry
    {
        public const int MAX_RECURSIONS = 10;
        private VertexBuffer vertexBuffer;
        private VertexDeclaration vertexDeclaration;
        private int vertexCount;
        private List<CollisionPolygon> collisionMesh;

        public StaticGeometry(GraphicsDevice device, VertexPositionNormalTexture[] vertices, string collisionMeshFilename, Vector3 collisionMeshOffset)
        {
            this.vertexBuffer = new VertexBuffer(device, vertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.vertexBuffer.SetData(vertices);

            this.vertexCount = vertices.Length;

            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);

            if (collisionMeshFilename != null && collisionMeshFilename != "")
            {
                this.LoadCollisionMesh(collisionMeshFilename, collisionMeshOffset);
            }
        }

        public void Draw(GraphicsDevice device)
        {
            device.VertexDeclaration = this.vertexDeclaration;
            device.Vertices[0].SetSource(this.vertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.vertexCount / 3);
        }

        private void LoadCollisionMesh(string filename, Vector3 collisionMeshOffset)
        {
            ArrayList positionList = new ArrayList();

            this.collisionMesh = new List<CollisionPolygon>();

            if (filename == "")
            {
                return;
            }

            positionList.Add(new Vector3());

            CollisionPolygon currentPolygon;

            FileStream meshFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader meshFileReader = new StreamReader(meshFile);

            string line = meshFileReader.ReadLine();
            string[] splitLine;
            string[] splitVertex;
            char[] splitChars = { ' ' };

            Vector3 polygonVector1;
            Vector3 polygonVector2;

            while (line != null)
            {
                if (line == "" || line == "\n")
                {
                    line = meshFileReader.ReadLine();
                    continue;
                }

                splitLine = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                if (splitLine[0] == "v") // Position
                {
                    positionList.Add(new Vector3((float)Convert.ToDouble(splitLine[1]), (float)Convert.ToDouble(splitLine[2]), (float)Convert.ToDouble(splitLine[3])));
                }
                else if (splitLine[0] == "f") // Face (each vertex is Position/Texture/Normal)
                {
                    splitVertex = splitLine[1].Split('/');
                    currentPolygon.v1 = (Vector3)positionList[Convert.ToInt32(splitVertex[0])];
                    splitVertex = splitLine[2].Split('/');
                    currentPolygon.v2 = (Vector3)positionList[Convert.ToInt32(splitVertex[0])];
                    splitVertex = splitLine[3].Split('/');
                    currentPolygon.v3 = (Vector3)positionList[Convert.ToInt32(splitVertex[0])];

                    polygonVector1 = currentPolygon.v1 - currentPolygon.v2;
                    polygonVector2 = currentPolygon.v3 - currentPolygon.v2;

                    Vector3.Cross(ref polygonVector1, ref polygonVector2, out currentPolygon.normal);
                    currentPolygon.normal.Normalize();

                    currentPolygon.v1 += collisionMeshOffset;
                    currentPolygon.v2 += collisionMeshOffset;
                    currentPolygon.v3 += collisionMeshOffset;

                    collisionMesh.Add(currentPolygon);
                }
                else // Unused line format, skipping
                {

                }

                line = meshFileReader.ReadLine();
            }
        }

        private void ScaleVector(ref Vector3 v, float scalar)
        {
            v.X *= scalar;
            v.Y *= scalar;
            v.Z *= scalar;
        }

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

        private Vector3 newPosition;

        private Vector3 collisionPoint;
        private Vector3 newVelocityVector;
        private float closestT;
        private Vector3 closestVelocityVector;
        private Vector3 closestCollisionPoint;

        private const float minVelocityVectorLen = 2.0f;
        private const float acceptableFloatError = 0.01f;

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
    }
}
