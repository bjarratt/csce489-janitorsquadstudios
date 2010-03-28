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

        Level firstLevel;

        //Texture2D terrainTexture;

        GameCamera camera;
        static public bool invertYAxis;

        Player player;
        List<Enemy> enemies;

        private List<Light> lights;

        /// <summary>
        /// Stores the last keyboard state and gamepad state.
        /// </summary>
        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
        KeyboardState lastKeyboradState;
        GamePadState lastgamepadState;

        /// <summary>
        /// Particle Effects
        /// </summary>
        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleSystem projectileTrailParticles;
        ParticleSystem smokePlumeParticles;
        ParticleSystem fireParticles;

        /// <summary>
        /// List of attacks
        /// </summary>
        List<Projectile> projectiles = new List<Projectile>();

        // Random number generator for the fire effect.
        Random random = new Random();

        ///<summary>
        /// This is the cel shader effect... basically the same as
        /// SkinnedModelBasicEffect.
        ///</summary>
        //Effect cel_effect;
        //Texture2D m_celMap;

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

            //terrain = new StaticGeometry(graphics.GraphicsDevice, "Cave1.obj", "cave1_collision.obj", Vector3.Zero, ref content);
            firstLevel = new Level(graphics.GraphicsDevice, ref content, ref lights, "first_level.txt");

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this.ScreenManager.game, content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this.ScreenManager.game, content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this.ScreenManager.game, content);
            smokePlumeParticles = new SmokePlumeParticleSystem(this.ScreenManager.game, content);
            fireParticles = new FireParticleSystem(this.ScreenManager.game, content);

            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke.
            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;

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

            // Register the particle system components.
            this.ScreenManager.game.Components.Add(explosionParticles);
            this.ScreenManager.game.Components.Add(explosionSmokeParticles);
            this.ScreenManager.game.Components.Add(projectileTrailParticles);
            this.ScreenManager.game.Components.Add(smokePlumeParticles);
            this.ScreenManager.game.Components.Add(fireParticles);

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
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (IsActive)
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

                player.Update(gameTime, currentGamePadState, lastgamepadState, currentKeyboardState, lastKeyboradState, ref this.firstLevel);
                camera.UpdateCamera(gameTime, currentGamePadState, lastgamepadState, currentKeyboardState, invertYAxis);

                foreach (Enemy e in enemies)
                {
                    e.Update(gameTime, ref this.firstLevel);
                }

                UpdateAttacks(gameTime, currentGamePadState, lastgamepadState, currentKeyboardState, lastKeyboradState);
                UpdateProjectiles(gameTime);

                // Save previous states
                lastKeyboradState = currentKeyboardState;
                lastgamepadState = currentGamePadState;
            }
        }

        /// <summary>
        /// Helper for updating the explosions effect.
        /// </summary>
        void UpdateAttacks(GameTime gameTime, GamePadState current_g_state, GamePadState prev_g_state, 
                             KeyboardState current_k_state, KeyboardState prev_k_state)
        {

            if (current_g_state.Buttons.B == ButtonState.Pressed && prev_g_state.Buttons.B == ButtonState.Released)
            {
                // Create a new projectile once per second. The real work of moving
                // and creating particles is handled inside the Projectile class.
                projectiles.Add(new Attack(player.position + new Vector3(0,20,0), camera.lookAt * 100.0f, 200, 30, 20, 60f, 0, explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles));
            }
        }

        /// <summary>
        /// Helper for updating the list of active projectiles.
        /// </summary>
        void UpdateProjectiles(GameTime gameTime)
        {
            int i = 0;

            while (i < projectiles.Count)
            {
                if (!projectiles[i].Update(gameTime, ref firstLevel))
                {
                    // Remove projectiles at the end of their life.
                    projectiles.RemoveAt(i);
                }
                else
                {
                    // Advance to the next projectile.
                    i++;
                }
            }
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

            // Pass camera matrices through to the particle system components.
            explosionParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            explosionSmokeParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            projectileTrailParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            smokePlumeParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            fireParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());

            //Cel Shading pass
            graphics.GraphicsDevice.Clear(Color.Black);
            player.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref lights);
            foreach (Enemy e in enemies)
            {
                e.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref lights);
            }

            //terrain.Draw(graphics.GraphicsDevice, true, ref camera);
            firstLevel.Draw(graphics.GraphicsDevice, ref camera);

            base.Draw(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }
        #endregion
    }
}
