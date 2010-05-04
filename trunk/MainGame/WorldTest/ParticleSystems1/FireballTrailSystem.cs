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
    class FireballTrailSystem : ParticleSystem
    {
        public FireballTrailSystem(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "fire";

            settings.MaxParticles = 1000000;

            settings.Duration = TimeSpan.FromSeconds(0.5);

            settings.DurationRandomness = 0.0f;

            settings.EmitterVelocitySensitivity = 0.0f;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 0;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 0;

            settings.EndVelocity = 1;

            settings.MinColor = new Color(255, 255, 255, 255);
            settings.MaxColor = new Color(255, 255, 255, 128);

            settings.Gravity = new Vector3(0, -100, 0);

            settings.MinRotateSpeed = -4;
            settings.MaxRotateSpeed = 4;

            settings.MinStartSize = 40;
            settings.MaxStartSize = 50;

            settings.MinEndSize = 2;
            settings.MaxEndSize = 3;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
