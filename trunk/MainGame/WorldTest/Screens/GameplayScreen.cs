#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using System.Runtime;
#endregion

namespace WorldTest
{
    /// <summary>
    /// This screen implements the actual game logic. 
    /// </summary>
    public class GameplayScreen : GameScreen
    {
        #region Fields

        GraphicsDeviceManager graphics;
        ContentManager content;

        SpriteFont gameFont;

        StaticGeometry terrain;

        Texture2D terrainTexture;

        GameCamera camera;
        static public bool invertYAxis;

        Player player;
        List<Enemy> enemies;

        List<Light> lights;

        /// <summary>
        /// Stores the last keyboard state and gamepad state.
        /// </summary>
        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
        KeyboardState lastKeyboradState;
        GamePadState lastgamepadState;



        ///<summary>
        /// This is the cel shader effect... basically the same as
        /// SkinnedModelBasicEffect.
        ///</summary>
        Effect cel_effect;
        Texture2D m_celMap;

        /// <summary>
        /// Render targets for the different shaders... 
        /// We do this because the outline shader needs the Normal/Depth
        /// texture as a separate entity to do its work.
        /// </summary>
        RenderTarget2D sceneRenderTarget;
        RenderTarget2D shadowRenderTarget;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen(ScreenManager sm)
        {
            this.ScreenManager = sm;
            //ScreenManager.Game.Content.RootDirectory = "Content";
            this.graphics = sm.graphics;

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            this.Initialize();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected void Initialize()
        {
            //initialize content
            lights = new List<Light>();
            lights.Add(new Light(new Vector3(100, 100, 100), new Vector3(1, 1, 1)));
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            gameFont = content.Load<SpriteFont>("gamefont");

            //main game screen content
            // Create a new SpriteBatch, which can be used to draw textures.

            GraphicsDevice device = graphics.GraphicsDevice;

            player = new Player(graphics, content);
            camera = new GameCamera(graphics, ref player);
            player.InitCamera(ref camera);

            player.LoadContent();

            enemies = new List<Enemy>();
            enemies.Add(new Enemy(graphics, content, "enemy_bind_pose"));

            foreach (Enemy e in enemies)
            {
                e.LoadContent();
            }

            // Load Cel Shader
            cel_effect = content.Load<Effect>("CelShade");
            m_celMap = content.Load<Texture2D>("Toon");

            terrain = new StaticGeometry(graphics.GraphicsDevice, "Cave1.obj", "cave1_collision.obj", Vector3.Zero);
            //collision_mesh = new StaticGeometry(graphics.GraphicsDevice, "cave1_collision.obj", "", Vector3.Zero);

            this.terrainTexture = content.Load<Texture2D>("tex");

            //Set up RenderTargets
            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            sceneRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);

            shadowRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);
            //player and HUD content

            // Start the sound!

            // Simulate loading time (if necessary)
            //Thread.Sleep(1000);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                 bool coveredByOtherScreen)
        {
            // Get states for keys and pad
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Allows the game to exit
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
               // this.Exit();

            if (currentGamePadState.Buttons.Y == ButtonState.Pressed && lastgamepadState.Buttons.Y == ButtonState.Released)
            {
                invertYAxis = !invertYAxis;
            }

            player.Update(gameTime, currentGamePadState, lastgamepadState, currentKeyboardState, lastKeyboradState, ref this.terrain);
            camera.UpdateCamera(gameTime, currentGamePadState, lastgamepadState, currentKeyboardState, invertYAxis);

            foreach (Enemy e in enemies)
            {
                e.Update(gameTime, ref this.terrain);
            }

            // Save previous states
            lastKeyboradState = currentKeyboardState;
            lastgamepadState = currentGamePadState;

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
        #endregion

        #region Handle Input
        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
               ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                // Otherwise move the player position
            }
        }
        #endregion

        #region Draw
        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            GraphicsDevice device = graphics.GraphicsDevice;

            #region ShadowMap
            /*
            graphics.GraphicsDevice.SetRenderTarget(0, shadowRenderTarget);
            graphics.GraphicsDevice.Clear(Color.Black);
            cel_effect.CurrentTechnique = cel_effect.Techniques["ShadowMap"];
            Matrix lightView = Matrix.CreateLookAt(lights[0].position, player.position, Vector3.Up);
            Matrix lightProj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, camera.aspectRatio, 1.0f, 1000f);
            cel_effect.Parameters["lightview"].Elements[0].SetValue(player.worldTransform * lightView * lightProj);

            foreach (ModelMesh modelMesh in player.model.Model.Meshes)
            {
                modelMesh.Draw();
            }
            this.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
            this.cel_effect.Begin();
            foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                this.terrain.Draw(this.GraphicsDevice);

                pass.End();
            }
            this.cel_effect.End();
            this.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            */
            #endregion

            //Cel Shading pass
            graphics.GraphicsDevice.Clear(Color.Black);
            player.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref lights);
            foreach (Enemy e in enemies)
            {
                e.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref lights);
            }

            //Draw terrain
            graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;

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
            cel_effect.Parameters["lights"].Elements[0].StructureMembers["color"].SetValue(new Vector3(1.0f));
            cel_effect.Parameters["lights"].Elements[0].StructureMembers["position"].SetValue(new Vector3(100, 100, 100));

            this.cel_effect.Begin();
            foreach (EffectPass pass in cel_effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                this.terrain.Draw(graphics.GraphicsDevice, true);
                //this.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;
                //this.collision_mesh.Draw(this.GraphicsDevice);
                //this.GraphicsDevice.RenderState.FillMode = FillMode.Solid;

                pass.End();
            }

            this.cel_effect.End();
            graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            base.Draw(gameTime);

            // create a new spritebatch for any on-screen text
            //SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            //spriteBatch.Begin();
            ////draw any necessary content
            //spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }
        #endregion
    }
}
