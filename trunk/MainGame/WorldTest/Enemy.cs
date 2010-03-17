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
    class Enemy : Agent
    {
        #region Properties

        #endregion

        #region Constructor

        public Enemy(GraphicsDeviceManager Graphics, ContentManager Content, string enemy_name) : base(Graphics, Content, enemy_name)
        {
            position = new Vector3(0, 100, 100);
            speed = 0.0f;

            rotation = 0.0f;
            turn_speed = 0.05f;
            turn_speed_reg = 1.6f;
            movement_speed_reg = 2.0f; // 14

            orientation = Quaternion.Identity;
            worldTransform = Matrix.Identity;
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads the model into the skinnedModel property.
        /// </summary>
        private void LoadSkinnedModel()
        {
            // Loads an animated model

            model = content.Load<SkinnedModel>(skinnedModelFile);

            // Copy the absolute transformation of each node
            absoluteBoneTransforms = new Matrix[model.Model.Bones.Count];
            model.Model.CopyBoneTransformsTo(absoluteBoneTransforms);

            // Creates an animation controller
            controller = new AnimationController(model.SkeletonBones);

            // Start the first animation stored in the AnimationClips dictionary
            controller.StartClip(
                model.AnimationClips.Values[activeAnimationClip]);
        }

        public override void LoadContent()
        {
            max_textures = 3;
            max_targets = 3;
            textures = new Texture2D[max_textures];
            render_targets = new RenderTarget2D[max_targets];

            shader = content.Load<Effect>("CelShade");
            textures.SetValue(content.Load<Texture2D>("ColorMap"), (int)Tex_Select.model);
            textures.SetValue(content.Load<Texture2D>("Toon"), (int)Tex_Select.cel_tex);

            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            render_targets[(int)Target_Select.normalDepth] = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);

            LoadSkinnedModel();
        }

        #endregion

        #region Update

        public void Update(GameTime gameTime, ref StaticGeometry terrain)
        {
            //reset rotation
            rotation = 0.0f;

            //turn speed is same even if machine running slow
            turn_speed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turn_speed *= turn_speed_reg;

            MoveForward(ref position, Quaternion.Identity, 0.0f, Vector4.Zero, ref terrain);
            worldTransform.Translation = position;

            // Update the animation according to the elapsed time
            controller.Update(gameTime.ElapsedGameTime, Matrix.Identity);

        }

        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed, Vector4 stick, ref StaticGeometry terrain)
        {
            position = terrain.CollideWith(position, new Vector3(0, -1, 0), 0.1, StaticGeometry.MAX_RECURSIONS);
        }

        #endregion

        #region Draw

        public void DrawCel(GameTime gameTime, Matrix view, Matrix projection,
            ref RenderTarget2D scene, ref RenderTarget2D shadow, ref List<Light> Lights)
        {

            #region Cel Shading

            //Cel shading
            graphics.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            graphics.GraphicsDevice.RenderState.AlphaTestEnable = false;
            graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;

            // Set the MeshPart Effect to our shader and set the model texture
            foreach (ModelMesh modelMesh in model.Model.Meshes)
            {
                for (int i = 0; i < modelMesh.MeshParts.Count; i++)
                {
                    modelMesh.MeshParts[i].Effect = shader;
                }
            }

            // Set parameters for the shader
            foreach (ModelMesh modelMesh in model.Model.Meshes)
            {
                foreach (Effect effect in modelMesh.Effects)
                {
                    effect.Parameters["matW"].SetValue(absoluteBoneTransforms[modelMesh.ParentBone.Index] * worldTransform);
                    effect.Parameters["matBones"].SetValue(controller.SkinnedBoneTransforms);
                    effect.Parameters["matVP"].SetValue(view * projection);
                    effect.Parameters["matVI"].SetValue(Matrix.Invert(view));
                    //effect.Parameters["shadowMap"].SetValue(shadow.GetTexture());
                    effect.Parameters["diffuseMap0"].SetValue(textures[(int)Tex_Select.model]);
                    effect.Parameters["CelMap"].SetValue(textures[(int)Tex_Select.cel_tex]);
                    effect.Parameters["ambientLightColor"].SetValue(new Vector3(0.1f));
                    effect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3(1.0f));
                    effect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.3f));
                    effect.Parameters["material"].StructureMembers["specularPower"].SetValue(10);
                    effect.Parameters["diffuseMapEnabled"].SetValue(true);
                    for (int i = 0; i < Lights.Count; i++)
                    {
                        effect.Parameters["lights"].Elements[i].StructureMembers["color"].SetValue(Lights[i].color);
                        effect.Parameters["lights"].Elements[i].StructureMembers["position"].SetValue(Lights[i].position);
                    }
                    switch (Lights.Count)
                    {
                        case 1: effect.CurrentTechnique = effect.Techniques["AnimatedModel_OneLight"];
                            break;
                        case 2: effect.CurrentTechnique = effect.Techniques["AnimatedModel_TwoLight"];
                            break;
                        case 4: effect.CurrentTechnique = effect.Techniques["AnimatedModel_FourLight"];
                            break;
                        case 6: effect.CurrentTechnique = effect.Techniques["AnimatedModel_SixLight"];
                            break;
                        case 8: effect.CurrentTechnique = effect.Techniques["AnimatedModel_EightLight"];
                            break;
                        default: effect.CurrentTechnique = effect.Techniques["AnimatedModel_OneLight"];
                            break;
                    }
                }

                // Draw model mesh
                foreach (EffectPass pass in shader.CurrentTechnique.Passes)
                {
                    modelMesh.Draw();
                }
            }

            #endregion

        }

        #endregion
    }
}
