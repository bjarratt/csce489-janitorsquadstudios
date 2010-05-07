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

        private bool freezeWhileNarrating = true;
        private bool saveWhenReached = false;
        private bool saveNeeded = false;

        private bool checkpointReached = false;

        private bool isFinalCheckpoint = false;

        // The dimension the checkpoint is available in (0 means both)
        private int dimension = 0;

        public bool FreezeControls
        {
            get { return freezeWhileNarrating && checkpointReached && !narration.NarrationFinished; }
        }

        public bool SaveNeeded
        {
            get { return saveNeeded; }
            set { saveNeeded = value; }
        }

        public int NavMeshIndex
        {
            get { return navMeshIndex; }
        }

        public bool FinalCheckpointReached
        {
            get { return isFinalCheckpoint && checkpointReached; }
        }

        public Checkpoint(int navMeshIndex, string narrationFilename, int dimension, SpriteFont narrationFont, bool freezeWhileNarrating, bool saveWhenReached, bool isFinalCheckpoint)
        {
            this.navMeshIndex = navMeshIndex;
            this.freezeWhileNarrating = freezeWhileNarrating;
            this.saveWhenReached = saveWhenReached;
            this.isFinalCheckpoint = isFinalCheckpoint;

            this.dimension = dimension;

            narration = new Narration(narrationFilename, narrationFont, Vector2.One * 20.0f);
        }

        public void LoadContent()
        {
            narration.LoadContent();
        }

        public bool Update(float elapsedSeconds, int playerNavIndex, Dimension playerDimension)
        {
            if (playerNavIndex == this.navMeshIndex && !this.checkpointReached && InCorrectDimension(playerDimension))
            {
                if (saveWhenReached)
                {
                    saveNeeded = true;
                }

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

        private bool InCorrectDimension(Dimension playerDimension)
        {
            if (this.dimension == 0 ||
                playerDimension == Dimension.FIRST && this.dimension == 1 ||
                playerDimension == Dimension.SECOND && this.dimension == 2)
            {
                return true;
            }
            else
            {
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
