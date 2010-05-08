#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace CaveOfShadows
{
    /// <summary>
    /// Custom particle system for creating the smokey part of the explosions.
    /// </summary>
    class LavaBall : Projectile
    {
        #region Fields

        public bool firstTime = true;

        #endregion

        public LavaBall(Vector3 position, Vector3 velocity, float trail_particles_per_s,
                            int num_contact_parts, int num_ext_contact_parts,
                            float lifespan, float gravity,
                            ParticleSystem contactParticles,
                            ParticleSystem extraContactParticles,
                            ParticleSystem projectileTrailParticles,
                            bool needLight)
            : base()
        {
            this.position = position;
            this.velocity = velocity;
            this.trailParticlesPerSecond = trail_particles_per_s;
            this.numContactParticles = num_contact_parts;
            this.numExtraContactParticles = num_ext_contact_parts;
            this.projectileLifespan = lifespan;
            this.gravity = gravity;
            this.needLight = needLight;
            this.contactParticles = contactParticles;
            this.extraContactParticles = extraContactParticles;
            trailEmitter = new ParticleEmitter(projectileTrailParticles,
                                               trailParticlesPerSecond, position);
            
            //collisionSphere = new BoundingSphere(position, 20);
            is_released = false;
            firstTime = true;
        }

        public bool Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            position += velocity * elapsedTime;
            velocity.Y -= elapsedTime * gravity;
            age += elapsedTime;

            trailEmitter.Update(gameTime, position);

            // If enough time has passed or it collides with something, explode! Note how we pass 
            // our velocity in to the AddParticle method: this lets the explosion be influenced
            // by the speed and direction of the projectile which created it.
            if (age > projectileLifespan)
            {

                for (int i = 0; i < numExtraContactParticles; i++)
                    extraContactParticles.AddParticle(position, velocity);

                return false;
            }

            return true; 
        }
    }
}

