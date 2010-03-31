#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
#endregion

namespace WorldTest
{
    /// <summary>
    /// The pause menu comes up over the top of the game,
    /// giving the player options to resume or quit.
    /// </summary>
    class InGameOptionsScreen : MenuScreen
    {

        #region Fields

        MenuEntry MenuEntry1;
        MenuEntry MenuEntry2;
        MenuEntry MenuEntry3;
        MenuEntry MenuEntry4;
        MenuEntry MenuEntry5;

        public struct Resolution
        {
            public int width;
            public int height;

            public Resolution(int w, int h)
            {
                width = w;
                height = h;
            }
        }

        private List<Resolution> validResolutions;
        private int currentResolution;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public InGameOptionsScreen(ScreenManager sm)
            : base("Options", sm)
        {
            // Flag that there is no need for the game to transition
            // off when the pause menu is on top of it.
            IsPopup = true;

            this.validResolutions = new List<Resolution>();

            this.validResolutions.Add(new Resolution(800, 600));

            if (screenManager.GraphicsDevice.DisplayMode.Width >= 1024 &&
                screenManager.GraphicsDevice.DisplayMode.Height >= 768)
            {
                this.validResolutions.Add(new Resolution(1024, 768));

                if (screenManager.GraphicsDevice.DisplayMode.Width >= 1280 &&
                    screenManager.GraphicsDevice.DisplayMode.Height >= 1024)
                {
                    this.validResolutions.Add(new Resolution(1280, 1024));

                    if (screenManager.GraphicsDevice.DisplayMode.Width >= 1440 &&
                        screenManager.GraphicsDevice.DisplayMode.Height >= 900)
                    {
                        this.validResolutions.Add(new Resolution(1440, 900));

                        if (screenManager.GraphicsDevice.DisplayMode.Width >= 1680 &&
                            screenManager.GraphicsDevice.DisplayMode.Height >= 1050)
                        {
                            this.validResolutions.Add(new Resolution(1680, 1050));
                        }
                    }
                }
            }

            this.currentResolution = 0;

            for (int i = 0; i < this.validResolutions.Count; i++)
            {
                if (screenManager.GraphicsDevice.Viewport.Width == this.validResolutions[i].width &&
                    screenManager.GraphicsDevice.Viewport.Height == this.validResolutions[i].height)
                {
                    this.currentResolution = i;
                }
            }

            // Create our menu entries.
            MenuEntry1 = new MenuEntry(string.Empty);
            MenuEntry2 = new MenuEntry(string.Empty);
            MenuEntry3 = new MenuEntry(string.Empty);
            MenuEntry4 = new MenuEntry(string.Empty);
            MenuEntry5 = new MenuEntry(string.Empty);
            MenuEntry backMenuEntry = new MenuEntry("Back");

            SetMenuEntryText();

            // Hook up menu event handlers.
            MenuEntry1.Selected += MenuEntry1Selected;
            MenuEntry2.Selected += MenuEntry2Selected;
            MenuEntry3.Selected += MenuEntry3Selected;
            MenuEntry4.Selected += MenuEntry4Selected;
            MenuEntry5.Selected += MenuEntry5Selected;
            backMenuEntry.Selected += OnCancel;


            // Add entries to the menu.
            MenuEntries.Add(MenuEntry1);
            MenuEntries.Add(MenuEntry2);
            MenuEntries.Add(MenuEntry3);
            MenuEntries.Add(MenuEntry4);
            MenuEntries.Add(MenuEntry5);
            MenuEntries.Add(backMenuEntry);
        }

        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            if (ScreenManager == null)
            {
                MenuEntry1.Text = "Resolution: " + screenManager.GraphicsDevice.Viewport.Width + "x" + screenManager.GraphicsDevice.Viewport.Height;
            }
            else
            {
                MenuEntry1.Text = "Resolution: " + ScreenManager.graphics.PreferredBackBufferWidth + "x" + ScreenManager.graphics.PreferredBackBufferHeight;
            }
            MenuEntry2.Text = "Fullscreen: " + (screenManager.graphics.IsFullScreen ? "on" : "off");
            MenuEntry3.Text = "Invert Y axis: " + (GameplayScreen.invertYAxis ? "on" : "off");

            string aaText = "";
            if (OptionsMenuScreen.CURRENT_AA_SETTING == MultiSampleType.TwoSamples)
            {
                aaText = "2X";
            }
            else if (OptionsMenuScreen.CURRENT_AA_SETTING == MultiSampleType.FourSamples)
            {
                aaText = "4X";
            }
            else if (OptionsMenuScreen.CURRENT_AA_SETTING == MultiSampleType.EightSamples)
            {
                aaText = "8X";
            }
            else
            {
                aaText = "off";
            }

            MenuEntry4.Text = "Antialiasing: " + aaText;

            MenuEntry5.Text = "Apply Changes";
        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the 1st menu entry is selected.
        /// </summary>
        void MenuEntry1Selected(object sender, PlayerIndexEventArgs e)
        {
            currentResolution = (currentResolution + 1) % this.validResolutions.Count;

            ScreenManager.graphics.PreferredBackBufferWidth = this.validResolutions[currentResolution].width;
            ScreenManager.graphics.PreferredBackBufferHeight = this.validResolutions[currentResolution].height;

            //screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 2nd menu entry is selected.
        /// </summary>
        void MenuEntry2Selected(object sender, PlayerIndexEventArgs e)
        {
            screenManager.graphics.IsFullScreen = !screenManager.graphics.IsFullScreen;

            //screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 3rd menu entry is selected.
        /// </summary>
        void MenuEntry3Selected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.invertYAxis = !GameplayScreen.invertYAxis;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 4th menu entry is selected.
        /// </summary>
        void MenuEntry4Selected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.graphics.GraphicsDevice.PresentationParameters.MultiSampleType = MultiSampleType.None;

            ScreenManager.graphics.PreferMultiSampling = true;

            for (int i = 0; i < OptionsMenuScreen.AA_SETTINGS.Length; i++)
            {
                if (OptionsMenuScreen.CURRENT_AA_SETTING == OptionsMenuScreen.AA_SETTINGS[i])
                {
                    OptionsMenuScreen.CURRENT_AA_SETTING = OptionsMenuScreen.AA_SETTINGS[(i + 1) % OptionsMenuScreen.AA_SETTINGS.Length];
                    break;
                }
            }

            if (OptionsMenuScreen.CURRENT_AA_SETTING == MultiSampleType.None)
            {
                ScreenManager.graphics.PreferMultiSampling = false;
            }

            ScreenManager.graphics.GraphicsDevice.PresentationParameters.MultiSampleType = OptionsMenuScreen.CURRENT_AA_SETTING;

            //ScreenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 5th menu entry is selected.
        /// </summary>
        void MenuEntry5Selected(object sender, PlayerIndexEventArgs e)
        {
            screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }

        #endregion

        #region Draw


        /// <summary>
        /// Draws the pause menu screen. This darkens down the gameplay screen
        /// that is underneath us, and then chains to the base MenuScreen.Draw.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 3 / 3);

            base.Draw(gameTime);
        }


        #endregion
    }
}
