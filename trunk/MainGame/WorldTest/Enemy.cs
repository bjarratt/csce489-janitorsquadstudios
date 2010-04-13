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
    #region Enums and enemy state info

    //enum EnemyAiState
    //{
    //    Chasing, //chasing the player
    //    Attack,  //has caught the player and can stop chasing
    //    Idle,    //enemy can't see the player and wanders
    //    Weakened //the enemy can be banished to the other dimension
    //}

    struct EnemyStats
    {
        public float chaseDistance;
        public float attackDistance;
        public float hysteresis;
        public float maxSpeed;
    }

    #endregion

    class Enemy : Agent
    {
        #region Properties

        public enum EnemyAiState
        {
            ChasingSmart, //chasing the player with A*
            ChasingDumb, //chasing the player when close
            Attack,  //has caught the player and can stop chasing
            Idle,    //enemy can't see the player and wanders
            Weakened //the enemy can be banished to the other dimension
        }

        public EnemyStats stats;
        public EnemyAiState state;

        private int first_path_poly;
        private int second_path_poly;

        private Path<NavMeshNode> currentPath;

        public int FirstPathPoly
        {
            get { return first_path_poly; }
            set { first_path_poly = value; }
        }

        public int SecondPathPoly
        {
            get { return second_path_poly; }
            set { second_path_poly = value; }
        }

        public Path<NavMeshNode> CurrentPath
        {
            get { return currentPath; }
            set { currentPath = value; }
        }

        #endregion

        #region Constructor

        public Enemy(GraphicsDeviceManager Graphics, ContentManager Content, string enemy_name, EnemyStats stats) : base(Graphics, Content, enemy_name)
        {
            position = new Vector3(0, 100, -100);
            speed = 0.0f;

            this.stats = stats;
            this.state = EnemyAiState.Idle;

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
            //controller.StartClip(
            //    model.AnimationClips.Values[activeAnimationClip]);
            controller.PlayClip(model.AnimationClips.Values[0]);
        }

        public override void LoadContent()
        {
            max_textures = 3;
            max_targets = 3;
            textures = new Texture2D[max_textures];
            render_targets = new RenderTarget2D[max_targets];

            shader = content.Load<Effect>("CelShade");
            textures.SetValue(content.Load<Texture2D>("ColorMap"), (int)Tex_Select.model);
            textures.SetValue(content.Load<Texture2D>("Toon2"), (int)Tex_Select.cel_tex);

            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            render_targets[(int)Target_Select.normalDepth] = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);

            LoadSkinnedModel();
        }

        #endregion

        #region Update

        public void Update(GameTime gameTime, ref Level currentLevel, ref Player player)
        {
            //reset rotation
            rotation = 0.0f;

            //turn speed is same even if machine running slow
            turn_speed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            turn_speed *= turn_speed_reg;

            MoveForward(ref position, Quaternion.Identity, 0.0f, Vector4.Zero, ref currentLevel);
            worldTransform.Translation = position;

            //if (currentGPState.Buttons.LeftShoulder == ButtonState.Pressed && lastGPState.Buttons.LeftShoulder == ButtonState.Released)
            //{
            //    controller.CrossFade(model.AnimationClips["Idle"], TimeSpan.FromMilliseconds(300));
            //    controller.Speed = 1.0f;
            //}
            //else if (currentGPState.Buttons.RightShoulder == ButtonState.Pressed && lastGPState.Buttons.RightShoulder == ButtonState.Released)
            //{
            //    controller.CrossFade(model.AnimationClips["Walk"], TimeSpan.FromMilliseconds(300));
            //    controller.Speed = 3.0f;
            //}

            #region Behavior

            // First we have to use the current state to decide what the thresholds are
            // for changing state
            float enemyChaseThreshold = this.stats.chaseDistance;
            float enemyAttackThreshold = this.stats.attackDistance;

            // if the enemy is idle, he prefers to stay idle. we do this by making the
            // chase distance smaller, so the enemy will be less likely to begin chasing
            // the player.
            if (this.state == EnemyAiState.Idle)
            {
                enemyChaseThreshold -= this.stats.hysteresis / 2;
            }

            // similarly, if the enemy is active, he prefers to stay active. we
            // accomplish this by increasing the range of values that will cause the
            // enemy to go into the active state.
            else if (this.state == EnemyAiState.ChasingSmart || this.state == EnemyAiState.ChasingDumb)
            {
                enemyChaseThreshold += this.stats.hysteresis / 2;
                enemyAttackThreshold -= this.stats.hysteresis / 2;
            }
            // the same logic is applied to the finished state.
            else if (this.state == EnemyAiState.Attack)
            {
                enemyAttackThreshold += this.stats.hysteresis / 2;
            }

            // Second, now that we know what the thresholds are, we compare the enemy's 
            // distance from the player against the thresholds to decide what the enemy's
            // current state is.
            float distanceFromPlayer = Vector3.Distance(this.position, player.position);
            if (distanceFromPlayer > enemyChaseThreshold)
            {
                // if the enemy is far away from the player, it should idle
                this.state = EnemyAiState.Idle;
            }
            else if (distanceFromPlayer > enemyAttackThreshold)
            {
                this.state = EnemyAiState.ChasingSmart;
            }
            else
            {
                this.state = EnemyAiState.Attack;
            }

            // Third, once we know what state we're in, act on that state.
            float currentEnemySpeed;
            if (this.state == EnemyAiState.ChasingSmart)
            {
                // the enemy wants to chase the player, so it will just use the TurnToFace
                // function to turn towards the player's position. Then, when the enemy
                // moves forward, he will chase the player.
                this.rotation = TurnToFace(this.position, player.position, this.rotation, this.turn_speed);
                currentEnemySpeed = this.stats.maxSpeed;
            }
            else if (this.state == EnemyAiState.Idle)
            {
                // call the wander function for the enemy
                Wander(this.position, ref this.velocity, ref this.rotation,
                    this.turn_speed);
                currentEnemySpeed = .25f * this.speed;
                currentEnemySpeed = 0.0f;
            }
            else
            {
                // if the enemy catches the player, it should stop.
                // Otherwise it will run right by, then spin around and
                // try to catch it all over again. The end result is that it will kind
                // of "run laps" around the player, which looks funny, but is not what
                // we're after.
                currentEnemySpeed = 0.0f;
            }

            // this calculation is also important; we construct a heading
            // vector based on the enemy's orientation, and then make the enemy move along
            // that heading.
            Vector3 heading = new Vector3(
                (float)Math.Cos(this.rotation), 0, (float)Math.Sin(this.rotation));
            this.position += heading * currentEnemySpeed;

            #endregion

            // Update the animation according to the elapsed time
            controller.Update(gameTime.ElapsedGameTime, Matrix.Identity);

        }

        //TODO: update to include results of AI pathfinding
        private void MoveForward(ref Vector3 position, Quaternion rotationQuat, float speed, Vector4 stick, ref Level currentLevel)
        {

            position = currentLevel.CollideWith(position, new Vector3(0, -1, 0), 0.1, Level.MAX_COLLISIONS);
        }

        #region AI

        /// <summary>
        /// Calculates the angle that an object should face, given its position, its
        /// target's position, its current angle, and its maximum turning speed.
        /// </summary>
        private float TurnToFace(Vector3 position, Vector3 faceThis,
            float currentAngle, float turnSpeed)
        {
            //x and z directions along the 2D floor plane
            float x = faceThis.X - this.position.Z;
            float z = faceThis.Z - this.position.Z;

            float desiredAngle = (float)Math.Atan2(z, x);

            // first, figure out how much we want to turn, using WrapAngle to get our
            // result from -Pi to Pi ( -180 degrees to 180 degrees )
            float difference = WrapAngle(desiredAngle - this.rotation);

            // clamp that between -turn_speed and turn_speed.
            difference = MathHelper.Clamp(difference, -this.turn_speed, this.turn_speed);

            // so, the closest we can get to our target is currentAngle + difference.
            // return that, using WrapAngle again.
            return this.WrapAngle(currentAngle + difference);
        }

        /// <summary>
        /// Returns the angle expressed in radians between -Pi and Pi.
        /// <param name="radians">the angle to wrap, in radians.</param>
        /// <returns>the input value expressed in radians from -Pi to Pi.</returns>
        /// </summary>
        private float WrapAngle(float radians)
        {
            while (radians < -MathHelper.Pi)
            {
                radians += MathHelper.TwoPi;
            }
            while (radians > MathHelper.Pi)
            {
                radians -= MathHelper.TwoPi;
            }
            return radians;
        }

        /// <summary>
        /// Wander contains functionality for the enemy, and does just what its name implies: 
        /// makes them wander around the screen. 
        private void Wander(Vector3 position, ref Vector3 wanderDirection,
            ref float orientation, float turnSpeed)
        {
            Random random = new Random();

            wanderDirection.X +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());
            wanderDirection.Z +=
                MathHelper.Lerp(-.25f, .25f, (float)random.NextDouble());

            // we'll renormalize the wander direction, ...
            if (wanderDirection != Vector3.Zero)
            {
                wanderDirection.Normalize();
            }
            orientation = TurnToFace(position, position + wanderDirection, orientation,
                .15f * turnSpeed);

            // next, we'll turn the enemy back towards the center of the screen, to
            // prevent them from getting stuck on the edges of the screen.
            Vector3 screenCenter = Vector3.Zero;
            screenCenter.X = graphics.GraphicsDevice.Viewport.Width / 2;
            screenCenter.Z = graphics.GraphicsDevice.Viewport.Height / 2;

            float distanceFromScreenCenter = Vector3.Distance(screenCenter, position);
            float MaxDistanceFromScreenCenter =
                Math.Min(screenCenter.Z, screenCenter.X);

            float normalizedDistance =
                distanceFromScreenCenter / MaxDistanceFromScreenCenter;

            float turnToCenterSpeed = .3f * normalizedDistance * normalizedDistance *
                turnSpeed;

            // once we've calculated how much we want to turn towards the center, we can
            // use the TurnToFace function to actually do the work.
            orientation = TurnToFace(position, screenCenter, orientation,
                turnToCenterSpeed);
        }

        #endregion

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
                    effect.Parameters["ambientLightColor"].SetValue(new Vector3(0.01f));
                    effect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3(1.0f));
                    effect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.3f));
                    effect.Parameters["material"].StructureMembers["specularPower"].SetValue(10);
                    effect.Parameters["diffuseMapEnabled"].SetValue(true);
                    for (int i = 0; i < Lights.Count; i++)
                    {
                        if ((i + 1) > GameplayScreen.MAX_LIGHTS)
                        {
                            break;
                        }
                        effect.Parameters["lights"].Elements[i].StructureMembers["color"].SetValue(Lights[i].color * (1 - Lights[i].currentExplosionTick));
                        effect.Parameters["lights"].Elements[i].StructureMembers["position"].SetValue(Lights[i].position);
                        effect.Parameters["lightRadii"].Elements[i].SetValue(Lights[i].attenuationRadius);
                    }
                    switch (Lights.Count)
                    {
                        case 1: effect.CurrentTechnique = effect.Techniques["AnimatedModel_OneLight"];
                            break;
                        case 2: effect.CurrentTechnique = effect.Techniques["AnimatedModel_TwoLight"];
                            break;
                        case 3: effect.CurrentTechnique = effect.Techniques["AnimatedModel_ThreeLight"];
                            break;
                        case 4: effect.CurrentTechnique = effect.Techniques["AnimatedModel_FourLight"];
                            break;
                        case 5: effect.CurrentTechnique = effect.Techniques["AnimatedModel_FiveLight"];
                            break;
                        case 6: effect.CurrentTechnique = effect.Techniques["AnimatedModel_SixLight"];
                            break;
                        case 7: effect.CurrentTechnique = effect.Techniques["AnimatedModel_SevenLight"];
                            break;
                        case 8: effect.CurrentTechnique = effect.Techniques["AnimatedModel_EightLight"];
                            break;
                        default: effect.CurrentTechnique = effect.Techniques["AnimatedModel_EightLight"];
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
