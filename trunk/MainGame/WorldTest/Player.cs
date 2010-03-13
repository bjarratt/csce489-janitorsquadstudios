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
    class Player : Agent
    {
        #region Properties

        //GraphicsDeviceManager graphics;
        //ContentManager content;

        public GameCamera camera;

        //public Vector3 position;
        //public Vector3 velocity;
        //public Vector3 reference;
        //private float movement_speed_reg;

        //private float rotation;
        //private float turn_speed;
        //private float turn_speed_reg;

        //public Quaternion orientation;
        //public Matrix worldTransform;

        //public Effect shader;
        //public SkinnedModel model;
        //readonly string skinnedModelFile = "PlayerMarine";

        //public Matrix[] absoluteBoneTransforms;
        //public Texture2D[] textures;
        //public int max_textures;
        //public RenderTarget2D[] render_targets;
        //public int max_targets;

        //public AnimationController controller;
        //public int activeAnimationClip;

        //public enum Tex_Select
        //{
        //    model = 0,
        //    cel_tex
        //}

        //public enum Target_Select
        //{
        //    scene = 0,
        //    normalDepth
        //}

        #endregion

        #region Constructor

        public Player(GraphicsDeviceManager Graphics, ContentManager Content) : base(Graphics, Content, "PlayerMarine")
        {
            position = Vector3.Zero;
            position.Y += 150.0f;
            position.Z += 100.0f;
            velocity = Vector3.Zero;

            rotation = 0.0f;
            turn_speed = 0.05f;
            turn_speed_reg = 1.6f;
            movement_speed_reg = 2.0f; // 14

            orientation = Quaternion.Identity;
            worldTransform = Matrix.Identity;
            worldTransform.Translation = position;
        }

        #endregion

        #region Initialize

        public void InitCamera(ref GameCamera cam)
        {
            camera = cam;
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

        public void Update(GameTime gameTime, GamePadState currentGPState,
            GamePadState lastGPState, KeyboardState currentKBState,
            KeyboardState lastKBState, ref StaticGeometry terrain)
        {
            //reset rotation
            rotation = 0.0f;

            //turn speed is same even if machine running slow
            turn_speed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turn_speed *= turn_speed_reg;

            Vector4 stickL = new Vector4(currentGPState.ThumbSticks.Left, 0.0f, 0.0f);
            Vector4 stickR = new Vector4(currentGPState.ThumbSticks.Right, 0.0f, 0.0f);

            if (stickL != Vector4.Zero && stickL.Y < 0) controller.PlaybackMode = PlaybackMode.Backward;
            else controller.PlaybackMode = PlaybackMode.Forward;

            // Animate Player
            if (stickL == Vector4.Zero &&
                    lastGPState.ThumbSticks.Left != Vector2.Zero)
            {
                controller.CrossFade(model.AnimationClips.Values[0],
                    TimeSpan.FromMilliseconds(300));
            }
            if (lastGPState.ThumbSticks.Left == Vector2.Zero &&
                stickL != Vector4.Zero)
            {
                controller.CrossFade(model.AnimationClips.Values[1],
                    TimeSpan.FromMilliseconds(300));
            }

            if (stickL.X != 0.0f)
            {
                if (stickL.X > 0) rotation -= turn_speed;
                if (stickL.X < 0) rotation += turn_speed;
            }
            if (!camera.first)
            {
                orientation = orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotation);
                worldTransform = Matrix.CreateFromQuaternion(orientation);
                worldTransform.Translation = position;

                float moveSpeed = (float)gameTime.ElapsedGameTime.Milliseconds / movement_speed_reg;
                MoveForward(ref position, orientation, moveSpeed, stickL, ref terrain);

                camera.camera_rotation = orientation * Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.ToRadians(camera.cameraRot));
                camera.camera_rotation = camera.camera_rotation * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(camera.cameraArc));
                camera.camera_rotation.Normalize();
                camera.transform = Matrix.CreateFromQuaternion(camera.camera_rotation);
                camera.transform.Translation = position;
            }
            else
            {
                orientation = orientation * Quaternion.CreateFromRotationMatrix(Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(camera.cameraRot)));
                worldTransform = Matrix.CreateFromQuaternion(orientation);
                worldTransform.Translation = position;
                float moveSpeed = (float)gameTime.ElapsedGameTime.Milliseconds / movement_speed_reg;
                MoveForward(ref position, orientation, moveSpeed, stickL, ref terrain);
            }

            // Update the animation according to the elapsed time
            controller.Update(gameTime.ElapsedGameTime, Matrix.Identity);

        }

        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed, Vector4 stick, ref StaticGeometry terrain)
        {
            if (camera.first)
            {
                Vector3 addVector = Vector3.Zero;
                if (stick.X > 0)
                {
                    addVector += camera.right * speed;
                }
                else if (stick.X < 0)
                {
                    addVector -= camera.right * speed;
                }
                if (stick.Y > 0)
                {
                    addVector += camera.lookAt * speed;
                }
                else if (stick.Y < 0)
                {
                    addVector -= camera.lookAt * speed;
                }
                addVector.Y = 0.0f;

                position = terrain.CollideWith(position, addVector + new Vector3(0, -1, 0), 0.8, StaticGeometry.MAX_RECURSIONS);
            }
            else
            {
                Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), rotationQuat);
                if (stick == Vector4.Zero)
                {
                    addVector = Vector3.Zero;
                }

                if (stick.Y > 0)
                {
                    position = terrain.CollideWith(position, -addVector * speed + new Vector3(0, -1, 0), 0.8, StaticGeometry.MAX_RECURSIONS); // + new Vector3(0,-1,0
                    //position -= addVector * speed;
                }
                else
                {
                    position = terrain.CollideWith(position, addVector * speed + new Vector3(0, -1, 0), 0.8, StaticGeometry.MAX_RECURSIONS);
                    //position += addVector * speed;
                }
            }
        }

        #endregion

        #region Draw

        public void Draw(GameTime gameTime, Matrix view, Matrix projection,
            ref RenderTarget2D scene, ref RenderTarget2D shadow, ref List<Light> Lights)
        {

            #region NormalDepth Rendering

            //NormalDepth rendering
            graphics.GraphicsDevice.SetRenderTarget(0, render_targets[(int)Target_Select.normalDepth]);
            graphics.GraphicsDevice.Clear(Color.Black);
            graphics.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            graphics.GraphicsDevice.RenderState.AlphaTestEnable = false;
            graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
            shader.CurrentTechnique = shader.Techniques["NormalDepth"];

            foreach (ModelMesh mesh in model.Model.Meshes)
            {
                shader.Parameters["matW"].SetValue(absoluteBoneTransforms[mesh.ParentBone.Index] * worldTransform);
                shader.Parameters["matBones"].SetValue(controller.SkinnedBoneTransforms);
                shader.Parameters["matVP"].SetValue(view * projection);
                foreach (ModelMeshPart part in mesh.MeshParts)
                    part.Effect = shader;
                mesh.Draw();
            }

            #endregion


            #region Cel Shading

            //Cel shading
            graphics.GraphicsDevice.SetRenderTarget(0, scene);
            graphics.GraphicsDevice.Clear(Color.Black);
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
                modelMesh.Draw();
            }

            #endregion

        }

        #endregion


    }
}
