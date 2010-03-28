using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WorldTest
{
    class Attack : Projectile
    {
        /// <summary>
        /// Constructs a new attack.
        /// </summary>
        public Attack(Vector3 position, Vector3 velocity, float trail_particles_per_s,
                            int num_contact_parts, int num_ext_contact_parts, 
                            float lifespan, float gravity,
                            ParticleSystem contactParticles,
                            ParticleSystem extraContactParticles,
                            ParticleSystem projectileTrailParticles) : base()
        {
            this.position = position;
            this.velocity = velocity;
            this.trailParticlesPerSecond = trail_particles_per_s;
            this.numContactParticles = num_contact_parts;
            this.numExtraContactParticles = num_ext_contact_parts;
            this.projectileLifespan = lifespan;
            this.gravity = gravity;
            this.contactParticles = contactParticles;
            this.extraContactParticles = extraContactParticles;
            trailEmitter = new ParticleEmitter(projectileTrailParticles,
                                               trailParticlesPerSecond, position);
        }

        public bool Update(GameTime gameTime, ref Level level)
        {
            return base.Update(gameTime, ref level);
        }
    }
}
