#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class IceAttackParticles : ParticleSystem
    {
        public IceAttackParticles(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "GenericParticle";

            settings.MaxParticles = 240000;

            settings.Duration = TimeSpan.FromSeconds(2.5);

            settings.DurationRandomness = 0;

            settings.MinHorizontalVelocity = 150;
            settings.MaxHorizontalVelocity = 200;

            settings.MinVerticalVelocity = 100;
            settings.MaxVerticalVelocity = 200;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 50, 0);

            settings.MinColor = new Color(0, 0, 150, 255); //10
            settings.MaxColor = new Color(0, 80, 255, 255); //40
            
            settings.MinStartSize = 40;
            settings.MaxStartSize = 50;

            settings.MinEndSize = 60;
            settings.MaxEndSize = 80;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
