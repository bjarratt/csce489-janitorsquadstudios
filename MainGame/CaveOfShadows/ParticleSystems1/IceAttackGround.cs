#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace CaveOfShadows
{
    class IceAttackGround : ParticleSystem
    {
        public IceAttackGround(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "GenericParticle";

            settings.MaxParticles = 240000;

            settings.Duration = TimeSpan.FromSeconds(1.0f);

            settings.DurationRandomness = 0;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 0;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 0;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 50, 0);

            settings.MinColor = new Color(0, 80, 150, 255); //10
            settings.MaxColor = new Color(0, 80, 255, 255); //40
            
            settings.MinStartSize = 160;
            settings.MaxStartSize = 170;

            settings.MinEndSize = 90;
            settings.MaxEndSize = 90;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
