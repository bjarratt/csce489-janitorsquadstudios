using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CaveOfShadows
{
    class Reticle
    {
        private VertexBuffer vBuffer;
        private VertexDeclaration vertexDeclaration;

        private int numTriangles;
        private Matrix viewMatrix;

        private Effect shader;

        private Vector4 color;

        private float innerRadius;
        private float outerRadius;

        private int numToDraw;

        private bool animationRunning = false;

        public bool AnimationRunning
        {
            get { return animationRunning; }
        }

        public Reticle(GraphicsDevice device, float innerRadius, float outerRadius, Vector4 color, int numTriangles)
        {
            this.numTriangles = numTriangles;

            if (this.numTriangles < 3)
            {
                this.numTriangles = 3;
            }

            // There is one more vertex than there are segments
            VertexPositionColor[] vertices = new VertexPositionColor[this.numTriangles + 2];
            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionColor.VertexElements);

            float angleIncrement = MathHelper.TwoPi / (float)this.numTriangles; // In radians
            float currentAngle = 0; // In radians

            this.innerRadius = innerRadius;
            this.outerRadius = outerRadius;

            vertices[0] = new VertexPositionColor();
            vertices[0].Position = Vector3.Zero;
            vertices[0].Color = new Color(color);

            for (int i = 1; i <= this.numTriangles + 1; i++)
            {
                currentAngle = angleIncrement * (float)i;

                vertices[i] = new VertexPositionColor();
                vertices[i].Position = new Vector3(outerRadius * (float)Math.Cos(currentAngle), outerRadius * (float)Math.Sin(currentAngle), -1.0f);
                vertices[i].Color = new Color(color);
            }

            this.vBuffer = new VertexBuffer(device, vertices.Length * VertexPositionColor.SizeInBytes, BufferUsage.WriteOnly);
            this.vBuffer.SetData(vertices);
            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionColor.VertexElements);

            this.viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);

            this.color = color;

            this.numToDraw = this.numTriangles;
        }

        public void LoadContent(ref ContentManager content)
        {
            shader = content.Load<Effect>("Render2D");
        }

        public void StartAnimation()
        {
            this.numToDraw = 0;
            this.animationRunning = true;
        }

        public void Draw(GraphicsDevice device)
        {
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
            device.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
            device.RenderState.AlphaTestEnable = true;

            if (this.numToDraw < this.numTriangles)
            {
                this.numToDraw += 1;
            }
            else
            {
                this.animationRunning = false;
            }

            Vector3 screenCenter = new Vector3( (float)device.Viewport.Width / 2.0f, (float)device.Viewport.Height / 2.0f, -1.0f );
            Matrix projectionMatrix = Matrix.CreateOrthographicOffCenter(0,
                (float)device.Viewport.Width,
                (float)device.Viewport.Height,
                0, 1.0f, 1000.0f);

            Matrix worldMatrix = Matrix.Identity;
            worldMatrix.Translation = new Vector3(screenCenter.X, screenCenter.Y, 0.0f);

            shader.Parameters["World"].SetValue(worldMatrix);
            shader.Parameters["View"].SetValue(viewMatrix);
            shader.Parameters["Projection"].SetValue(projectionMatrix);
            shader.Parameters["reticleColor"].SetValue(this.color);
            shader.Parameters["reticlePosition"].SetValue(screenCenter);
            shader.Parameters["reticleInnerRadius"].SetValue(innerRadius);
            shader.Parameters["reticleOuterRadius"].SetValue(outerRadius);

            shader.CurrentTechnique = shader.Techniques["Reticle"];

            shader.Begin();

            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                pass.Begin();

                device.VertexDeclaration = this.vertexDeclaration;
                device.Vertices[0].SetSource(this.vBuffer, 0, VertexPositionColor.SizeInBytes);
                device.DrawPrimitives(PrimitiveType.TriangleFan, 0, this.numToDraw);

                pass.End();
            }

            shader.End();
        }
    }
}
