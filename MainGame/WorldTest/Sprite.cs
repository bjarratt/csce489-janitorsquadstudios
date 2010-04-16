using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace WorldTest
{
    class Sprite
    {
        const int MAX_WINX = 800;
        const int MAX_WINY = 750;
        //rotation in rads
        public float rotation;
        //origin of rotation
        public Vector2 origin = Vector2.Zero;
        //asset name for the Sprites texture
        public string AssetName;

        //The size of the Sprite        
        public Rectangle Size;

        Rectangle mSource;
        public Rectangle Source
        {
            get { return mSource; }
            set
            {
                mSource = value;
                Size = new Rectangle(0, 0, (int)(mSource.Width * Scale), (int)(mSource.Height * Scale));
            }
        }

        //Used to size the Sprite up or down from the original image        
        public float mScale = 1.0f;

        public float Scale
        {
            get { return mScale; }
            set
            {
                mScale = value;
                Size = new Rectangle(0, 0, (int)(mSource.Width * Scale), (int)(mSource.Height * Scale));
            }
        }

        //The current position of the Sprite
        public Vector2 Position = new Vector2(0, 0);

        //The texture object used when drawing the sprite
        public Texture2D mSpriteTexture;

        //Load the texture for the sprite using the Content Pipeline
        public void LoadContent(ContentManager theContentManager, string theAssetName)
        {
            mSpriteTexture = theContentManager.Load<Texture2D>(theAssetName);
            AssetName = theAssetName;
            Source = new Rectangle(0, 0, mSpriteTexture.Width, mSpriteTexture.Height);
            Size = new Rectangle(0, 0, (int)(mSpriteTexture.Width * Scale), (int)(mSpriteTexture.Height * Scale));
        }

        public void Update(GameTime theGameTime)
        {
            if ((Position.X + Size.Width) >= MAX_WINX)
                Position.X = MAX_WINX - Size.Width;
            if (Position.X <= 0)
                Position.X = 0;
            if (Position.Y <= 0)
                Position.Y = 0;
            if ((Position.Y + Size.Height) >= MAX_WINY)
                Position.Y = MAX_WINY - Size.Height;
        }

        //Draw the sprite to the screen
        public virtual void Draw(SpriteBatch theSpriteBatch)
        {
            theSpriteBatch.Draw(mSpriteTexture, Position, Source,
                Color.White, rotation, origin, Scale, SpriteEffects.None, 0);
        }
    }
}