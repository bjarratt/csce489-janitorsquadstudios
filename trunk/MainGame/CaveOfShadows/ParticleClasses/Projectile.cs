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

        protected float trailParticlesPerSecond; //200
        protected int numContactParticles; //30
        protected int numExtraContactParticles; //10
        protected float projectileLifespan; //1.5
        protected float sidewaysVelocityRange = 60;
        protected float verticalVelocityRange = 40;
        protected float gravity; //15

        protected ParticleSystem contactParticles;
        protected ParticleSystem extraContactParticles;
        protected ParticleEmitter trailEmitter;

        protected Vector3 position;
        protected Vector3 velocity;
        protected float age;

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
        public bool Update(GameTime gameTime, ref Level level)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Simple projectile physics.
            position += velocity * elapsedTime;
            velocity.Y -= elapsedTime * gravity;
            age += elapsedTime;

            // Update the particle emitter, which will create our particle trail.
            trailEmitter.Update(gameTime, position);

            //collision detect whether emitter collided with geometry or agents
            if (level.EmitterCollideWith(this.position, this.velocity, 0.2f))
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

                for (int i = 0; i < numExplosionSmokeParticles; i++)
                    extraContactParticles.AddParticle(position, velocity);

                return false;
            }
                
            return true;
        }
    }
}
