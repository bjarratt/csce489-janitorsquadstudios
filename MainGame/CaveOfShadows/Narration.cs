using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
    class NarrationClip
    {
        public string text;
        public float duration;
        public float remaining;
    }

    class Narration
    {
        private List<NarrationClip> clips;

        private int currentClipIndex;

        private bool isPlaying;

        private bool isFinished;

        private string filename;

        private SpriteFont font;

        private Vector2 position;

        public bool NarrationFinished
        {
            get { return this.isFinished; }
        }

        public Narration(string filename, SpriteFont font, Vector2 position)
        {
            this.isPlaying = false;
            this.isFinished = false;
            this.currentClipIndex = -1;
            this.filename = filename;

            this.font = font;

            this.position = position;

            this.clips = new List<NarrationClip>();
        }

        public void LoadContent()
        {
            FileStream narrationFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader narrationFileReader = new StreamReader(narrationFile);

            string line = narrationFileReader.ReadLine();
            string[] splitLine;
            char[] splitChars = { ' ' };

            while (line != null)
            {
                if (line == "" || line == "\n")
                {
                    line = narrationFileReader.ReadLine();
                    continue;
                }

                NarrationClip clip = new NarrationClip();

                splitLine = line.Split(splitChars, 2);

                clip.duration = (float)Convert.ToDouble(splitLine[0]);
                clip.text = splitLine[1];
                clip.remaining = clip.duration;

                this.clips.Add(clip);

                line = narrationFileReader.ReadLine();
            }

            narrationFile.Close();

            this.currentClipIndex = 0;
        }

        public void StartNarration()
        {
            this.currentClipIndex = 0;

            this.isPlaying = true;
            this.isFinished = false;
        }

        public void Update(float elapsedSeconds)
        {
            if (this.isPlaying && !this.isFinished)
            {
                if (this.currentClipIndex < 0)
                {
                    this.isPlaying = false;
                    return;
                }

                this.clips[this.currentClipIndex].remaining -= elapsedSeconds;

                if (this.clips[this.currentClipIndex].remaining <= 0.0f)
                {
                    this.currentClipIndex += 1;
                }

                if (this.currentClipIndex >= this.clips.Count)
                {
                    this.isFinished = true;
                }
            }
        }

        public void Draw(ref SpriteBatch spriteBatch)
        {
            if (this.isPlaying && !this.isFinished)
            {
                Color color = Color.White;

                float scale = 1;

                Vector2 origin = new Vector2(0, font.LineSpacing / 2);

                spriteBatch.DrawString(this.font, this.clips[this.currentClipIndex].text, this.position, color, 0,
                                       origin, scale, SpriteEffects.None, 0);
            }
        }
    }
}
