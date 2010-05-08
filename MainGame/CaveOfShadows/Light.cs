using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace CaveOfShadows
{
    class Light
    {
        public Vector3 position;
        public Vector3 color;
        public float attenuationRadius;
        public float currentExplosionTick = 0.0f;

        public Light()
        {

        }

        /// <summary>
        /// Initialize a dynamic light
        /// </summary>
        /// <param name="pos">Position of the light</param>
        /// <param name="col">Color (intensity) of the light</param>
        /// <param name="attenuationRadius">Radius at which light intensity is zero</param>
        public Light(Vector3 pos, Vector3 col, float attenuationRadius)
        {
            position = pos;
            color = col;
            this.attenuationRadius = attenuationRadius;
        }

        /// <summary>
        /// Initialize a dynamic light for an explosion flash
        /// </summary>
        /// <param name="pos">Position of the light</param>
        /// <param name="col">Color (intensity) of the light</param>
        /// <param name="attenuationRadius">Radius at which the light intensity is zero</param>
        /// <param name="initialTick">Initial setting for explosion flash countdown</param>
        public Light(Vector3 pos, Vector3 col, float attenuationRadius, float initialTick)
        {
            position = pos;
            color = col;
            this.attenuationRadius = attenuationRadius;
            this.currentExplosionTick = initialTick;
        }
    }
}
