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
    #region Enemy Stats

    struct EnemyStats
    {
        public float smartChaseDistance;
        public float dumbChaseDistance;
        public float attackDistance;
        public float hysteresis;
        public float maxSpeed;
        public float recoveryTime;
        public int maxHealth;
        public bool useLineOfSight;
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
            Weakened, //the enemy can be banished to the other dimension
            Airborne //the enemy is in the air and needs to stay still until it hits the ground
        }

        private EnemyStats stats;
        public EnemyAiState state;
        public EnemyAiState prevState;
        public bool beginChase = false;

        private List<KeyValuePair<EnemyAiState, string>> narrationCues;

        private float timeUntilRecovery = -1.0f;

        private int first_path_poly;
        private int second_path_poly;

        private float timeBetweenDamage = 0;

        private Vector3 lookAt = new Vector3(0, 0, 1);

        private LinkedList<NavMeshNode> currentPath;

        private Narration currentNarration = null;

        public float TimeUntilRecovery
        {
            get { return timeUntilRecovery; }
        }

        public int MaxHealth
        {
            get { return stats.maxHealth; }
        }

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

        public LinkedList<NavMeshNode> CurrentPath
        {
            get { return currentPath; }
            set { currentPath = value; }
        }

        public BoundingSphere collisionSphere;

        private bool lineOfSightTriggered = false;
        private bool inFinalRoom = false;

        public bool InFinalRoom
        {
            get { return inFinalRoom; }
        }

        #endregion

        #region Constructor

        public Enemy(GraphicsDeviceManager Graphics, ContentManager Content, string enemy_name,
                     EnemyStats stats, Vector3 position, Dimension currentDimension,
                     List<KeyValuePair<EnemyAiState, string>> narrationCues, bool useLineOfSight, bool inFinalRoom) : base(Graphics, Content, enemy_name)
        {
            this.position = position;
            speed = 10.0f;

            this.stats = stats;
            this.stats.useLineOfSight = useLineOfSight;
            this.state = EnemyAiState.Idle;

            rotation = 0.0f;
            turn_speed = 1.5f;
            turn_speed_reg = 2.6f;
            movement_speed_reg = 14.0f; // 14

            health = this.stats.maxHealth;

            orientation = Quaternion.Identity;
            worldTransform = Matrix.Identity;

            this.currentDimension = currentDimension;

            this.inFinalRoom = inFinalRoom;

            this.narrationCues = narrationCues;
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

        public void LoadContent(ref Player player, ref Level level)
        {
            max_textures = 3;
            max_targets = 3;
            textures = new Texture2D[max_textures];
            render_targets = new RenderTarget2D[max_targets];

            shader = content.Load<Effect>("CelShade");
            textures.SetValue(content.Load<Texture2D>("ColorMap"), (int)Tex_Select.model);
            textures.SetValue(content.Load<Texture2D>("Toon2"), (int)Tex_Select.cel_tex);

            collisionSphere = new BoundingSphere(position + lookAt * 50 + new Vector3(0,40,0), 100f);
            PresentationParameters pp = graphics.GraphicsDevice.PresentationParameters;

            render_targets[(int)Target_Select.normalDepth] = new RenderTarget2D(graphics.GraphicsDevice,
                pp.BackBufferWidth, pp.BackBufferHeight, 1,
                pp.BackBufferFormat, pp.MultiSampleType, pp.MultiSampleQuality);


            currentPath = ConvertToList(RunAStar(ref player, ref level));
            if (currentPath.Count < 2)
            {
                this.state = EnemyAiState.ChasingDumb;
            }
            else
            {
                this.FirstPathPoly = currentPath.First.Value.Index;
                this.SecondPathPoly = currentPath.First.Next.Value.Index;
            }

            LoadSkinnedModel();
        }

        #endregion

        #region Update

        public void Update(GameTime gameTime, ref Level currentLevel, ref Player player, SpriteFont gameFont)
        {
            // If there are any narration cues
            if (narrationCues.Count > 0)
            {
                // If the enemy is in the right state for the cue
                if (narrationCues[0].Key == this.state)
                {
                    // Check the current narration cues to see if it's already playing
                    if (currentNarration == null)
                    {
                        currentNarration = new Narration(narrationCues[0].Value, gameFont, Vector2.One * 20.0f);
                        currentNarration.LoadContent();
                        currentNarration.StartNarration();
                    }
                }

                if (currentNarration != null && currentNarration.NarrationFinished)
                {
                    // If it's done playing, remove it
                    currentNarration = null;
                    narrationCues.RemoveAt(0);
                }
            }

            if (currentNarration != null)
            {
                currentNarration.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (this.CurrentDimension == player.CurrentDimension)
            {
                this.prevState = state;

                UpdateEnemyLocation(ref currentLevel);

                //turn speed is same even if machine running slow
                turn_speed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
                turn_speed *= turn_speed_reg;

                timeBetweenDamage += (float)gameTime.ElapsedGameTime.TotalSeconds;

                #region Behavior

                if (this.state == EnemyAiState.Weakened)
                {
                    this.timeUntilRecovery -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (this.timeUntilRecovery <= 0)
                    {
                        this.state = EnemyAiState.Idle;
                        this.health = this.stats.maxHealth;
                    }
                }

                #region Hysteresis

                // First we have to use the current state to decide what the thresholds are
                // for changing state
                float enemySmartChaseThreshold = this.stats.smartChaseDistance;
                float enemyDumbChaseThreshold = this.stats.dumbChaseDistance;
                float enemyAttackThreshold = this.stats.attackDistance;

                // if the enemy is idle, he prefers to stay idle. we do this by making the
                // chase distance smaller, so the enemy will be less likely to begin chasing
                // the player.
                if (this.state == EnemyAiState.Idle)
                {
                    enemySmartChaseThreshold -= this.stats.hysteresis * 0.5f;
                }

                // similarly, if the enemy is active, he prefers to stay active. we
                // accomplish this by increasing the range of values that will cause the
                // enemy to go into the active state.
                else if (this.state == EnemyAiState.ChasingSmart)
                {
                    enemySmartChaseThreshold += this.stats.hysteresis * 0.5f;
                    enemyDumbChaseThreshold -= this.stats.hysteresis * 0.5f;
                }
                else if (this.state == EnemyAiState.ChasingDumb)
                {
                    enemyDumbChaseThreshold -= this.stats.hysteresis * 0.5f;
                    enemyAttackThreshold -= this.stats.hysteresis * 0.5f;
                }
                // the same logic is applied to the finished state.
                else if (this.state == EnemyAiState.Attack)
                {
                    enemyAttackThreshold += this.stats.hysteresis * 0.5f;
                }

                #endregion

                #region State Determination

                float distanceFromPlayer = Vector3.Distance(this.position, player.position);

                if (this.state == EnemyAiState.Weakened)
                {
                    // Do nothing
                }
                else if (distanceFromPlayer < enemyAttackThreshold)
                {
                    // Enemy is within attack radius
                    this.state = EnemyAiState.Attack;
                }
                else if (this.current_poly_index == player.current_poly_index)
                {
                    // Enemy and player are in same polygon
                    this.state = EnemyAiState.ChasingDumb;
                }
                else if (distanceFromPlayer < enemyDumbChaseThreshold)
                {
                    // Enemy is within dumb chasing radius
                    this.state = EnemyAiState.ChasingDumb;
                }
                else if (this.current_poly_index < 0 || player.current_poly_index < 0)
                {
                    // Player or enemy location unknown, so chase dumb
                    this.state = EnemyAiState.ChasingDumb;
                }
                else if (distanceFromPlayer < enemySmartChaseThreshold)
                {
                    if (this.state == EnemyAiState.ChasingSmart)
                    {
                        // Enemy was already chasing smart, so it already has a path
                    }
                    else
                    {
                        // Enemy was not in chasing smart, so make a new path
                        this.state = EnemyAiState.ChasingSmart;
                        LinkedList<NavMeshNode> newPath = ConvertToList(RunAStar(ref player, ref currentLevel));

                        if (newPath == null || newPath.Count < 2)
                        {
                            throw new Exception("Invalid Path");
                        }
                        else
                        {
                            if (beginChase)
                            {
                                //GameplayScreen.soundControl.Play("enemy alerted");
                                beginChase = false;
                            }
                            this.currentPath = newPath;
                            this.FirstPathPoly = this.currentPath.First.Value.Index;
                            this.SecondPathPoly = this.currentPath.First.Next.Value.Index;
                        }
                    }
                }
                else
                {
                    this.state = EnemyAiState.Idle;
                }

                #endregion

                // Third, once we know what state we're in, act on that state.
                float currentEnemySpeed;
                if (this.state == EnemyAiState.ChasingSmart)
                {
                    if (lineOfSightTriggered || !this.stats.useLineOfSight ||
                        (this.stats.useLineOfSight && !currentLevel.PolygonExistsBetween(this.position + GameplayScreen.NUDGE_UP, player.position + GameplayScreen.NUDGE_UP)))
                    {
                        lineOfSightTriggered = true;

                        if (prevState != EnemyAiState.ChasingSmart)
                        {
                            beginChase = true;
                            GameplayScreen.soundControl.Play("enemy alerted");
                        }
                        try
                        {
                            Vector3 moveTo = Navigate(ref currentLevel, ref player);
                            this.rotation = TurnToFace(this.position, moveTo, this.rotation, this.turn_speed);
                            currentEnemySpeed = this.stats.maxSpeed;
                        }
                        catch (Exception e)
                        {
                            // If smart navigation fails, revert to dumb
                            this.state = EnemyAiState.ChasingDumb;
                            this.rotation = TurnToFace(this.position, player.position, this.rotation, this.turn_speed);
                            currentEnemySpeed = this.stats.maxSpeed;
                        }
                    }
                    // Line of sight is enabled, but enemy doesn't have a line of sight
                    else
                    {
                        this.state = EnemyAiState.Idle;
                    }
                }
                else if (this.state == EnemyAiState.ChasingDumb)
                {
                    this.rotation = TurnToFace(this.position, player.position, this.rotation, this.turn_speed);
                    currentEnemySpeed = this.stats.maxSpeed;
                }
                else if (this.state == EnemyAiState.Idle)
                {
                    // Do nothing
                }
                else if (this.state == EnemyAiState.Attack)
                {
                    // if the enemy catches the player, it should stop.
                    // Otherwise it will run right by, then spin around and
                    // try to catch it all over again. The end result is that it will kind
                    // of "run laps" around the player, which looks funny, but is not what
                    // we're after.
                    
                    currentEnemySpeed = 0.0f;

                    if (!player.isHit) // If player is not currently taking damage
                    {
                        player.isHit = true; // Player takes damage
                        player.health -= 20;
                        if (player.HealthIsLow && !player.IsDead)
                        {
                            GameplayScreen.soundControl.Play("heartbeat");
                        }
                        GameplayScreen.soundControl.Play("player hit 1");
                        timeBetweenDamage = 0;
                    }
                }

                #endregion
                orientation = Quaternion.CreateFromAxisAngle(Vector3.Up, rotation);
                lookAt = Vector3.Transform(new Vector3(0, 0, 1), orientation);

                MoveForward(lookAt, ref currentLevel);

                // Update the animation according to the elapsed time
                controller.Update(gameTime.ElapsedGameTime, Matrix.Identity);
            }
        }

        public void ResetRecoveryTime()
        {
            this.timeUntilRecovery = this.stats.recoveryTime;
        }

        public void IncreaseMaxHealth()
        {
            this.stats.maxHealth += 50;
        }

        private void MoveForward(Vector3 heading, ref Level currentLevel)
        {
            worldTransform = Matrix.CreateFromQuaternion(orientation);
            worldTransform.Translation = position;
            velocity = heading * 30;
            velocity.Y = -1.0f;
            
            if (this.state == EnemyAiState.Attack)
            {
                if (this.activeAnimationClip != 2)
                {
                    this.activeAnimationClip = 2;
                    controller.CrossFade(model.AnimationClips["Attack"],TimeSpan.FromMilliseconds(300));
                    controller.Speed = 2.0f;
                    GameplayScreen.soundControl.Play("enemy attacking");
                }
                position = currentLevel.CollideWith(position, Vector3.Down, 0.1, Level.MAX_COLLISIONS);
            }
            else if (this.state == EnemyAiState.Weakened || this.state == EnemyAiState.Idle)
            {
                if (this.activeAnimationClip != 0)
                {
                    this.activeAnimationClip = 0;
                    controller.CrossFade(model.AnimationClips["Idle"], TimeSpan.FromMilliseconds(300));
                    controller.Speed = 1.0f;
                }
            }
            else {
                if (this.activeAnimationClip != 1)
                {
                    controller.CrossFade(model.AnimationClips["Walk"], TimeSpan.FromMilliseconds(300));
                    this.activeAnimationClip = 1;
                    controller.Speed = 4.0f;
                }
                position = currentLevel.CollideWith(position, velocity, 0.1, Level.MAX_COLLISIONS);
            }
            collisionSphere.Center = position + lookAt * 10 + new Vector3(0,25,0);
        }

        private void UpdateEnemyLocation(ref Level currentLevel)
        {
            //enemy... same process as player.
            if (currentLevel.IntersectsNavQuad(new Ray(this.position + new Vector3(0, 5, 0), Vector3.Down),
                this.current_poly_index))
            {
                this.prev_poly_index = this.current_poly_index;
            }
            else
            {
                this.prev_poly_index = this.current_poly_index;
                this.current_poly_index = currentLevel.NavigationIndex(this.position, this.current_poly_index);
            }
        }

        /// <summary>
        /// Here we use the optimal path returned by FindPath to move the enemy.
        /// </summary>
        Vector3 Navigate(ref Level currentLevel, ref Player player)
        {
            if (this.state == Enemy.EnemyAiState.Idle)
            {
                // This state should be impossible
                throw new Exception("Enemy state is idle while in Navigate");
            }
            else if (this.state == Enemy.EnemyAiState.ChasingSmart)
            {
                // calculate movement vector to orient the enemy and move him
                if (player.current_poly_index == player.prev_poly_index)
                {
                    // player has not moved since the last frame... continue following the 
                    // old path.

                    if (this.current_poly_index == this.prev_poly_index)
                    {
                        // Enemy also hasn't changed polygons, so keep following the same path

                        if (this.SecondPathPoly == this.current_poly_index)
                        {
                            // If enemy is travelling to the polygon it is currently in, move to next (if available)
                            if (this.CurrentPath.Count > 2)
                            {
                                this.CurrentPath.RemoveFirst();
                                this.FirstPathPoly = this.SecondPathPoly;
                                this.SecondPathPoly = this.CurrentPath.First.Next.Value.Index;
                            }
                            else
                            {
                                throw new Exception("Current path is invalid");
                            }
                        }
                        Vector3 travel_point = currentLevel.TravelPoint(this.SecondPathPoly);
                        return travel_point;
                    }
                    else // Enemy has changed polygons, but player hasn't
                    {
                        if (this.CurrentPath.Count < 2)
                        {
                            this.CurrentPath = ConvertToList(currentLevel.FindPath(this.current_poly_index, player.current_poly_index));

                            if (this.CurrentPath.Count < 2)
                            {
                                // If the path is still too short, chase dumb
                                throw new Exception("Current path is too short");
                            }
                        }

                        this.CurrentPath.RemoveFirst();

                        this.FirstPathPoly = this.SecondPathPoly;

                        if (this.CurrentPath.Count > 2)
                        {
                            // Only advance the SecondPathPoly if there is one after it
                            this.SecondPathPoly = this.CurrentPath.First.Next.Value.Index;
                        }

                        Vector3 travel_point = currentLevel.TravelPoint(this.SecondPathPoly);

                        return travel_point;
                    }
                }
                else // Player position has changed
                {

                    if (player.current_poly_index < 0 || this.current_poly_index < 0)
                    {
                        throw new Exception("Player or enemy polygon index invalid while chasing smart");
                    }
                    else // Player position has changed
                    {
                        LinkedList<NavMeshNode> newPath = ConvertToList(RunAStar(ref player, ref currentLevel));
                        if (newPath.Count < 2)
                        {
                            throw new Exception("Invalid new path in Navigate");
                        }
                        else
                        {
                            this.currentPath = newPath;
                            this.FirstPathPoly = currentPath.First.Value.Index;
                            this.SecondPathPoly = currentPath.First.Next.Value.Index;
                        }
                    }

                    Vector3 travel_point = currentLevel.TravelPoint(this.SecondPathPoly);

                    return travel_point;
                }
            }
            else
            {
                throw new Exception("Enemy state not chasing smart while in Navigate");
            }
        }

        private LinkedList<NavMeshNode> ConvertToList(Path<NavMeshNode> path)
        {
            LinkedList<NavMeshNode> list = new LinkedList<NavMeshNode>();
            Path<NavMeshNode> pathNode = path;
            while (pathNode != null)
            {
                list.AddFirst(pathNode.LastStep);
                pathNode = pathNode.PreviousSteps;
            }

            return list;
        }
        
        #region AI

        /// <summary>
        /// This is the wrapper for the enemy pathfinding AI using A*.  It takes the player and
        /// the enemy as arguments.  First it casts a ray straight down to determine which polygons
        /// the player and enemy are in (with respect to the NavigationMesh data structure).  Both the
        /// player and enemy keep information about which polygons they are currently in and which ones
        /// they were in last.  The rigourous initial calculations of this data occurs at game initialization.
        /// This allows us to only have to check polygons that are adjacent to the player and enemy current 
        /// polygons for the ray casting, rather than the entire collision mesh.  From there we call
        /// FindPath which returns the optimal path from the start node to the destination node.  Then 
        /// we do processing to orient the enemy and smooth his traversal of the path.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        Path<NavMeshNode> RunAStar(ref Player player, ref Level currentLevel)
        {
            return currentLevel.FindPath(this.current_poly_index, player.current_poly_index);
        }

        /// <summary>
        /// Calculates the angle that an object should face, given its position, its
        /// target's position, its current angle, and its maximum turning speed.
        /// </summary>
        private float TurnToFace(Vector3 position, Vector3 faceThis,
            float currentAngle, float turnSpeed)
        {
            //x and z directions along the 2D floor plane
            float x = faceThis.X - this.position.X;
            float z = faceThis.Z - this.position.Z;

            float desiredAngle = (float)Math.Atan2(x, z);
            //return desiredAngle;

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
            //Vector3 screenCenter = Vector3.Zero;
            //screenCenter.X = graphics.GraphicsDevice.Viewport.Width * 0.5f;
            //screenCenter.Z = graphics.GraphicsDevice.Viewport.Height * 0.5f;

            //float distanceFromScreenCenter = Vector3.Distance(screenCenter, position);
            //float MaxDistanceFromScreenCenter =
            //    Math.Min(screenCenter.Z, screenCenter.X);

            //float normalizedDistance =
            //    distanceFromScreenCenter / MaxDistanceFromScreenCenter;

            //float turnToCenterSpeed = .3f * normalizedDistance * normalizedDistance *
            //    turnSpeed;

            //// once we've calculated how much we want to turn towards the center, we can
            //// use the TurnToFace function to actually do the work.
            //orientation = TurnToFace(position, screenCenter, orientation,
            //    turnToCenterSpeed);
        }

        #endregion

        #endregion

        #region Draw

        public void DrawCel(GameTime gameTime, Matrix view, Matrix projection,
            ref RenderTarget2D scene, ref RenderTarget2D shadow, ref List<Light> Lights,
            Vector3 playerPosition, Dimension playerDimension, ref SpriteBatch spriteBatch)
        {
            
            #region Cel Shading

            if (currentNarration != null)
            {
                spriteBatch.Begin();
                currentNarration.Draw(ref spriteBatch);
                spriteBatch.End();
            }

            if (playerDimension == this.currentDimension || GameplayScreen.transitioning)
            {
                //Cel shading
                graphics.GraphicsDevice.RenderState.AlphaBlendEnable = false;
                //graphics.GraphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
                //graphics.GraphicsDevice.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
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
                        effect.Parameters["material"].StructureMembers["diffuseColor"].SetValue(new Vector3((float)this.health * 0.01f, (float)this.health * 0.01f, (float)this.health * 0.01f));
                        effect.Parameters["material"].StructureMembers["specularColor"].SetValue(new Vector3(0.3f));
                        effect.Parameters["material"].StructureMembers["specularPower"].SetValue(10);
                        //effect.Parameters["diffuseMapEnabled"].SetValue(true);
                        effect.Parameters["playerPosition"].SetValue(playerPosition);
                        effect.Parameters["transitionRadius"].SetValue(GameplayScreen.transitionRadius);
                        effect.Parameters["waveRadius"].SetValue(GameplayScreen.transitionRadius - GameplayScreen.WAVE_FRONT_SIZE);

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

                        string techniqueModifier = "";

                        if (playerDimension == Dimension.FIRST)
                        {
                            techniqueModifier = "";
                        }
                        else
                        {
                            techniqueModifier = "_Gray";
                        }

                        if (GameplayScreen.transitioning)
                        {
                            effect.Parameters["transitioning"].SetValue(1);
                        }
                        else
                        {
                            effect.Parameters["transitioning"].SetValue(0);
                        }

                        if (playerDimension == this.currentDimension)
                        {
                            // If enemy is in player's dimension and is beyond the transition effect, clip
                            effect.Parameters["inOtherDimension"].SetValue(-1);
                        }
                        else
                        {
                            // If enemy is not in player's dimension and is beyond the transition effect, don't clip
                            effect.Parameters["inOtherDimension"].SetValue(1);
                        }

                        switch (Lights.Count)
                        {
                            case 1: effect.CurrentTechnique = effect.Techniques["AnimatedModel_OneLight" + techniqueModifier];
                                break;
                            case 2: effect.CurrentTechnique = effect.Techniques["AnimatedModel_TwoLight" + techniqueModifier];
                                break;
                            case 3: effect.CurrentTechnique = effect.Techniques["AnimatedModel_ThreeLight" + techniqueModifier];
                                break;
                            case 4: effect.CurrentTechnique = effect.Techniques["AnimatedModel_FourLight" + techniqueModifier];
                                break;
                            case 5: effect.CurrentTechnique = effect.Techniques["AnimatedModel_FiveLight" + techniqueModifier];
                                break;
                            case 6: effect.CurrentTechnique = effect.Techniques["AnimatedModel_SixLight" + techniqueModifier];
                                break;
                            case 7: effect.CurrentTechnique = effect.Techniques["AnimatedModel_SevenLight" + techniqueModifier];
                                break;
                            case 8: effect.CurrentTechnique = effect.Techniques["AnimatedModel_EightLight" + techniqueModifier];
                                break;
                            default: effect.CurrentTechnique = effect.Techniques["AnimatedModel_EightLight" + techniqueModifier];
                                break;
                        }
                    }

                    // Draw model mesh
                    //foreach (EffectPass pass in shader.CurrentTechnique.Passes)
                    //{
                    modelMesh.Draw();
                    //}
                }
            }

            #endregion

        }

        #endregion
    }
}
