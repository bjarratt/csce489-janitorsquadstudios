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
    class Monolith : StaticGeometry
    {
        private Effect barrierEffect;
        private Texture2D bumpMap;
        private Texture2D barrierMap;

        private VertexBuffer barrierVertexBuffer;
        private VertexDeclaration barrierVertexDeclaration;
        private int vertexCount2;

        private VertexBuffer monolithVertexBuffer;
        private VertexDeclaration monolithVertexDeclaration;

        public Vector3 monPosition1;
        public Vector3 monPosition2;

        public Matrix worldMatrix1 = Matrix.Identity;
        public Matrix worldMatrix2 = Matrix.Identity;

        private Dimension dimension;

        private float time;

        public Dimension CurrentDimension
        {
            get { return dimension; }
        }

        private Vector3 v1;
        private Vector3 v2;
        private Vector3 v3;

        private Vector3 q1;
        private Vector3 q2;
        private Vector3 q3;

        private CollisionPolygon v;
        private CollisionPolygon q;
        private CollisionPolygon w;
        private CollisionPolygon z;

        public int index_v;
        public int index_q;
        public int index_w;
        public int index_z;


        public Monolith(string meshFile, Vector3 position1, Vector3 position2, Dimension dim) : base(meshFile, null, null)
        {
            this.monPosition1 = position1;
            this.monPosition2 = position2;

            worldMatrix1.Translation = position1;
            worldMatrix2.Translation = position2;

            this.dimension = dim;
        }

        public void Load(GraphicsDevice device, ref ContentManager content, ref Player player, Level currentLevel)
        {
            VertexPositionNormalTexture[] terrainVertices1 = (VertexPositionNormalTexture[])this.LoadFromOBJ(visibleMeshFilename, worldMatrix1, false).ToArray(typeof(VertexPositionNormalTexture));
            VertexPositionNormalTexture[] terrainVertices2 = (VertexPositionNormalTexture[])this.LoadFromOBJ(visibleMeshFilename, worldMatrix2, false).ToArray(typeof(VertexPositionNormalTexture));
            this.terrainVertexBuffer = new VertexBuffer(device, terrainVertices1.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.terrainVertexBuffer.SetData(terrainVertices1);
            this.monolithVertexBuffer = new VertexBuffer(device, terrainVertices2.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.WriteOnly);
            this.monolithVertexBuffer.SetData(terrainVertices2);

            this.vertexCount = terrainVertices1.Length;
            this.vertexCount2 = terrainVertices2.Length;

            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
            this.monolithVertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);
            SetUpBarrierVertices(ref device, ref player, currentLevel);

            barrierEffect = content.Load<Effect>("Water");
            bumpMap = content.Load<Texture2D>("waterbump");
            barrierMap = content.Load<Texture2D>("barrier");
        }

        private void SetUpBarrierVertices(ref GraphicsDevice device, ref Player player, Level currentLevel)
        {
            VertexPositionTexture[] barrierVertices = new VertexPositionTexture[6];
            
            barrierVertices[0] = new VertexPositionTexture(new Vector3(monPosition1.X, 50, monPosition1.Z), new Vector2(1, 1));
            barrierVertices[2] = new VertexPositionTexture(new Vector3(monPosition1.X, 300, monPosition1.Z), new Vector2(0, 1));
            barrierVertices[1] = new VertexPositionTexture(new Vector3(monPosition2.X, 50, monPosition2.Z), new Vector2(1, 0));

            this.v1 = new Vector3(monPosition1.X, -1, monPosition1.Z);
            this.v2 = new Vector3(monPosition1.X, 300, monPosition1.Z);
            this.v3 = new Vector3(monPosition2.X, -1, monPosition2.Z);

            barrierVertices[3] = new VertexPositionTexture(new Vector3(monPosition1.X, 300, monPosition1.Z), new Vector2(0, 1));
            barrierVertices[5] = new VertexPositionTexture(new Vector3(monPosition2.X, 300, monPosition2.Z), new Vector2(1, 1));
            barrierVertices[4] = new VertexPositionTexture(new Vector3(monPosition2.X, 50, monPosition2.Z), new Vector2(1, 0));

            this.q1 = new Vector3(monPosition1.X, 300, monPosition1.Z);
            this.q2 = new Vector3(monPosition2.X, 300, monPosition2.Z);
            this.q3 = new Vector3(monPosition2.X, -1, monPosition2.Z);

            barrierVertexBuffer = new VertexBuffer(device, barrierVertices.Length * VertexPositionTexture.SizeInBytes, BufferUsage.WriteOnly);
            barrierVertexBuffer.SetData(barrierVertices);
            barrierVertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            v = new CollisionPolygon();
            q = new CollisionPolygon();
            w = new CollisionPolygon();
            z = new CollisionPolygon();
            v.v1 = this.v1;
            v.v2 = this.v2;
            v.v3 = this.v3;
            q.v1 = this.q1;
            q.v2 = this.q2;
            q.v3 = this.q3;
            w.v1 = this.v1;
            w.v2 = this.v2;
            w.v3 = this.v3;
            z.v1 = this.q1;
            z.v2 = this.q2;
            z.v3 = this.q3;

            v.normal = -Vector3.Cross(v2 - v1, v3 - v1);
            v.normal.Normalize();
            q.normal = v.normal;
            w.normal = -v.normal;
            z.normal = -v.normal;

            currentLevel.BarrierMeshes.Add(v);
            currentLevel.barrierOn.Add(true);
            index_v = currentLevel.barrierOn.Count - 1;
            currentLevel.BarrierMeshes.Add(q);
            currentLevel.barrierOn.Add(true);
            index_q = currentLevel.barrierOn.Count - 1;
            currentLevel.BarrierMeshes.Add(w);
            currentLevel.barrierOn.Add(true);
            index_w = currentLevel.barrierOn.Count - 1;
            currentLevel.BarrierMeshes.Add(z);
            currentLevel.barrierOn.Add(true);
            index_z = currentLevel.barrierOn.Count - 1;
        }

        public void Update(GameTime gameTime, ref Player player, ref Level currentLevel)
        {
            time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
            if (this.dimension == player.CurrentDimension)
            {
                currentLevel.barrierOn[index_v] = true;
                currentLevel.barrierOn[index_q] = true;
                currentLevel.barrierOn[index_w] = true;
                currentLevel.barrierOn[index_z] = true;
            }
            else
            {
                currentLevel.barrierOn[index_v] = false;
                currentLevel.barrierOn[index_q] = false;
                currentLevel.barrierOn[index_w] = false;
                currentLevel.barrierOn[index_z] = false;
            }
        }

        public void DrawBarrier(GraphicsDevice device, ref GameCamera camera, ref List<Light> lights)
        {
            barrierEffect.CurrentTechnique = barrierEffect.Techniques["Barrier"];
            barrierEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            barrierEffect.Parameters["xView"].SetValue(camera.GetViewMatrix());
            barrierEffect.Parameters["xProjection"].SetValue(camera.GetProjectionMatrix());
            barrierEffect.Parameters["xWaterBumpMap"].SetValue(bumpMap);
            barrierEffect.Parameters["xBarrierMap"].SetValue(barrierMap);
            barrierEffect.Parameters["xWaveLength"].SetValue(0.5f);
            barrierEffect.Parameters["xWaveHeight"].SetValue(0.4f);
            barrierEffect.Parameters["xCamPos"].SetValue(camera.position);
            barrierEffect.Parameters["xTime"].SetValue(this.time);
            barrierEffect.Parameters["xWindForce"].SetValue(0.001f);
            barrierEffect.Parameters["xWindDirection"].SetValue(new Vector3(0.3f, 0.3f, 0f));
            for (int i = 0; i < lights.Count; i++)
            {
                if ((i + 1) > GameplayScreen.MAX_LIGHTS)
                {
                    break;
                }
                barrierEffect.Parameters["lights"].Elements[i].StructureMembers["color"].SetValue(lights[i].color);
                barrierEffect.Parameters["lights"].Elements[i].StructureMembers["position"].SetValue(lights[i].position);
            }
            barrierEffect.Parameters["lightCount"].SetValue(lights.Count);

            barrierEffect.Begin();
            foreach (EffectPass pass in barrierEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.VertexDeclaration = this.barrierVertexDeclaration;
                device.Vertices[0].SetSource(this.barrierVertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                int noVertices = barrierVertexBuffer.SizeInBytes / VertexPositionTexture.SizeInBytes;
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, noVertices / 3);

                pass.End();
            }
            barrierEffect.End();
        }

        public void Draw(GraphicsDevice device, ref GameCamera camera, Effect effect)
        {
            effect.Parameters["matW"].SetValue(worldMatrix1);
            device.VertexDeclaration = this.vertexDeclaration;
            device.Vertices[0].SetSource(this.terrainVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.vertexCount / 3);

            effect.Parameters["matW"].SetValue(worldMatrix2);
            device.VertexDeclaration = this.monolithVertexDeclaration;
            device.Vertices[0].SetSource(this.monolithVertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, this.vertexCount / 3);

            effect.Parameters["matW"].SetValue(Matrix.Identity);
        }

    }
}
