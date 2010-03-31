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

namespace WorldTest
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

        public Light(Vector3 pos, Vector3 col, float attenuationRadius)
        {
            position = pos;
            color = col;
            this.attenuationRadius = attenuationRadius;
        }

        public Light(Vector3 pos, Vector3 col, float attenuationRadius, float initialTick)
        {
            position = pos;
            color = col;
            this.attenuationRadius = attenuationRadius;
            this.currentExplosionTick = initialTick;
        }

        public void setPosition(Vector3 pos)
        {
            this.position = pos;
        }
    }
}
