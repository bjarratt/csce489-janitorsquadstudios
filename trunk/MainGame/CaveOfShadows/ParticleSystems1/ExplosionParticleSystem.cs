#region File Description
//-----------------------------------------------------------------------------
// ExplosionParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace CaveOfShadows
{
    /// <summary>
    /// Custom particle system for creating the fiery part of the explosions.
    /// </summary>
    class ExplosionParticleSystem : ParticleSystem
    {
        public ExplosionParticleSystem(Game game, ContentManager content, bool attached)
            : base(game, content, attached)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "fire";

            settings.MaxParticles = 10000;

            settings.Duration = TimeSpan.FromSeconds(0.5f);
            settings.DurationRandomness = 0.1f;

            settings.MinHorizontalVelocity = 40;
            settings.MaxHorizontalVelocity = 50;

            settings.MinVerticalVelocity = -50;
            settings.MaxVerticalVelocity = 50;

            settings.EndVelocity = -1f;

            settings.MinColor = Color.White;
            settings.MaxColor = Color.White;

            settings.MinRotateSpeed = -1;
            settings.MaxRotateSpeed = 1;

            settings.MinStartSize = 10;
            settings.MaxStartSize = 10;

            settings.MinEndSize = 200;
            settings.MaxEndSize = 300;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
