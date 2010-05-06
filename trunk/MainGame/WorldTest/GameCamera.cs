#region Using
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using XNAnimation;
using XNAnimation.Controllers;
using XNAnimation.Effects;

#endregion

namespace WorldTest
{
    class GameCamera
    {
        #region Properties

        //3rd person target
        private Player Target;

        public bool first;
        public bool cam_movementup;
        public bool cam_movementside;

        public float cameraArc = -5;
        public float cameraRot = 0;
        public float cameraRoll = 0;
        public float cameraDistance = 50;
        public float cameraMovementUp = 0;
        public float cameraMovementSide = 0;
        private float accumulatedArc = 0;
        //Camera movement and rotation speeds
        public float rotationSpeed = 0.05f;
        private const float VERTICAL_ROT_SPEED = 0.05f;  // 0.025f
        private const float HORIZONTAL_ROT_SPEED = 0.1f; // 0.05f

        private static Vector3 INITIAL_CAMERA_OFFSET = new Vector3(0, 0, 0);

        //Position and reference vectors
        public Vector3 position;
        public Vector3 up;
        public Vector3 right;
        public Vector3 lookAt;
        
        //Screen ratio
        //public float aspectRatio = 0.0f; 
        
        //Any camera rotations
        public Quaternion camera_rotation;
        public Matrix transform;
        
        //Projection and view matrix
        private Matrix projection; 
        private Matrix view;

        private GraphicsDeviceManager graphics;
        
        #endregion

        #region Constructor

        public GameCamera(GraphicsDeviceManager graphics, ref Player target)
        {
            this.graphics = graphics;

            //Start aiming forward (no turn)
            cameraRot = 0;

            Target = target;
            first = true;
            cam_movementup = true;
            cam_movementside = false;
            if (first)
            {
                position = Target.position + INITIAL_CAMERA_OFFSET;
                up = new Vector3(0, 1, 0);
                right = new Vector3(-1, 0, 0);
                lookAt = /*position +*/ new Vector3(0, 0, 1);
                cameraArc = 0;
                cameraRot = 0;
                cameraRoll = 0;
                cameraDistance = 0;
                cameraMovementUp = 0;
                cameraMovementSide = 0;
            }
            else
            {
                position = new Vector3(0.0f, 30.0f, -cameraDistance);
                up = new Vector3(0, 1, 0);
                right = new Vector3(1, 0, 0);
                lookAt = Target.position;
                lookAt.Y += 15.0f;
                cameraArc = -5;
                cameraRot = 0;
                cameraRoll = 0;
                cameraDistance = 50;
            }

            //Aspect ratio of screen
            //aspectRatio = (float)graphics.GraphicsDevice.Viewport.Width / (float)graphics.GraphicsDevice.Viewport.Height;

            //Initialize our camera rotation to identity
            camera_rotation = Quaternion.Identity;
            transform = Matrix.Identity;

            //Create a general view matrix from start position and original lookat
            view = Matrix.CreateLookAt(position, lookAt, Vector3.Up);

            //Create general projection matrix for the screen
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), (float)graphics.GraphicsDevice.Viewport.Width / (float)graphics.GraphicsDevice.Viewport.Height, 0.01f, 10000.0f);
        }

        #endregion

        #region Methods

        public void UpdateCamera(GameTime gameTime, ControlState inputState, bool invertYAxis)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (inputState.currentGamePadState.Buttons.RightStick == ButtonState.Pressed &&
                inputState.lastGamePadState.Buttons.RightStick == ButtonState.Released)
            {
                first = !first;
                cameraRot = 0.0f;
                cameraArc = 0.0f;
                cameraDistance = 0.0f;
                cameraMovementUp = 0.0f;
                cameraMovementSide = 0.0f;

                Matrix trans = Matrix.CreateFromQuaternion(Target.orientation);
                //this is where the problem most likely resides for orientation getting jacked up when we switch views
                this.lookAt = Vector3.Transform(new Vector3(0, 0, 1), trans);
                this.right = Vector3.Transform(new Vector3(-1, 0, 0), trans);
                this.up = Vector3.Transform(new Vector3(0, 1, 0), trans);
            }

            if (first)
            {
                position = Target.position + INITIAL_CAMERA_OFFSET;
                Vector3 look = this.lookAt;
                look.Y = 0;
                look.Normalize();
                position.Y = position.Y + 110;
                position = position + look * 5.0f;
                cameraArc = 0;
                cameraRot = 0;
                cameraRoll = 0;
                cameraDistance = 0;
            }
            else
            {
                position = new Vector3(0.0f, 30.0f, -cameraDistance);
                position = Vector3.Transform(position, transform);
            }

            // Check for input to rotate the camera up and down around the model.
            if (inputState.currentKeyboardState.IsKeyDown(Keys.Up) /*||
                currentKeyboardState.IsKeyDown(Keys.W)*/)
            {
                if (first)
                {
                    if (invertYAxis)
                    {
                        cameraArc += time * VERTICAL_ROT_SPEED;
                    }
                    else
                    {
                        cameraArc -= time * VERTICAL_ROT_SPEED;
                    }
                }
                else
                    cameraArc += time * VERTICAL_ROT_SPEED;
            }

            if (inputState.currentKeyboardState.IsKeyDown(Keys.Down) /*||
                currentKeyboardState.IsKeyDown(Keys.S)*/)
            {
                if (first)
                {
                    if (invertYAxis)
                    {
                        cameraArc -= time * VERTICAL_ROT_SPEED;
                    }
                    else
                    {
                        cameraArc += time * VERTICAL_ROT_SPEED;
                    }
                }
                else
                    cameraArc -= time * VERTICAL_ROT_SPEED;
            }

            if (invertYAxis)
            {
                float tempArc = accumulatedArc;
                cameraArc -= inputState.currentGamePadState.ThumbSticks.Right.Y * time * HORIZONTAL_ROT_SPEED;
                if ((tempArc + cameraArc) > 55.0f || (tempArc + cameraArc) < -55.0f) cameraArc = 0;
                else accumulatedArc += cameraArc;
            }
            else
            {
                float tempArc = accumulatedArc;
                cameraArc += inputState.currentGamePadState.ThumbSticks.Right.Y * time * HORIZONTAL_ROT_SPEED;
                if ((tempArc + cameraArc) > 55.0f || (tempArc + cameraArc) < -55.0f) cameraArc = 0;
                else accumulatedArc += cameraArc;
            }

            // Limit the arc movement.
            if (cameraArc > 55.0f)
                cameraArc = 55.0f;
            else if (cameraArc < -55.0f)
                cameraArc = -55.0f;

            // Check for input to rotate the camera around the model.
            if (inputState.currentKeyboardState.IsKeyDown(Keys.Right))
            {
                if (first)
                {
                    cameraRot -= time * HORIZONTAL_ROT_SPEED;
                }
                else
                {
                    cameraRot += time * HORIZONTAL_ROT_SPEED;
                }
            }

            if (inputState.currentKeyboardState.IsKeyDown(Keys.Left))
            {
                if (first)
                {
                    cameraRot += time * HORIZONTAL_ROT_SPEED;
                }
                else
                {
                    cameraRot -= time * HORIZONTAL_ROT_SPEED;
                }
            }
            
            if (first)
            {
                cameraRot -= inputState.currentGamePadState.ThumbSticks.Right.X * time * HORIZONTAL_ROT_SPEED;
            }
            else
            {
                cameraRot += inputState.currentGamePadState.ThumbSticks.Right.X * time * HORIZONTAL_ROT_SPEED;
            }

            // Check for input to zoom camera in and out.
            if (inputState.currentKeyboardState.IsKeyDown(Keys.Z))
                cameraDistance += time * 0.25f;

            if (inputState.currentKeyboardState.IsKeyDown(Keys.X))
                cameraDistance -= time * 0.25f;

            cameraDistance += inputState.currentGamePadState.Triggers.Left * time * 0.5f;
            cameraDistance -= inputState.currentGamePadState.Triggers.Right * time * 0.5f;

            // Limit the camera distance.
            if (cameraDistance > 500)
                cameraDistance = 500;
            else if (cameraDistance < 10)
                cameraDistance = 10;

            if (inputState.currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
                inputState.currentKeyboardState.IsKeyDown(Keys.R))
            {
                if (first)
                {
                    cameraArc = 0;
                    cameraRot = 0;
                    cameraRoll = 0;
                    cameraDistance = 0;
                    cameraMovementUp = 0;
                    cameraMovementSide = 0;
                }
                else
                {
                    cameraArc = -5;
                    cameraRot = 0;
                    cameraDistance = 50;
                }
            }

            Vector3 player = Target.position;
            player.Y += 10.0f;
            
            if (first)
            {
                PlayerCameraMovement(cam_movementup, cam_movementside, Target.Status);
                Matrix rollMatrix = Matrix.CreateFromAxisAngle(lookAt, MathHelper.ToRadians(cameraRoll));
                up = Vector3.Transform(up, rollMatrix);
                right = Vector3.Transform(right, rollMatrix);
                Matrix yawMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(cameraRot));
                //lookAt = Vector3.Transform(lookAt, yawMatrix);
                //right = Vector3.Transform(right, yawMatrix);
                Matrix pitchMatrix = Matrix.CreateFromAxisAngle(right, MathHelper.ToRadians(cameraArc));
                Matrix combine = Matrix.Multiply(yawMatrix, pitchMatrix);
                lookAt = Vector3.Normalize(Vector3.Transform(lookAt, combine));
                right = Vector3.Normalize(Vector3.Transform(right, combine));
                up = Vector3.Normalize(Vector3.Transform(up, combine));
                 
                Vector3 target = position + lookAt;
                view = Matrix.CreateLookAt(position, target, up);
            }
            else
            {
                view = Matrix.CreateLookAt(position, player, Vector3.Up);
            }

            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)graphics.GraphicsDevice.Viewport.Width / (float)graphics.GraphicsDevice.Viewport.Height, 1, 10000);
        }

        private void PlayerCameraMovement(bool cam_up, bool cam_side, Player.State state)
        {
            if (state == Player.State.running)
            {
                if (cam_movementup)
                {
                    cameraMovementUp += 0.2f;
                }
                else
                {
                    cameraMovementUp += 0.2f;
                }

                if (cam_movementside)
                {
                    cameraMovementSide += 0.1f;
                }
                else
                {
                    cameraMovementSide -= 0.1f;
                }

                if (cameraMovementUp > (MathHelper.TwoPi * 2) || cameraMovementUp < (-MathHelper.TwoPi * 2))
                {
                    cam_movementup = !cam_movementup;
                    cameraMovementUp = 0;
                }

                if (cameraMovementSide > MathHelper.TwoPi || cameraMovementSide < -MathHelper.TwoPi)
                {
                    cam_movementside = !cam_movementside;
                    cameraMovementSide = 0;
                }

                position += right * (float)Math.Cos((double)cameraMovementSide) * 3.0f;
                position += up * (float)Math.Sin((double)cameraMovementUp) * 1.5f;
            }
            else if (state == Player.State.idle)
            {
                cameraMovementUp += 0.05f;
                cameraMovementSide = MathHelper.PiOver2;

                if (cameraMovementUp > MathHelper.TwoPi)
                {
                    cameraMovementUp = 0;
                }

                position += right * (float)Math.Cos((double)cameraMovementSide);
                position += up * (float)Math.Sin((double)cameraMovementUp);
            }
        }

        public Matrix GetProjectionMatrix() 
        {
            return projection; 
        }

        public Matrix GetViewMatrix()
        {
            //Get the newest view
            //view = Matrix.CreateLookAt(position, lookAt, Vector3.Up); 
            return view;
        }

        #endregion
    }
}
