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
    class Checkpoint
    {
        private int navMeshIndex;
        private Narration narration;

        private bool checkpointReached = false;

        public int NavMeshIndex
        {
            get { return navMeshIndex; }
        }

        public Checkpoint(int navMeshIndex, string narrationFilename, SpriteFont narrationFont)
        {
            this.navMeshIndex = navMeshIndex;

            narration = new Narration(narrationFilename, narrationFont, Vector2.One * 20.0f);
        }

        public void LoadContent()
        {
            narration.LoadContent();
        }

        public bool Update(float elapsedSeconds, int playerNavIndex)
        {
            if (playerNavIndex == this.navMeshIndex && !this.checkpointReached)
            {
                this.checkpointReached = true;
                narration.StartNarration();
                narration.Update(elapsedSeconds);
                return true;
            }
            else
            {
                narration.Update(elapsedSeconds);
                return false;
            }
        }

        public void Draw(ref SpriteBatch spriteBatch)
        {
            if (this.checkpointReached)
            {
                narration.Draw(ref spriteBatch);
            }
        }
    }
}
