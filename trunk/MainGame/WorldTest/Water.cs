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
    class Water
    {
        public GraphicsDevice device;
        public SpriteBatch spriteBatch;
        private Model waterModel;
        private Effect waterEffect;
        private Texture3D noise;
        private TextureCube environmentCube;
        private float velocity;

        public Water(ref GraphicsDevice device)
        {
            this.device = device;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        public void LoadContent(ContentManager content)
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(device);
            waterModel = content.Load<Model>("oceano");
            waterEffect = content.Load<Effect>("fxoceano");
            noise = content.Load<Texture3D>("NoiseVolume");

            // TODO: use this.Content to load your game content here
        }

        public void Update(GameTime gameTime)
        {
            velocity += ((float)gameTime.ElapsedGameTime.TotalSeconds) * 5.5f;
            waterEffect.Parameters["time_0_X"].SetValue(velocity);
        }

        public void Draw(GameTime gameTime, ref GameCamera camera, ref TextureCube cube)
        {

            foreach (ModelMesh mesh in waterModel.Meshes)
            {
                Matrix World = Matrix.CreateScale(3000) *
                              Matrix.CreateRotationX(-MathHelper.PiOver2) *
                              Matrix.CreateTranslation(new Vector3(3000, -390, 0));
                //GraphicsDevice.Indices = mesh.IndexBuffer;
                waterEffect.Parameters["World"].SetValue(World);
                waterEffect.Parameters["View"].SetValue(camera.GetViewMatrix());
                waterEffect.Parameters["VI"].SetValue(Matrix.Invert(camera.GetViewMatrix()));
                waterEffect.Parameters["Projection"].SetValue(camera.GetProjectionMatrix());
                waterEffect.Parameters["Noise_tex"].SetValue(noise);
                waterEffect.Parameters["skyBox_Tex"].SetValue(cube);
                waterEffect.Parameters["waterColor"].SetValue(new Vector4(0.25f, 0.35f, 0.65f, 1));
                device.Indices = mesh.IndexBuffer;

                waterEffect.Begin();
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    waterEffect.CurrentTechnique.Passes[0].Begin();
                    device.VertexDeclaration = part.VertexDeclaration;
                    device.Vertices[0].SetSource(mesh.VertexBuffer,
                                                         part.StreamOffset,
                                                         part.VertexStride);
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                         part.BaseVertex,
                                                         0,
                                                         part.NumVertices,
                                                         part.StartIndex,
                                                         part.PrimitiveCount);
                    waterEffect.CurrentTechnique.Passes[0].End();
                }
                waterEffect.End();
            }
        }
    }
}
