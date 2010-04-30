#region File Description
//-----------------------------------------------------------------------------
// Projectile.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
#endregion

namespace WorldTest
{
    /// <summary>
    /// This class demonstrates how to combine several different particle systems
    /// to build up a more sophisticated composite effect. It implements a rocket
    /// projectile, which arcs up into the sky using a ParticleEmitter to leave a
    /// steady stream of trail particles behind it. After a while it explodes,
    /// creating a sudden burst of explosion and smoke particles.
    /// </summary>
    abstract class Projectile
    {

        #region Fields

        public bool is_released = false;

        protected float trailParticlesPerSecond; //200
        protected int numContactParticles; //30
        protected int numExtraContactParticles; //10
        protected float projectileLifespan; //1.5
        public float LifeSpan
        {
            get { return projectileLifespan; }
            set { projectileLifespan = value; }
        }
        protected float sidewaysVelocityRange = 60;
        protected float verticalVelocityRange = 40;
        protected float gravity; //15
        public float Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }

        protected ParticleSystem contactParticles;
        protected ParticleSystem extraContactParticles;
        protected ParticleEmitter trailEmitter;

        protected Vector3 position;
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        protected Vector3 velocity;
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        protected float age;
        public float Age
        {
            get { return age; }
            set { age = value; }
        }

        public Light light;
        public Ray projRay;
        public bool is_banisher;

        static Random random = new Random();

        #endregion


        /// <summary>
        /// Constructs a new projectile... called from initializer of 'Attack' derived class.
        /// </summary>
        protected Projectile()
        {
            
        }


        /// <summary>
        /// Updates the projectile.
        /// </summary>
        public virtual bool Update(GameTime gameTime, ref Level level, ref List<Enemy> enemies, Dimension playerDimension)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Simple projectile physics.
            if (!is_released)
            {
                
            }
            else
            {
                position += velocity * elapsedTime;
            }
            velocity.Y -= elapsedTime * gravity;
            age += elapsedTime;
            light.position = this.position;
            //collisionSphere.Center = this.position;


            // Update the particle emitter, which will create our particle trail.
            trailEmitter.Update(gameTime, position);

            Vector3 collidedPosition = this.position;
            Ray dir = new Ray(this.position, Vector3.Normalize(this.velocity));

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].CurrentDimension != playerDimension)
                {
                    //pass over enemy in other dimension
                }
                else
                {
                    float? dist = this.intersectRaySphere(dir, enemies[i].collisionSphere);
                    if (dist == null) continue;
                    Vector3 len = dir.Position - enemies[i].collisionSphere.Center;
                    if (len.Length() <= enemies[i].collisionSphere.Radius)
                    {
                        age = projectileLifespan + 1;
                        if (this.is_banisher)
                        {
                            if (enemies[i].state == Enemy.EnemyAiState.Weakened)
                            {
                                enemies[i].ChangeDimension();
                                enemies[i].state = Enemy.EnemyAiState.Idle;
                                enemies[i].IncreaseMaxHealth();
                                enemies[i].health = enemies[i].MaxHealth;
                            }
                        }
                        else
                        {
                            enemies[i].health -= 50;
                            if (enemies[i].health <= 0)
                            {
                                enemies[i].state = Enemy.EnemyAiState.Weakened;
                                enemies[i].ResetRecoveryTime();
                            }
                        }

                        break;
                    }
                    else
                    {
                        //do nothing because it's not close enough yet
                    }
                }
            }

            //collision detect whether emitter collided with geometry or agents
            if (level.EmitterCollideWith(this.position, this.velocity, 0.005f))
            {
                age = projectileLifespan + 1;
            }

            // If enough time has passed or it collides with something, explode! Note how we pass 
            // our velocity in to the AddParticle method: this lets the explosion be influenced
            // by the speed and direction of the projectile which created it.
            if (age > projectileLifespan)
            {
                for (int i = 0; i < numContactParticles; i++)
                    contactParticles.AddParticle(position, velocity);

                for (int i = 0; i < numExtraContactParticles; i++)
                    extraContactParticles.AddParticle(position, velocity);

                this.position = this.position + ((collidedPosition - this.position) / 2.0f);

                if (this.is_banisher)
                {
                    GameplayScreen.soundControl.Play("banish hit");
                }
                else
                {
                    GameplayScreen.soundControl.Play("fireball_hit");
                }
                return false;
            }
                
            return true;
        }

        public float? intersectRaySphere(Ray ray, BoundingSphere sphere) {
	        Vector3 dst = ray.Position - sphere.Center;
	        float B = Vector3.Dot(dst, ray.Direction);
	        float C = Vector3.Dot(dst, dst) - sphere.Radius*sphere.Radius;
	        float D = B*B - C;
	        return D > 0 ? -B - (float?)Math.Sqrt(D) : null;
        }


    }
}
