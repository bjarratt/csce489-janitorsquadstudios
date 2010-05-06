#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class IceAttackEnergy : ParticleSystem
    {
        public IceAttackEnergy(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "generic_particle2";

            settings.MaxParticles = 240000;

            settings.Duration = TimeSpan.FromSeconds(1.5f);

            settings.DurationRandomness = 0;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 0;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 800;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 150, 0);

            settings.MinColor = new Color(0, 150, 255, 255); //10
            settings.MaxColor = new Color(0, 255, 255, 255); //40
            
            settings.MinStartSize = 40;
            settings.MaxStartSize = 50;

            settings.MinEndSize = 20;
            settings.MaxEndSize = 20;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
