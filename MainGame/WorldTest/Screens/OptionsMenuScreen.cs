#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System.Collections.Generic;
#endregion

namespace WorldTest
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        #region Fields

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

        MenuEntry MenuEntry1;
        MenuEntry MenuEntry2;
        MenuEntry MenuEntry3;
        MenuEntry MenuEntry4;

        enum Shader
        {
            Toony,
            reallyToony,
            Llama,
        }

        static Shader currentShader = Shader.Toony;

        static string[] languages = { "English", "French", "Spanish" };
        static int currentLanguage = 0;

        static bool antialias = true;

        static bool sfx = true;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen(ScreenManager sm)
            : base("Options", sm)
        {
            this.validResolutions = new List<Resolution>();

            this.validResolutions.Add(new Resolution(800, 600));
            this.validResolutions.Add(new Resolution(1024, 768));
            this.validResolutions.Add(new Resolution(1280, 1024));
            this.validResolutions.Add(new Resolution(1440, 900));
            this.validResolutions.Add(new Resolution(1680, 1050));

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

            SetMenuEntryText();

            MenuEntry backMenuEntry = new MenuEntry("Back");

            // Hook up menu event handlers.
            MenuEntry1.Selected += MenuEntry1Selected;
            MenuEntry2.Selected += MenuEntry2Selected;
            MenuEntry3.Selected += MenuEntry3Selected;
            MenuEntry4.Selected += MenuEntry4Selected;
            backMenuEntry.Selected += OnCancel;
            
            // Add entries to the menu.
            MenuEntries.Add(MenuEntry1);
            MenuEntries.Add(MenuEntry2);
            MenuEntries.Add(MenuEntry3);
            MenuEntries.Add(MenuEntry4);
            MenuEntries.Add(backMenuEntry);
        }


        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            MenuEntry1.Text = "Resolution: " + screenManager.GraphicsDevice.Viewport.Width + "x" + screenManager.GraphicsDevice.Viewport.Height;
            MenuEntry2.Text = "Fullscreen: " + (screenManager.graphics.IsFullScreen ? "on" : "off");
            MenuEntry3.Text = "Anti-aliasing: " + (antialias ? "on" : "off"); ;
            MenuEntry4.Text = "Sound FX: " + (sfx ? "on" : "off"); ;
        }


        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the Ungulate menu entry is selected.
        /// </summary>
        void MenuEntry1Selected(object sender, PlayerIndexEventArgs e)
        {
            currentResolution = (currentResolution + 1) % this.validResolutions.Count;

            ScreenManager.graphics.PreferredBackBufferWidth = this.validResolutions[currentResolution].width;
            ScreenManager.graphics.PreferredBackBufferHeight = this.validResolutions[currentResolution].height;

            screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void MenuEntry2Selected(object sender, PlayerIndexEventArgs e)
        {
            screenManager.graphics.IsFullScreen = !screenManager.graphics.IsFullScreen;

            screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Frobnicate menu entry is selected.
        /// </summary>
        void MenuEntry3Selected(object sender, PlayerIndexEventArgs e)
        {
            antialias = !antialias;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Elf menu entry is selected.
        /// </summary>
        void MenuEntry4Selected(object sender, PlayerIndexEventArgs e)
        {
            sfx = !sfx;

            SetMenuEntryText();
        }


        #endregion
    }
}
