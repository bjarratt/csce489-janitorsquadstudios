#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class Lava : ParticleSystem
    {

        public Lava(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }

        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "lava_ball";

            settings.MaxParticles = 1000;

            settings.Duration = TimeSpan.FromSeconds(0.05);

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 0;

            settings.MinVerticalVelocity = 0;
            settings.MaxVerticalVelocity = 0;

            settings.Gravity = new Vector3(0, 0, 0);

            settings.EndVelocity = 1;

            settings.MinColor = Color.White;
            settings.MaxColor = Color.White;

            settings.MinRotateSpeed = -20;
            settings.MaxRotateSpeed = 20;

            settings.MinStartSize = 20;
            settings.MaxStartSize = 25;

            settings.MinEndSize = 25;
            settings.MaxEndSize = 25;

            settings.EmitterVelocitySensitivity = 0;
        }
    }
}
