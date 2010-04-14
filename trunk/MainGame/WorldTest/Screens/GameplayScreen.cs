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
    struct ControlState
    {
        public KeyboardState currentKeyboardState;
        public GamePadState currentGamePadState;
        public MouseState currentMouseState;
        public KeyboardState lastKeyboardState;
        public GamePadState lastGamePadState;
        public MouseState lastMouseState;
    }

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

        private Model pointLightMesh;
        private Effect pointLightMeshEffect;
        private Matrix lightMeshWorld;

        GameCamera camera;
        static public bool invertYAxis;

        Player player;
        List<Enemy> enemies;

        private EnemyStats ENEMY_STATS;

        private List<Light> lights;
        private List<Light> explosionLights;
        private Light relicLight;
        private bool relicLightOn = false;
        public static int MAX_LIGHTS = 8;

        public static Vector3 FIRE_COLOR = new Vector3(0.87f, 0.2f, 0.0f);
        public static Vector3 ICE_COLOR = new Vector3(0.2f, 0.8f, 1.0f);

        private static float EXPLOSION_INCR = 1.0f / 40.0f;

        /// <summary>
        /// Stores the last keyboard state and gamepad state.
        /// </summary>
        //KeyboardState currentKeyboardState;
        //GamePadState currentGamePadState;
        //MouseState currentMouseState;
        //KeyboardState lastKeyboardState;
        //GamePadState lastgamepadState;
        //MouseState lastMouseState;
        ControlState inputControlState;

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

            this.ENEMY_STATS.maxSpeed = 2.0f;
            this.ENEMY_STATS.attackDistance = 50f;
            this.ENEMY_STATS.smartChaseDistance = 2000f;
            this.ENEMY_STATS.dumbChaseDistance = 500f;
            this.ENEMY_STATS.hysteresis = 15f;

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
            explosionLights = new List<Light>();
            relicLight = new Light();
            Light newLight = new Light();
            newLight.color = new Vector3(1, 1, 1);
            newLight.position = new Vector3(0, 100, 0);
            newLight.attenuationRadius = 1000.0f;
            lights.Add(newLight);
            lightMeshWorld = Matrix.Identity;
            inputControlState = new ControlState();
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

            pointLightMeshEffect = content.Load<Effect>("PointLightMesh");
            pointLightMesh = content.Load<Model>("SphereLowPoly");

            player = new Player(graphics, content);
            camera = new GameCamera(graphics, ref player);
            player.InitCamera(ref camera);

            player.LoadContent();

            //terrain = new StaticGeometry(graphics.GraphicsDevice, "Cave1.obj", "cave1_collision.obj", Vector3.Zero, ref content);
            firstLevel = new Level(graphics.GraphicsDevice, ref content, "first_level.txt");
            firstLevel.Load(graphics.GraphicsDevice, ref content);

            // has to be done after level load because data structure isn't filled yet
            enemies = new List<Enemy>();
            enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS));

            foreach (Enemy e in enemies)
            {
                e.LoadContent(ref player, ref firstLevel);
            }

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this.ScreenManager.game, content, false);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this.ScreenManager.game, content, false);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this.ScreenManager.game, content, false);
            smokePlumeParticles = new SmokePlumeParticleSystem(this.ScreenManager.game, content, false);
            fireParticles = new FireParticleSystem(this.ScreenManager.game, content, true);

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

            // Register the particle system components.
            this.ScreenManager.game.Components.Add(explosionParticles);
            this.ScreenManager.game.Components.Add(explosionSmokeParticles);
            this.ScreenManager.game.Components.Add(projectileTrailParticles);
            this.ScreenManager.game.Components.Add(smokePlumeParticles);
            this.ScreenManager.game.Components.Add(fireParticles);

            // initialize enemy and player... figure out what polygons in the navigatin mesh they're in
            player.current_poly_index = this.firstLevel.NavigationIndex(player.position);
            player.prev_poly_index = player.current_poly_index;

            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].current_poly_index = this.firstLevel.NavigationIndex(enemies[i].position);
                enemies[i].prev_poly_index = enemies[i].current_poly_index;
            }

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

            if (IsActive)
            {
                // Get states for keys and pad
                inputControlState.currentKeyboardState = Keyboard.GetState();
                inputControlState.currentGamePadState = GamePad.GetState(PlayerIndex.One);
                inputControlState.currentMouseState = Mouse.GetState();

                // Allows the game to exit
                //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                // this.Exit();

                if (inputControlState.currentGamePadState.Buttons.Y == ButtonState.Pressed && inputControlState.lastGamePadState.Buttons.Y == ButtonState.Released)
                {
                    invertYAxis = !invertYAxis;
                }

                player.Update(gameTime, inputControlState, ref this.firstLevel);
                //lights[0].setPosition(new Vector3(player.position.X, player.position.Y + 100, player.position.Z));
                camera.UpdateCamera(gameTime, inputControlState, invertYAxis);
                //lights[0] = new Light(player.position + new Vector3(0,50,0), new Vector3(1,1,1));

                foreach (Enemy e in enemies)
                {
                    e.Update(gameTime, ref this.firstLevel, ref player);
                }

                for (int i = 0; i < explosionLights.Count; i++)
                {
                    explosionLights[i].currentExplosionTick += GameplayScreen.EXPLOSION_INCR;
                }

                explosionLights.RemoveAll(explosionLightHasExpired);

                UpdateAttacks(gameTime, inputControlState);
                UpdateProjectiles(gameTime);

                // Save previous states
                inputControlState.lastKeyboardState = inputControlState.currentKeyboardState;
                inputControlState.lastGamePadState = inputControlState.currentGamePadState;
                inputControlState.lastMouseState = inputControlState.currentMouseState;
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        private static bool explosionLightHasExpired(Light light)
        {
            return (light.currentExplosionTick > GameplayScreen.EXPLOSION_INCR * 40.0f);
        }

        /// <summary>
        /// Helper for updating the explosions effect.
        /// </summary>
        void UpdateAttacks(GameTime gameTime, ControlState inputState)
        {
            if ( (inputState.currentGamePadState.Buttons.B == ButtonState.Pressed && inputState.lastGamePadState.Buttons.B == ButtonState.Released) ||
                 (inputState.currentMouseState.LeftButton == ButtonState.Pressed && inputState.lastMouseState.LeftButton == ButtonState.Released) )
            {
                relicLight.attenuationRadius = 3000.0f;
                relicLight.color = GameplayScreen.FIRE_COLOR * 2.0f;
                relicLight.currentExplosionTick = 0.0f;
                Vector3 pos = player.position + new Vector3(0, 20, 0);
                pos += camera.right * 5;
                pos += camera.lookAt * 20;
                relicLight.position = pos;
                this.relicLightOn = true;
            }
            else if ( (inputState.currentGamePadState.Buttons.B == ButtonState.Pressed && inputState.lastGamePadState.Buttons.B == ButtonState.Pressed) ||
                      (inputState.currentMouseState.LeftButton == ButtonState.Pressed && inputState.lastMouseState.LeftButton == ButtonState.Pressed) )
            {
                Vector3 pos = player.position + new Vector3(0, 20, 0);
                if (player.velocity.X != 0 || player.velocity.Z != 0)
                {
                    pos += camera.right * 4;
                    pos += camera.lookAt * 22;
                    relicLight.position = pos;
                    // set the world matrix for the particles
                    Matrix world = Matrix.Identity;
                    world.Translation = player.position;
                    fireParticles.SetWorldMatrix(world);
                    for (int i = 0; i < 3; i++)
                    {
                        fireParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
                else
                {
                    pos += camera.right * 4;
                    pos += camera.lookAt * 20;
                    relicLight.position = pos;
                    fireParticles.SetWorldMatrix(player.worldTransform);
                    for (int i = 0; i < 3; i++)
                    {
                        fireParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
            }
            if ( (inputState.currentGamePadState.Buttons.B == ButtonState.Released && inputState.lastGamePadState.Buttons.B == ButtonState.Pressed) ||
                 (inputState.currentMouseState.LeftButton == ButtonState.Released && inputState.lastMouseState.LeftButton == ButtonState.Pressed) )
            {
                this.relicLightOn = false;

                Vector3 pos = player.position + new Vector3(0, 20, 0);
                pos += camera.right * 5;
                pos += camera.lookAt * 20;
                projectiles.Add(new Attack(pos, camera.lookAt * 400f, 100, 30, 20, 5f, 0, explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles));
                projectiles[projectiles.Count - 1].is_released = true;
            }
        }

        void UpdateFire()
        {
            //fireParticles
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
                    explosionLights.Add(new Light(projectiles[i].Position, GameplayScreen.FIRE_COLOR * 5.5f, 3000.0f, 0.0f));
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
               ScreenManager.AddScreen(new PauseMenuScreen(this.ScreenManager), ControllingPlayer);
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
            List<Light> projLightList = new List<Light>();
            projLightList.AddRange(lights);
            if (this.relicLightOn)
            {
                projLightList.Add(this.relicLight);
            }
            projLightList.AddRange(explosionLights);
            foreach (Projectile projectile in projectiles)
            {
                projLightList.Add(projectile.light);
            }

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
            player.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref projLightList);
            foreach (Enemy e in enemies)
            {
                e.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref projLightList);
            }

            //terrain.Draw(graphics.GraphicsDevice, true, ref camera);
            firstLevel.Draw(graphics.GraphicsDevice, ref camera, false, true, ref projLightList);

            //Draw lights
            DrawLights();

            base.Draw(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }

        /// <summary>
        /// This simple draw function is used to draw the on-screen
        /// representation of the lights affecting the meshes in the scene.
        /// </summary>
        public void DrawLights()
        {
            ModelMesh mesh = pointLightMesh.Meshes[0];
            ModelMeshPart meshPart = mesh.MeshParts[0];

            graphics.GraphicsDevice.Vertices[0].SetSource(
                mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
            graphics.GraphicsDevice.VertexDeclaration = meshPart.VertexDeclaration;
            graphics.GraphicsDevice.Indices = mesh.IndexBuffer;


            pointLightMeshEffect.Begin(SaveStateMode.None);
            pointLightMeshEffect.CurrentTechnique.Passes[0].Begin();


            for (int i = 0; i < lights.Count; i++)
            {
                lightMeshWorld.M41 = lights[i].position.X;
                lightMeshWorld.M42 = lights[i].position.Y;
                lightMeshWorld.M43 = lights[i].position.Z;

                pointLightMeshEffect.Parameters["world"].SetValue(lightMeshWorld);
                pointLightMeshEffect.Parameters["view"].SetValue(camera.GetViewMatrix());
                pointLightMeshEffect.Parameters["projection"].SetValue(camera.GetProjectionMatrix());
                pointLightMeshEffect.Parameters["lightColor"].SetValue(
                   new Vector4(lights[i].color, 1f));
                pointLightMeshEffect.CommitChanges();

                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, meshPart.BaseVertex, 0,
                    meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);

            }
            pointLightMeshEffect.CurrentTechnique.Passes[0].End();
            pointLightMeshEffect.End();
        }

        #endregion
    }
}
