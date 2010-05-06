#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class BanishingHandSystem : ParticleSystem
    {
        public BanishingHandSystem(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }

        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "GenericParticle";

            settings.MaxParticles = 100000;

            settings.Duration = TimeSpan.FromSeconds(0.5f);

            settings.DurationRandomness = 1;

            settings.MinHorizontalVelocity = -10;
            settings.MaxHorizontalVelocity = 10;

            settings.MinVerticalVelocity = -10;
            settings.MaxVerticalVelocity = 10;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 20, 0);

            settings.MinColor = new Color(80, 43, 226, 250); //10
            settings.MaxColor = new Color(80, 43, 226, 250); //40

            settings.MinStartSize = 5;
            settings.MaxStartSize = 7;

            settings.MinEndSize = 0.2f;
            settings.MaxEndSize = 1;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
