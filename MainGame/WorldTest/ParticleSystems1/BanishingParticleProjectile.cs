#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    /// <summary>
    /// Custom particle system for leaving smoke trails behind the rocket projectiles.
    /// </summary>
    class BanishingParticleSystem : ParticleSystem
    {
        public BanishingParticleSystem(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "GenericParticle";

            settings.MaxParticles = 100000;

            settings.Duration = TimeSpan.FromSeconds(0.7f);

            settings.DurationRandomness = 0.0f;

            settings.EmitterVelocitySensitivity = 0.0f;

            settings.MinHorizontalVelocity = -50;
            settings.MaxHorizontalVelocity = 40;

            settings.MinVerticalVelocity = -40;
            settings.MaxVerticalVelocity = 50;

            settings.EndVelocity = 0;

            settings.MinColor = new Color(0, 200, 200, 200);
            settings.MaxColor = new Color(0, 255, 255, 255);

            settings.MinRotateSpeed = 0;
            settings.MaxRotateSpeed = 0;

            settings.MinStartSize = 20;
            settings.MaxStartSize = 25;

            settings.MinEndSize = 1;
            settings.MaxEndSize = 2;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}

