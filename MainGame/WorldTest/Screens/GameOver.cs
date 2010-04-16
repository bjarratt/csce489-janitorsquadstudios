#region File Description
// Own Class Adapted from Microsofts examples
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace WorldTest
{
    class GameOver : GameScreen
    {
        // Texture for the picture
        Texture2D gameOverScreen;

        // Are the other screens gone
        bool otherScreensAreGone;

        // List of screens to load
        GameScreen[] screensToLoad;

        // Number of frames to wait... used to keep the gameover screen up for a longer time
        private int waitingFrames;

        // Private constructor so it only gets called when the Game Over condition occurs
        // and the Load static method is subsequently called...
        private GameOver(ScreenManager screenManager,
                              GameScreen[] screensToLoad)
        {
            this.screensToLoad = screensToLoad;
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(2.0);
            this.waitingFrames = 60 * 5;
        }

        public static void Load(ScreenManager screenManager,
                                PlayerIndex? controllingPlayer,
                                params GameScreen[] screensToLoad)
        {
            // Tell all the current screens to transition off.
            foreach (GameScreen screen in screenManager.GetScreens())
                screen.ExitScreen();

            // Create and activate the loading screen.
            GameOver gameoverscreen = new GameOver(screenManager, screensToLoad);

            screenManager.AddScreen(gameoverscreen, controllingPlayer);
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            gameOverScreen = content.Load<Texture2D>("game_over");
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // If all the previous screens have finished transitioning
            // off, it is time to actually perform the load.
            if (otherScreensAreGone)
            {
                this.waitingFrames--;

                if (this.waitingFrames <= 0)
                {
                    ScreenManager.RemoveScreen(this);

                    foreach (GameScreen screen in screensToLoad)
                    {
                        if (screen != null)
                        {
                            ScreenManager.AddScreen(screen, ControllingPlayer);
                        }
                    }

                    // Once the load has finished, we use ResetElapsedTime to tell
                    // the  game timing mechanism that we have just finished a very
                    // long frame, and that it should not try to catch up.
                    ScreenManager.Game.ResetElapsedTime();
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if ((ScreenState == ScreenState.Active) &&
                (ScreenManager.GetScreens().Length == 1))
            {
                otherScreensAreGone = true;
            }

            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.Begin();
            spriteBatch.Draw(gameOverScreen, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            spriteBatch.End();
        }

    }
}
