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
using System.IO;
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

        private string loadFilename;

        Level firstLevel;

        public static Sound soundControl = new Sound();

        private Model pointLightMesh;
        private Model enemySphereMesh;
        private Model hand;
        private Texture2D handDiffuse;
        private Texture2D handCel;
        private Effect handEffect;
        private Effect pointLightMeshEffect;
        private Matrix lightMeshWorld;


        GameCamera camera;
        static public bool invertYAxis;

        Player player;
        List<Enemy> enemies;

        //damage object and TODO associated fade values
        Damage blood;

        private EnemyStats ENEMY_STATS;

        private List<Light> lights;
        private List<Light> explosionLights;
        private Light relicLight;
        private bool relicLightOn = false;
        public static int MAX_LIGHTS = 8;

        public static Vector3 FIRE_COLOR = new Vector3(0.87f, 0.2f, 0.0f);
        public static Vector3 ACID_FIRE = new Vector3(0, 0.7f, 0);
        public static Vector3 ICE_COLOR = new Vector3(0.0f, 0.6f, 0.6f);

        private static float EXPLOSION_INCR = 1.0f / 40.0f;

        public const float MAX_TRANSITION_RADIUS = 6000.0f;
        public const float WAVE_FRONT_SIZE = 50.0f;
        public const float TRANSITION_SPEED = 1000f;
        public static float transitionRadius = MAX_TRANSITION_RADIUS + 1.0f;
        public static bool transitioning = false;

        /// <summary>
        /// Stores the last keyboard, mouse and gamepad state.
        /// </summary>
        ControlState inputControlState;

        /// <summary>
        /// Particle Effects
        /// </summary>
        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleSystem projectileTrailParticles;
        ParticleSystem smokePlumeParticles;
        ParticleSystem fireParticles;
        ParticleSystem banishingParticleProj;
        ParticleSystem banishingHandParticles;
        ParticleSystem banisherExplosions;

        /// <summary>
        /// List of attacks
        /// </summary>
        List<Projectile> projectiles = new List<Projectile>();

        // Random number generator for the fire effect.
        Random random = new Random();

        /// <summary>
        /// Render targets for the different shaders... 
        /// We do this because the outline shader needs the Normal/Depth
        /// texture as a separate entity to do its work.
        /// </summary>
        RenderTarget2D sceneRenderTarget;
        RenderTarget2D shadowRenderTarget;

        Portal portal;

        ToolTips tips;

        // Hud
        Reticle fireReticle;
        Reticle banishReticle;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen(ScreenManager sm, string loadFilename)
        {
            this.ScreenManager = sm;
            //ScreenManager.Game.Content.RootDirectory = "Content";
            this.graphics = sm.graphics;

            //set enemy stats
            this.ENEMY_STATS.maxSpeed = 10.0f;
            this.ENEMY_STATS.attackDistance = 200f;
            this.ENEMY_STATS.smartChaseDistance = 4000f;
            this.ENEMY_STATS.dumbChaseDistance = 500f;
            this.ENEMY_STATS.hysteresis = 15f;
            this.ENEMY_STATS.recoveryTime = 7f;
            this.ENEMY_STATS.maxHealth = 100;

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            this.loadFilename = loadFilename;

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
            newLight.attenuationRadius = 10000.0f;
            lights.Add(newLight);
            lightMeshWorld = Matrix.Identity;
            inputControlState = new ControlState();
            //init damage object
            blood = new Damage(ref this.ScreenManager.game);
            fireReticle = new Reticle(this.graphics.GraphicsDevice, 12.0f, 20.0f, new Vector4(GameplayScreen.FIRE_COLOR, 1.0f), 50);
            banishReticle = new Reticle(this.graphics.GraphicsDevice, 18.0f, 26.0f, new Vector4(GameplayScreen.ICE_COLOR, 1.0f), 80);
        }

        #endregion

        #region Load

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            gameFont = content.Load<SpriteFont>("gamefont");

            GameplayScreen.soundControl.PlayMusic("cave game first area");

            //main game screen content
            // Create a new SpriteBatch, which can be used to draw textures.

            GraphicsDevice device = graphics.GraphicsDevice;

            pointLightMeshEffect = content.Load<Effect>("PointLightMesh");
            pointLightMesh = content.Load<Model>("SphereLowPoly");
            enemySphereMesh = content.Load<Model>("SphereLowPoly");
            hand = content.Load<Model>("player_hand");
            handEffect = content.Load<Effect>("Render2D");
            handDiffuse = content.Load<Texture2D>("ColorMap");
            handCel = content.Load<Texture2D>("Toon2");

            player = new Player(graphics, content);
            camera = new GameCamera(graphics, ref player);
            player.InitCamera(ref camera);

            player.LoadContent();

            blood.LoadContent(content);

            //terrain = new StaticGeometry(graphics.GraphicsDevice, "Cave1.obj", "cave1_collision.obj", Vector3.Zero, ref content);
            firstLevel = new Level(graphics.GraphicsDevice, ref content, "second_level.txt");
            firstLevel.Load(graphics.GraphicsDevice, ref content);

            // has to be done after level load because data structure isn't filled yet
            enemies = new List<Enemy>();
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(0,0,0),Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(0, 0, 0),Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(1500, 50, 0),Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(-1000, 50, 0), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(1000, 50, 0), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(-1000, 50, 100), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(-1000, 50, -100), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(1000, 50, 100), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(1000, 50, -100), Dimension.FIRST));
            //enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, new Vector3(-1000, 50, 0), Dimension.FIRST));

            this.LoadGame(this.loadFilename);

            //foreach (Enemy e in enemies)
            //{
            //    e.LoadContent(ref player, ref firstLevel);
            //}

            // Construct our particle system components.
            explosionParticles = new ExplosionParticleSystem(this.ScreenManager.game, content, false);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this.ScreenManager.game, content, false);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this.ScreenManager.game, content, false);
            smokePlumeParticles = new SmokePlumeParticleSystem(this.ScreenManager.game, content, false);
            fireParticles = new FireParticleSystem(this.ScreenManager.game, content, true);
            banishingParticleProj = new BanishingParticleSystem(this.ScreenManager.game, content, false);
            banishingHandParticles = new BanishingHandSystem(this.ScreenManager.game, content, false);
            banisherExplosions = new BanisherExplosion(this.ScreenManager.game, content, false);

            // Set the draw order so the explosions and fire
            // will appear over the top of the smoke.
            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            banishingParticleProj.DrawOrder = 350;
            banishingHandParticles.DrawOrder = 375;
            banisherExplosions.DrawOrder = 380;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;

            portal = new Portal(new Vector3(0,-330,0), 50f);
            portal.Load(this.ScreenManager.game, content);

            tips = new ToolTips();
            tips.LoadContent(graphics.GraphicsDevice, content);

            //Set up RenderTargets
            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            sceneRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);

            shadowRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);
            //player and HUD content

            fireReticle.LoadContent(ref content);
            banishReticle.LoadContent(ref content);

            // Start the sound!

            // Register the particle system components.
            this.ScreenManager.game.Components.Add(explosionParticles);
            this.ScreenManager.game.Components.Add(explosionSmokeParticles);
            this.ScreenManager.game.Components.Add(projectileTrailParticles);
            this.ScreenManager.game.Components.Add(smokePlumeParticles);
            this.ScreenManager.game.Components.Add(fireParticles);
            this.ScreenManager.game.Components.Add(banishingParticleProj);
            this.ScreenManager.game.Components.Add(banishingHandParticles);
            this.ScreenManager.game.Components.Add(banisherExplosions);

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

        #region Save and Load Game

        public void SaveGame()
        {
            StreamWriter writer = new StreamWriter("save1.txt");

            // Format: Camera <lookAt.x> <.y> <.z> <right.x> <.y> <.z> <up.x> <.y> <.z>
            writer.Write("Camera ");
            writer.Write(camera.lookAt.X.ToString() + " ");
            writer.Write(camera.lookAt.Y.ToString() + " ");
            writer.Write(camera.lookAt.Z.ToString() + " ");
            writer.Write(camera.right.X.ToString() + " ");
            writer.Write(camera.right.Y.ToString() + " ");
            writer.Write(camera.right.Z.ToString() + " ");
            writer.Write(camera.up.X.ToString() + " ");
            writer.Write(camera.up.Y.ToString() + " ");
            writer.WriteLine(camera.up.Z.ToString());

            // Format: Player <dimension> <health> <position.x> <.y> <.z> <orientation.x> <.y> <.z> <.w>
            writer.Write("Player ");
            if (player.CurrentDimension == Dimension.FIRST)
            {
                writer.Write("1 ");
            }
            else
            {
                writer.Write("2 ");
            }
            writer.Write(player.health.ToString() + " ");
            writer.Write(player.position.X.ToString() + " ");
            writer.Write(player.position.Y.ToString() + " ");
            writer.Write(player.position.Z.ToString() + " ");
            writer.Write(player.orientation.X.ToString() + " ");
            writer.Write(player.orientation.Y.ToString() + " ");
            writer.Write(player.orientation.Z.ToString() + " ");
            writer.WriteLine(player.orientation.W.ToString());
            
            // Format: Enemy <dimension> <current_health> <max_health> <position.x> <.y> <.z>
            for (int i = 0; i < enemies.Count; i++)
            {
                writer.Write("Enemy ");
                if (enemies[i].CurrentDimension == Dimension.FIRST)
                {
                    writer.Write("1 ");
                }
                else
                {
                    writer.Write("2 ");
                }
                writer.Write(enemies[i].health.ToString() + " ");
                writer.Write(enemies[i].MaxHealth.ToString() + " ");
                writer.Write(enemies[i].position.X.ToString() + " ");
                writer.Write(enemies[i].position.Y.ToString() + " ");
                writer.WriteLine(enemies[i].position.Z.ToString());
            }

            writer.Close();
        }

        public void LoadGame(string filename)
        {
            StreamReader reader = new StreamReader(filename);

            string line = reader.ReadLine();
            string[] splitLine;
            char[] splitChars = { ' ' };

            enemies.Clear();

            while (line != null)
            {
                splitLine = line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                // Format: Player <dimension> <health> <position.x> <.y> <.z> <orientation.x> <.y> <.z> <.w>
                if (splitLine[0] == "Player")
                {
                    // Dimension
                    if (Convert.ToInt32(splitLine[1]) == 1)
                    {
                        player.ChangeDimension(Dimension.FIRST);
                    }
                    else
                    {
                        player.ChangeDimension(Dimension.SECOND);
                    }

                    // Health
                    player.health = Convert.ToInt32(splitLine[2]);

                    // Position
                    player.position.X = (float)Convert.ToDouble(splitLine[3]);
                    player.position.Y = (float)Convert.ToDouble(splitLine[4]);
                    player.position.Z = (float)Convert.ToDouble(splitLine[5]);

                    // Orientation
                    player.orientation.X = (float)Convert.ToDouble(splitLine[6]);
                    player.orientation.Y = (float)Convert.ToDouble(splitLine[7]);
                    player.orientation.Z = (float)Convert.ToDouble(splitLine[8]);
                    player.orientation.W = (float)Convert.ToDouble(splitLine[9]);
                }
                // Format: Enemy <dimension> <current_health> <max_health> <position.x> <.y> <.z>
                else if (splitLine[0] == "Enemy")
                {
                    // Dimension
                    Dimension enemyDimension;

                    if (Convert.ToInt32(splitLine[1]) == 1)
                    {
                        enemyDimension = Dimension.FIRST;
                    }
                    else
                    {
                        enemyDimension = Dimension.SECOND;
                    }

                    // Health
                    int enemyHealth = Convert.ToInt32(splitLine[2]);

                    // Max health
                    ENEMY_STATS.maxHealth = Convert.ToInt32(splitLine[3]);

                    // Position
                    Vector3 enemyPosition;
                    enemyPosition.X = (float)Convert.ToDouble(splitLine[4]);
                    enemyPosition.Y = (float)Convert.ToDouble(splitLine[5]);
                    enemyPosition.Z = (float)Convert.ToDouble(splitLine[6]);

                    enemies.Add(new Enemy(graphics, content, "enemy1_all_final", ENEMY_STATS, enemyPosition, enemyDimension));
                }
                // Format: Camera <lookAt.x> <.y> <.z> <right.x> <.y> <.z> <up.x> <.y> <.z>
                else if (splitLine[0] == "Camera")
                {
                    camera.lookAt.X = (float)Convert.ToDouble(splitLine[1]);
                    camera.lookAt.Y = (float)Convert.ToDouble(splitLine[2]);
                    camera.lookAt.Z = (float)Convert.ToDouble(splitLine[3]);
                    camera.right.X = (float)Convert.ToDouble(splitLine[4]);
                    camera.right.Y = (float)Convert.ToDouble(splitLine[5]);
                    camera.right.Z = (float)Convert.ToDouble(splitLine[6]);
                    camera.up.X = (float)Convert.ToDouble(splitLine[7]);
                    camera.up.Y = (float)Convert.ToDouble(splitLine[8]);
                    camera.up.Z = (float)Convert.ToDouble(splitLine[9]);
                }

                line = reader.ReadLine();
            }

            foreach (Enemy e in enemies)
            {
                e.LoadContent(ref player, ref firstLevel);
            }

            reader.Close();
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

                if (GameplayScreen.transitionRadius <= GameplayScreen.MAX_TRANSITION_RADIUS)
                {
                    GameplayScreen.transitionRadius += ((float)gameTime.ElapsedGameTime.TotalSeconds * GameplayScreen.TRANSITION_SPEED);
                }
                else
                {
                    GameplayScreen.transitioning = false;
                }

                if (inputControlState.currentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed &&
                    inputControlState.lastGamePadState.Buttons.LeftShoulder == ButtonState.Released)
                {
                    SaveGame();
                }

                if (inputControlState.currentGamePadState.Buttons.RightShoulder == ButtonState.Pressed &&
                    inputControlState.lastGamePadState.Buttons.RightShoulder == ButtonState.Released)
                {
                    LoadGame("save1.txt");
                }

                // Allows the game to exit
                //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                // this.Exit();

                //if (inputControlState.currentGamePadState.Buttons.Y == ButtonState.Pressed && inputControlState.lastGamePadState.Buttons.Y == ButtonState.Released)
                //{
                //    invertYAxis = !invertYAxis;
                //}

                player.Update(gameTime, inputControlState, ref this.firstLevel);
                //lights[0].setPosition(new Vector3(player.position.X, player.position.Y + 100, player.position.Z));
                camera.UpdateCamera(gameTime, inputControlState, invertYAxis);
                //lights[0] = new Light(player.position + new Vector3(0,50,0), new Vector3(1,1,1));
                blood.Update(gameTime, ref player);

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

                portal.Update(gameTime);
                tips.Update(gameTime, ref player, ref firstLevel);

                // Save previous states
                inputControlState.lastKeyboardState = inputControlState.currentKeyboardState;
                inputControlState.lastGamePadState = inputControlState.currentGamePadState;
                inputControlState.lastMouseState = inputControlState.currentMouseState;
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        private static bool explosionLightHasExpired(Light light)
        {
            return (light.currentExplosionTick > GameplayScreen.EXPLOSION_INCR * 20f);
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
                Vector3 pos = player.position + new Vector3(0, 105, 0);
                pos += camera.right * 5;
                pos += camera.lookAt * 20;
                relicLight.position = pos;
                this.relicLightOn = true;
                GameplayScreen.soundControl.Play("fireball_ignite");
            }
            else if ( (inputState.currentGamePadState.Buttons.B == ButtonState.Pressed && inputState.lastGamePadState.Buttons.B == ButtonState.Pressed) ||
                      (inputState.currentMouseState.LeftButton == ButtonState.Pressed && inputState.lastMouseState.LeftButton == ButtonState.Pressed) )
            {
                //GameplayScreen.soundControl.Play("fireball_held");
                Vector3 pos = player.position + new Vector3(0, 105, 0);
                if (player.velocity.X != 0 || player.velocity.Z != 0)
                {
                    pos += camera.right * 5;
                    pos += camera.lookAt * 22;
                    relicLight.position = pos;
                    // set the world matrix for the particles
                    //Matrix world = Matrix.CreateShadow(camera.lookAt, new Plane(-camera.lookAt.X, -camera.lookAt.Y, -camera.lookAt.Z, Vector3.Distance(player.position, Vector3.Zero)));
                    //fireParticles.SetWorldMatrix(world);
                    for (int i = 0; i < 3; i++)
                    {
                        fireParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
                else
                {
                    pos += camera.right * 5;
                    pos += camera.lookAt * 20;
                    relicLight.position = pos;
                    fireParticles.SetWorldMatrix(player.worldTransform);
                    for (int i = 0; i < 1; i++)
                    {
                        fireParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
            }
            if ( (inputState.currentGamePadState.Buttons.B == ButtonState.Released && inputState.lastGamePadState.Buttons.B == ButtonState.Pressed) ||
                 (inputState.currentMouseState.LeftButton == ButtonState.Released && inputState.lastMouseState.LeftButton == ButtonState.Pressed) )
            {
                if (!this.fireReticle.AnimationRunning)
                {
                    this.relicLightOn = false;
                    this.fireReticle.StartAnimation();
                    GameplayScreen.soundControl.Play("fireball_deploy");
                    Vector3 pos = player.position + new Vector3(0, 105, 0);
                    pos += camera.right * 5;
                    pos += camera.lookAt * 20;
                    projectiles.Add(new Attack(pos, camera.lookAt * 900f, 100, 30, 20, 5f, 0, explosionParticles,
                                                   explosionSmokeParticles,
                                                   projectileTrailParticles, ref enemies, false));
                    projectiles[projectiles.Count - 1].is_released = true;
                }
            }


            // Y BUTTON
            if ((inputState.currentGamePadState.Buttons.Y == ButtonState.Pressed && inputState.lastGamePadState.Buttons.Y == ButtonState.Released) ||
                 (inputState.currentMouseState.RightButton == ButtonState.Pressed && inputState.lastMouseState.RightButton == ButtonState.Released))
            {
                GameplayScreen.soundControl.Play("banish activated");
                relicLight.attenuationRadius = 3000.0f;
                relicLight.color = GameplayScreen.ICE_COLOR * 2.0f;
                relicLight.currentExplosionTick = 0.0f;
                Vector3 pos = player.position + new Vector3(0, 105, 0);
                pos += camera.right * 5;
                pos += camera.lookAt * 20;
                relicLight.position = pos;
                this.relicLightOn = true;
            }
            else if ((inputState.currentGamePadState.Buttons.Y == ButtonState.Pressed && inputState.lastGamePadState.Buttons.Y == ButtonState.Pressed) ||
                      (inputState.currentMouseState.RightButton == ButtonState.Pressed && inputState.lastMouseState.RightButton == ButtonState.Pressed))
            {
                Vector3 pos = player.position + new Vector3(0, 105, 0);
                if (player.velocity.X != 0 || player.velocity.Z != 0)
                {
                    pos += camera.right * 5;
                    pos += camera.lookAt * 22;
                    relicLight.position = pos;
                    // set the world matrix for the particles
                    //Matrix world = Matrix.CreateShadow(camera.lookAt, new Plane(-camera.lookAt.X, -camera.lookAt.Y, -camera.lookAt.Z, Vector3.Distance(player.position, Vector3.Zero)));
                    //fireParticles.SetWorldMatrix(world);
                    for (int i = 0; i < 3; i++)
                    {
                        banishingHandParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
                else
                {
                    pos += camera.right * 5;
                    pos += camera.lookAt * 20;
                    relicLight.position = pos;
                    for (int i = 0; i < 1; i++)
                    {
                        banishingHandParticles.AddParticle(pos, Vector3.Zero);
                    }
                }
            }
            if ((inputState.currentGamePadState.Buttons.Y == ButtonState.Released && inputState.lastGamePadState.Buttons.Y == ButtonState.Pressed) ||
                 (inputState.currentMouseState.RightButton == ButtonState.Released && inputState.lastMouseState.RightButton == ButtonState.Pressed))
            {
                if (!this.banishReticle.AnimationRunning)
                {
                    this.relicLightOn = false;
                    this.banishReticle.StartAnimation();
                GameplayScreen.soundControl.Play("banish deployed");
                    Vector3 pos = player.position + new Vector3(0, 105, 0);
                    pos += camera.right * 5;
                    pos += camera.lookAt * 20;
                    projectiles.Add(new Attack(pos, camera.lookAt * 900f, 100, 30, 20, 5f, 0, banisherExplosions,
                                                   banisherExplosions,
                                                   banishingParticleProj, ref enemies, true));
                    projectiles[projectiles.Count - 1].is_released = true;
                }
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
                if (!projectiles[i].Update(gameTime, ref firstLevel, ref enemies, player.CurrentDimension))
                {
                    // Remove projectiles at the end of their life.
                    if (projectiles[i].is_banisher)
                    {
                        explosionLights.Add(new Light(projectiles[i].Position, GameplayScreen.ICE_COLOR * 5.5f, 3000.0f, 0.0f));
                    }
                    else
                    {
                        explosionLights.Add(new Light(projectiles[i].Position, GameplayScreen.FIRE_COLOR * 5.5f, 3000.0f, 0.0f));
                    }
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

        public void DrawHud()
        {
            fireReticle.Draw(this.graphics.GraphicsDevice);
            banishReticle.Draw(this.graphics.GraphicsDevice);
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

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

            if (projLightList.Count % 2 != 0)
            {
                projLightList.Add(new Light(Vector3.Zero, Vector3.Zero, 1.0f));
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
            banishingParticleProj.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            portal.Draw(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix());
            banishingHandParticles.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());
            banisherExplosions.SetCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix());

            base.Draw(gameTime);

            //Cel Shading pass
            graphics.GraphicsDevice.Clear(Color.Black);

            //terrain.Draw(graphics.GraphicsDevice, true, ref camera);
            firstLevel.Draw(graphics.GraphicsDevice, ref camera, false, true, ref projLightList, player.CurrentDimension, player.position, ref spriteBatch, gameTime);

            //player.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref projLightList);
            foreach (Enemy e in enemies)
            {
                e.DrawCel(gameTime, camera.GetViewMatrix(), camera.GetProjectionMatrix(), ref sceneRenderTarget, ref shadowRenderTarget, ref projLightList, player.position, player.CurrentDimension);
                //DrawEnemySphere(e.collisionSphere.Radius, e.collisionSphere.Center);
            }

            //Draw lights
            DrawLights();
            //DrawHand();

            //draw all on-screen hud or damage indicators
            spriteBatch.Begin();

            //if (player.isHit)
            //{
                blood.Draw(spriteBatch, ref player, device.PresentationParameters);
            //}

            tips.Draw(spriteBatch, graphics.GraphicsDevice.PresentationParameters);

            if (player.health <= 0)
            {
                //ScreenManager.RemoveScreen(this);
                this.ScreenState = ScreenState.Hidden;
                soundBank.Dispose();
                soundControl.StopMusic("cave game first area");
                GameOver.Load(ScreenManager, null, new BackgroundScreen(), new MainMenuScreen(this.ScreenManager));
            }

            spriteBatch.End();

            this.DrawHud();

            //base.Draw(gameTime);

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
                pointLightMeshEffect.Parameters["lightColor"].SetValue(new Vector4(lights[i].color, 1f));
                pointLightMeshEffect.CommitChanges();

                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, meshPart.BaseVertex, 0,
                    meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);

            }
            pointLightMeshEffect.CurrentTechnique.Passes[0].End();
            pointLightMeshEffect.End();
        }

        public void DrawEnemySphere(float radius, Vector3 center)
        {
            ModelMesh mesh = this.enemySphereMesh.Meshes[0];
            ModelMeshPart meshPart = mesh.MeshParts[0];

            graphics.GraphicsDevice.Vertices[0].SetSource(
                mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
            graphics.GraphicsDevice.VertexDeclaration = meshPart.VertexDeclaration;
            graphics.GraphicsDevice.Indices = mesh.IndexBuffer;


            pointLightMeshEffect.Begin(SaveStateMode.None);
            pointLightMeshEffect.CurrentTechnique.Passes[0].Begin();

            //for (int i = 0; i < lights.Count; i++)
            //{
                //lightMeshWorld.M41 = lights[i].position.X;
                //lightMeshWorld.M42 = lights[i].position.Y;
                //lightMeshWorld.M43 = lights[i].position.Z;
            lightMeshWorld = Matrix.Multiply(Matrix.CreateScale(radius), Matrix.CreateTranslation(center));

                pointLightMeshEffect.Parameters["world"].SetValue(lightMeshWorld);
                pointLightMeshEffect.Parameters["view"].SetValue(camera.GetViewMatrix());
                pointLightMeshEffect.Parameters["projection"].SetValue(camera.GetProjectionMatrix());
                pointLightMeshEffect.Parameters["lightColor"].SetValue(
                   new Vector4(1,1,1, 0.5f));
                pointLightMeshEffect.CommitChanges();

                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, meshPart.BaseVertex, 0,
                    meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);

            //}
            pointLightMeshEffect.CurrentTechnique.Passes[0].End();
            pointLightMeshEffect.End();
        }

        public void DrawHand()
        {
            ModelMesh mesh = this.hand.Meshes[0];
            ModelMeshPart meshPart = mesh.MeshParts[0];

            graphics.GraphicsDevice.Vertices[0].SetSource(
                mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
            graphics.GraphicsDevice.VertexDeclaration = meshPart.VertexDeclaration;
            graphics.GraphicsDevice.Indices = mesh.IndexBuffer;

            Vector3 screenOffset = new Vector3((float)graphics.GraphicsDevice.Viewport.Width / 2.0f, (float)graphics.GraphicsDevice.Viewport.Height / 2.0f, -1.0f);
            Matrix projectionMatrix = Matrix.CreateOrthographicOffCenter(0,
                (float)graphics.GraphicsDevice.Viewport.Width,
                (float)graphics.GraphicsDevice.Viewport.Height,
                0, 1.0f, 10000.0f);

            Matrix worldMatrix = Matrix.CreateScale(0.05f);
            worldMatrix.Translation = new Vector3(screenOffset.X, screenOffset.Y, 0.0f);
            Matrix viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);
            handEffect.Parameters["World"].SetValue(worldMatrix);
            handEffect.Parameters["View"].SetValue(viewMatrix);
            handEffect.Parameters["Projection"].SetValue(projectionMatrix);
            handEffect.Parameters["reticleColor"].SetValue(new Vector4(1, 1, 1, 1));
            handEffect.Parameters["reticlePosition"].SetValue(screenOffset);
            //shader.Parameters["reticleInnerRadius"].SetValue(innerRadius);
            //shader.Parameters["reticleOuterRadius"].SetValue(outerRadius);

            handEffect.CurrentTechnique = handEffect.Techniques["Reticle"];

            handEffect.Begin(SaveStateMode.None);

            foreach (EffectPass pass in handEffect.CurrentTechnique.Passes)
            {
                //lightMeshWorld = Matrix.Multiply(Matrix.CreateScale(0.05f), Matrix.CreateTranslation(0, -650, 0));//Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(0,-550,0);

                //handEffect.Parameters["matW"].SetValue(lightMeshWorld);
                //handEffect.Parameters["matVP"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
                //handEffect.Parameters["matVI"].SetValue(Matrix.Invert(camera.GetViewMatrix()));
                ////handEffect.Parameters["shadowMap"].SetValue(shadowRenderTarget.GetTexture());
                //handEffect.Parameters["diffuseMap0"].SetValue(handDiffuse);
                //handEffect.Parameters["CelMap"].SetValue(handCel);
                //handEffect.Parameters["ambientLightColor"].SetValue(new Vector3(0.0f));
                //handEffect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3(1.0f));
                //handEffect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.1f));
                //handEffect.Parameters["material"].StructureMembers["specularPower"].SetValue(55);
                ////handEffect.Parameters["diffuseMapEnabled"].SetValue(true);
                //handEffect.Parameters["playerPosition"].SetValue(player.position);
                //handEffect.Parameters["transitionRadius"].SetValue(GameplayScreen.transitionRadius);
                //handEffect.Parameters["waveRadius"].SetValue(GameplayScreen.transitionRadius - GameplayScreen.WAVE_FRONT_SIZE);
                //handEffect.CommitChanges();

                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, meshPart.BaseVertex, 0,
                    meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
            }
            
            handEffect.End();
        }

        #endregion
    }
}
