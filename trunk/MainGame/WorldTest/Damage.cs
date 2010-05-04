using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace WorldTest
{
    class Damage : Sprite
    {
        const string DAMAGE_ASSET_NAME = "splatter";
        Game game;
        //give the bar a reference to the player so it knows the new health at each update
        private float health;
        Texture2D blood;
        Texture2D border;
        int mAlphaValue = 255;
        int healthAlpha = 0;
        int mFadeIncrement = 3;
        double mFadeDelay = .02;

        public Damage(ref Game game1)
        {
            game = game1;
        }

        public void LoadContent(ContentManager theContentManager)
        {
            base.LoadContent(theContentManager, DAMAGE_ASSET_NAME);
            this.Scale = 0.5f;
            blood = theContentManager.Load<Texture2D>("splatter");
            border = theContentManager.Load<Texture2D>("damage_border");
        }

        public void ResetAlpha()
        {
            this.mAlphaValue = 255;
        }

        public void Update(GameTime theGameTime, ref Player player)
        {
            // Player recovers health fully from 0 in 7 seconds (@60 fps).
            player.health += 0.089524f;

            if (player.health > 100) player.health = 100;
            health = player.health;
            //Decrement the delay by the number of seconds that have elapsed since
            //the last time that the Update method was called
            mFadeDelay -= theGameTime.ElapsedGameTime.TotalSeconds;
            //If the Fade delays has dropped below zero, then it is time to 
            //fade in/fade out the image a little bit more.
            if (mFadeDelay <= 0)
            {
                //Reset the Fade delay
                mFadeDelay = .035;
                //Increment/Decrement the fade value for the image
                //mAlphaValue -= mFadeIncrement;
                //If the AlphaValue is equal or above the max Alpha value or
                //has dropped below or equal to the min Alpha value, then 
                //reverse the fade
                //if (mAlphaValue <= 0)
                //{
                //    mAlphaValue = 255;
                //}
            }
            if (player.isHit == true) // If player has taken damage
            {
                mAlphaValue -= mFadeIncrement;
                //mAlphaValue = 0;
            }

            if (mAlphaValue <= 0 && player.isHit) // If player has regained health
            {
                //player.health = 100.0f;
                player.isHit = false;
                mAlphaValue = 255;
            }

            healthAlpha = 255 - (int)Math.Truncate((health * 0.01) * 255);

            base.Update(theGameTime);
        }

        public void Draw(SpriteBatch theSpriteBatch, ref Player player, PresentationParameters pres)
        {
            if (player.isHit)
            {
                theSpriteBatch.Draw(blood, new Rectangle(0, 0, pres.BackBufferWidth, pres.BackBufferHeight), new Color(255, 255, 255, (byte)MathHelper.Clamp(mAlphaValue, 0, 255)));
            }
            theSpriteBatch.Draw(border, new Rectangle(-20, -40, pres.BackBufferWidth + 60, pres.BackBufferHeight + 40), new Color(255, 255, 255, (byte)MathHelper.Clamp(healthAlpha, 0, 255)));
        }
    }
}
