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

        public GameCamera camera;

        public enum State
        {
            idle = 0,
            running,
            jumping
        };

        State state = State.idle;

        public State Status
        {
            get { return state; }
            set { state = value; }
        }

        #endregion

        #region Constructor

        public Player(GraphicsDeviceManager Graphics, ContentManager Content) : base(Graphics, Content, "PlayerMarine")
        {
            position = Vector3.Zero;
            velocity = Vector3.Zero;
            speed = 2.5f; // In meters/second

            rotation = 0.0f;
            turn_speed = 0.10f; // 0.05
            turn_speed_reg = 1.6f;
            movement_speed_reg = 2.0f; // 14
            this.speedScale = 200.0f;
            this.previousVelocity = Vector3.Zero;

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
            KeyboardState lastKBState, ref Level currentLevel)
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

                //float moveSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / movement_speed_reg;
                float moveSpeed_ms = (float)gameTime.ElapsedGameTime.TotalSeconds * this.speed * this.speedScale;
                InputState input = new InputState();
                MoveForward(gameTime, ref position, orientation, moveSpeed_ms, stickL, currentKBState, currentGPState, lastGPState, ref currentLevel);
                
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
                //float moveSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / movement_speed_reg;
                float moveSpeed_ms = (float)gameTime.ElapsedGameTime.TotalSeconds * this.speed * this.speedScale;
                MoveForward(gameTime, ref position, orientation, moveSpeed_ms, stickL, currentKBState, currentGPState, lastGPState, ref currentLevel);
            }

            // Update the animation according to the elapsed time
            controller.Update(gameTime.ElapsedGameTime, Matrix.Identity);

        }

        private void MoveForward(GameTime gameTime, ref Vector3 position, Quaternion rotationQuat, float speed, 
            Vector4 stick, KeyboardState currentKeyState, GamePadState current_gamepad, GamePadState prev_gamepad, ref Level currentLevel)
        {
            if (camera.first)
            {
                if (!(Status == State.jumping))
                {
                    velocity.X = 0.0f;
                    velocity.Z = 0.0f;
                }
                if ((stick.X > 0 || currentKeyState.IsKeyDown(Keys.D)) && !(Status == State.jumping))
                {
                    Status = State.running;
                    velocity += camera.right * speed;
                }
                else if ((stick.X < 0 || currentKeyState.IsKeyDown(Keys.A)) && !(Status == State.jumping))
                {
                    Status = State.running;
                    velocity -= camera.right * speed;
                }

                if ((stick.Y > 0 || currentKeyState.IsKeyDown(Keys.W)) && !(Status == State.jumping))
                {
                    this.Status = State.running;
                    velocity.X += camera.lookAt.X * speed;
                    velocity.Z += camera.lookAt.Z * speed;
                }
                else if ((stick.Y < 0 || currentKeyState.IsKeyDown(Keys.S)) && !(Status == State.jumping))
                {
                    this.Status = State.running;
                    velocity.X -= camera.lookAt.X * speed;
                    velocity.Z -= camera.lookAt.Z * speed;
                }
                else if (Status == State.jumping)
                {
                    float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    velocity.Y -= elapsedTime * 9.8f; // gravity = 9.8 m/s^2
                }
                else 
                {
                    this.Status = State.idle;
                }

                if (Status == State.running || Status == State.idle)
                {
                    velocity.Y = -0.5f;
                }

                if (current_gamepad.Buttons.A == ButtonState.Pressed && prev_gamepad.Buttons.A == ButtonState.Released && !(Status == State.jumping))
                {
                    this.Status = State.jumping;
                    velocity.Y += 8;
                    velocity.X *= 0.5f;
                    velocity.Z *= 0.5f;
                }

                Vector3 oldpos = position;
                position = currentLevel.CollideWith(position, velocity, 0.8, Level.MAX_COLLISIONS);

                if ((oldpos + velocity) == position)    //no collisions
                {
                    Status = State.jumping;
                }
                else if ((stick.Y > 0 || currentKeyState.IsKeyDown(Keys.W)) && !(Status == State.jumping))
                {
                    Status = State.running;
                }
                else if ((stick.Y < 0 || currentKeyState.IsKeyDown(Keys.S)) && !(Status == State.jumping))
                {
                    Status = State.running;
                }
                else
                {
                    Status = State.idle;
                }
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
                    position = currentLevel.CollideWith(position, -addVector * speed + new Vector3(0, -1, 0), 0.8, Level.MAX_COLLISIONS);
                    //position -= addVector * speed;
                }
                else
                {
                    position = currentLevel.CollideWith(position, addVector * speed + new Vector3(0, -1, 0), 0.8, Level.MAX_COLLISIONS);
                    //position += addVector * speed;
                }
            }
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
                        //Vector3 pos = Vector3.Transform(Lights[i].position, worldTransform);
                        effect.Parameters["lights"].Elements[i].StructureMembers["color"].SetValue(Lights.ElementAt(i).color);
                        effect.Parameters["lights"].Elements[i].StructureMembers["position"].SetValue(Lights.ElementAt(i).position);
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
