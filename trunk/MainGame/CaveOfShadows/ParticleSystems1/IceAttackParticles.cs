#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace CaveOfShadows
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

            settings.Duration = TimeSpan.FromSeconds(1.5f);

            settings.DurationRandomness = 0;

            settings.MinHorizontalVelocity = 150;
            settings.MaxHorizontalVelocity = 200;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 300;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 50, 0);

            settings.MinColor = new Color(0, 200, 255, 255); //10
            settings.MaxColor = new Color(0, 255, 255, 255); //40
            
            settings.MinStartSize = 20;
            settings.MaxStartSize = 20;

            settings.MinEndSize = 10;
            settings.MaxEndSize = 10;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
