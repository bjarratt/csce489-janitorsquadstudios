#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class PortalParticleSystem : ParticleSystem
    {
        public PortalParticleSystem(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }

        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "GenericParticle";

            settings.MaxParticles = 100000;

            settings.Duration = TimeSpan.FromSeconds(3);

            settings.DurationRandomness = 1;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 2;

            settings.MinVerticalVelocity = -2;
            settings.MaxVerticalVelocity = 2;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 20, 0);

            settings.MinColor = new Color(0, 255, 255, 200); //10
            settings.MaxColor = new Color(0, 255, 255, 255); //40

            settings.MinStartSize = 10;
            settings.MaxStartSize = 15;

            settings.MinEndSize = 1f;
            settings.MaxEndSize = 3;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
