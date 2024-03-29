#region Using Statements
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
#endregion

namespace WorldTest
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class WorldTest : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        #region Properties

        StaticGeometry terrain;

        Texture2D terrainTexture;

        GameCamera camera;
        private bool invertYAxis;

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

        #region Constructor
        /// <summary>
        /// Constructor for WorldTest Game
        /// </summary>
        public WorldTest()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferMultiSampling = true;

            Content.RootDirectory = "Content";
            invertYAxis = true;

            this.IsFixedTimeStep = false;
        }
        #endregion

        #region Initialize

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            lights = new List<Light>();
            lights.Add(new Light(new Vector3(100,100,100), new Vector3(1,1,1)));
            base.Initialize();
        }
        #endregion

        #region Load

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            GraphicsDevice device = graphics.GraphicsDevice;

            player = new Player(graphics, Content);
            camera = new GameCamera(graphics, ref player);
            player.InitCamera(ref camera);

            player.LoadContent();

            enemies = new List<Enemy>();
            enemies.Add(new Enemy(graphics, Content, "enemy_bind_pose"));

            foreach (Enemy e in enemies)
            {
                e.LoadContent();
            }

            // Load Cel Shader
            cel_effect = Content.Load<Effect>("CelShade");
            m_celMap = Content.Load<Texture2D>("Toon");

            terrain = new StaticGeometry(graphics.GraphicsDevice, "Cave1.obj", "cave1_collision.obj", Vector3.Zero);
            //collision_mesh = new StaticGeometry(graphics.GraphicsDevice, "cave1_collision.obj", "", Vector3.Zero);

            this.terrainTexture = Content.Load<Texture2D>("tex");

            //Set up RenderTargets
            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            sceneRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);

            shadowRenderTarget = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        #endregion

        #region Update

        private float deltaFPSTime = 0;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            float fps = 1 / elapsed;
            deltaFPSTime += elapsed;
            if (deltaFPSTime > 1)
            {
                Window.Title = "FPS: " + fps.ToString();
                deltaFPSTime -= 1;
            }

            // Get states for keys and pad
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

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

            base.Update(gameTime);
        }
        #endregion

        #region Draw

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
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
            this.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
            
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

                this.terrain.Draw(this.GraphicsDevice, true);
                //this.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;
                //this.collision_mesh.Draw(this.GraphicsDevice);
                //this.GraphicsDevice.RenderState.FillMode = FillMode.Solid;

                pass.End();
            }
            
            this.cel_effect.End();
            this.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
             
            base.Draw(gameTime);
        }

        #endregion
    }
}
