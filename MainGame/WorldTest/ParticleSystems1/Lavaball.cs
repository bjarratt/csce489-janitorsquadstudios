#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    /// <summary>
    /// Custom particle system for creating the smokey part of the explosions.
    /// </summary>
    class LavaBall : ParticleSystem
    {
        public LavaBall(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "lava_ball";

            settings.MaxParticles = 200;

            settings.Duration = TimeSpan.FromSeconds(1.0f);

            settings.MinHorizontalVelocity = -250;
            settings.MaxHorizontalVelocity = 250;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 200;

            settings.Gravity = new Vector3(0, -290, 0);

            settings.EndVelocity = 2;

            settings.MinColor = Color.White;
            settings.MaxColor = Color.White;

            settings.MinRotateSpeed = -20;
            settings.MaxRotateSpeed = 20;

            settings.MinStartSize = 30;
            settings.MaxStartSize = 40;

            settings.MinEndSize = 30;
            settings.MaxEndSize = 40;

            settings.EmitterVelocitySensitivity = 0;
        }
    }
}

