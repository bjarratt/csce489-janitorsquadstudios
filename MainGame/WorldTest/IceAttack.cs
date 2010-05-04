﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using XNAnimation;
using XNAnimation.Controllers;
using XNAnimation.Effects;

namespace WorldTest
{
    class IceAttack
    {
        #region Properties

        /// <summary>
        /// This is the point in world space where the portal is located
        /// (center of the circle)
        /// </summary>
        private Vector3 origin;

        public Vector3 Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        private float radius;

        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        private float time = 0;

        /// <summary>
        /// Particle System for drawing the circular portal.
        /// </summary>
        public IceAttackParticles iceParticles;
        //public PortalMystParticleSystem PortalMyst;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private static Random random = new Random();

        public static Random Random
        {
            get { return random; }
        }

        #endregion

        #region Constructor

        public IceAttack(Vector3 origin, float radius)
        {
            this.origin = origin;
            this.radius = radius;
        }

        #endregion

        #region Load

        public void Load(Game game, ContentManager content)
        {
            iceParticles = new IceAttackParticles(game, content, false);
            game.Components.Add(iceParticles);

            //PortalMyst = new PortalMystParticleSystem(game, content, false);
            //game.Components.Add(PortalMyst);
        }

        #endregion

        #region Update

        public void Update(GameTime gameTime, ControlState state, ref Player player)
        {
            //time += (float)gameTime.ElapsedGameTime.TotalSeconds;
            //for (int i = 0; i < 4; i++)
            //{
            //    PortalMagic.AddParticle(RandomPointInCircle(this.origin, this.radius), Vector3.Zero);
            //}
            //time = 0;
            this.origin = player.position;
            //if (state.currentGamePadState.Triggers.Right != 0)
            //{
                if (state.currentGamePadState.Buttons.X == ButtonState.Pressed && state.lastGamePadState.Buttons.X == ButtonState.Released)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        iceParticles.AddParticle(RandomPointOnCircle(player.position, this.radius), new Vector3(0, 5, 0));
                    }
                }
            //}
        }

        #endregion

        #region Draw

        public void Draw(GameTime gameTime, Matrix view, Matrix proj)
        {
            iceParticles.SetCamera(view, proj);
            iceParticles.Draw(gameTime);
            //PortalMyst.SetCamera(view, proj);
            //PortalMyst.Draw(gameTime);
        }

        #endregion

        #region Update Helpers

        public Vector3 RandomPointOnCircle(Vector3 origin, float radius)
        {
            double angle = (double)RandomBetween(0.0f, MathHelper.TwoPi);
            return new Vector3(origin.X + radius * (float)Math.Cos(angle), origin.Y, origin.Z + radius * (float)Math.Sin(angle));
        }

        public Vector3 RandomPointInCircle(Vector3 origin, float radius)
        {
            double angle = (double)RandomBetween(0.0f, MathHelper.TwoPi);
            return new Vector3(origin.X + RandomBetween(0.0f, radius) * (float)Math.Cos(angle), origin.Y, origin.Z + RandomBetween(0.0f, radius) * (float)Math.Sin(angle));
        }

        public static float RandomBetween(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        #endregion

    }
}
