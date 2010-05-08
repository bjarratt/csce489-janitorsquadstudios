using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CaveOfShadows
{
    class ParticleProjectiles : Projectile
    {
        public ParticleProjectiles(Vector3 position, Vector3 velocity, float trail_particles_per_s,
                            int num_contact_parts, int num_ext_contact_parts, 
                            float lifespan, float gravity,
                            ParticleSystem contactParticles,
                            ParticleSystem extraContactParticles,
                            ParticleSystem projectileTrailParticles,
                            ref List<Enemy> enemies, bool is_banisher, bool needLight) : base()
        {
            this.position = position;
            this.velocity = velocity;
            this.trailParticlesPerSecond = trail_particles_per_s;
            this.numContactParticles = num_contact_parts;
            this.numExtraContactParticles = num_ext_contact_parts;
            this.projectileLifespan = lifespan;
            this.gravity = gravity;
            this.is_banisher = is_banisher;
            this.needLight = needLight;
            this.contactParticles = contactParticles;
            this.extraContactParticles = extraContactParticles;
            trailEmitter = new ParticleEmitter(projectileTrailParticles,
                                               trailParticlesPerSecond, position);
            if (is_banisher)
            {
                light = new Light(position, GameplayScreen.BANISH_COLOR * 2.0f, 3000.0f);
            }
            else
            {
                if (needLight)
                    light = new Light(position, GameplayScreen.FIRE_COLOR * 2.0f, 3000.0f);
            }
            //collisionSphere = new BoundingSphere(position, 20);
            is_released = false;
        }
    }
}
