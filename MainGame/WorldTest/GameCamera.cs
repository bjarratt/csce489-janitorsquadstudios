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
        Agent Target;

        public bool first;

        public float cameraArc = -5;
        public float cameraRot = 0;
        public float cameraDistance = 50;

        //Camera movement and rotation speeds
        public float rotationSpeed = 0.05f; 

        //Position and reference vectors
        private Vector3 position; 
        private Vector3 lookAt;
        
        //Screen ratio
        public float aspectRatio = 0.0f; 
        
        //Any camera rotations
        public Quaternion camera_rotation;
        public Matrix transform;
        
        //Projection and view matrix
        private Matrix projection; 
        private Matrix view;
        
        #endregion

        #region Constructor

        public GameCamera(GraphicsDeviceManager graphics, ref Agent target)
        {
            //Start aiming forward (no turn)
            cameraRot = 0;

            Target = target;
            first = false;
            if (first)
            {
                lookAt = new Vector3(0, 0, 1);
            }
            else
            {
                lookAt = Target.position;
                lookAt.Y += 15.0f;
            }
            
            //Starting position of the camera
            if (first)
            {
                position = Target.position;
                position.Z += 10;
            }
            else
            {
                position = new Vector3(0.0f, 30.0f, -cameraDistance);
            }

            //Aspect ratio of screen
            aspectRatio = graphics.GraphicsDevice.Viewport.Width / graphics.GraphicsDevice.Viewport.Height;
            
            //Initialize our camera rotation to identity
            camera_rotation = Quaternion.Identity;
            transform = Matrix.Identity;
            
            //Create a general view matrix from start position and original lookat
            view = Matrix.CreateLookAt(position, lookAt, Vector3.Up);

            //Create general projection matrix for the screen
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), aspectRatio, 0.01f, 10000.0f);
        }

        #endregion

        #region Methods

        public void UpdateCamera(GameTime gameTime, GamePadState currentGamePadState, KeyboardState currentKeyboardState)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (first)
            {
                position = Target.position;
                //position = Vector3.Transform(position, transform);
            }
            else
            {
                position = new Vector3(0.0f, 30.0f, -cameraDistance);
                position = Vector3.Transform(position, transform);
            }

            // Check for input to rotate the camera up and down around the model.
            if (currentKeyboardState.IsKeyDown(Keys.Up) ||
                currentKeyboardState.IsKeyDown(Keys.W))
            {
                cameraArc += time * 0.025f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Down) ||
                currentKeyboardState.IsKeyDown(Keys.S))
            {
                cameraArc -= time * 0.025f;
            }

            cameraArc += currentGamePadState.ThumbSticks.Right.Y * time * 0.05f;

            // Limit the arc movement.
            if (cameraArc > 55.0f)
                cameraArc = 55.0f;
            else if (cameraArc < -90.0f)
                cameraArc = -90.0f;

            // Check for input to rotate the camera around the model.
            if (currentKeyboardState.IsKeyDown(Keys.Right) ||
                currentKeyboardState.IsKeyDown(Keys.D))
            {
                cameraRot += time * 0.05f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Left) ||
                currentKeyboardState.IsKeyDown(Keys.A))
            {
                cameraRot -= time * 0.05f;
            }

            cameraRot += currentGamePadState.ThumbSticks.Right.X * time * 0.05f;

            // Check for input to zoom camera in and out.
            if (currentKeyboardState.IsKeyDown(Keys.Z))
                cameraDistance += time * 0.25f;

            if (currentKeyboardState.IsKeyDown(Keys.X))
                cameraDistance -= time * 0.25f;

            cameraDistance += currentGamePadState.Triggers.Left * time * 0.5f;
            cameraDistance -= currentGamePadState.Triggers.Right * time * 0.5f;

            // Limit the camera distance.
            if (cameraDistance > 500)
                cameraDistance = 500;
            else if (cameraDistance < 10)
                cameraDistance = 10;

            if (currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraArc = -5;
                cameraRot = 0;
                cameraDistance = 50;
            }

            Vector3 player = Target.position;
            player.Y += 15.0f;
            if (first)
            {
                view = Matrix.CreateLookAt(position, Target.reference, Vector3.Up);
            }
            else
            {
                view = Matrix.CreateLookAt(position, player, Vector3.Up);
            }

            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                  aspectRatio,
                                                                  1, 10000);
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
