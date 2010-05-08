#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
#endregion

namespace CaveOfShadows
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        #region Fields

        List<MenuEntry> optionsMenuEntries = new List<MenuEntry>();
        int selectedEntry = 0;
        string menuTitle = "Options";

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

        private bool changesApplied = false;

        MenuEntry MenuEntry1;
        MenuEntry MenuEntry2;
        MenuEntry MenuEntry3;
        MenuEntry MenuEntry4;
        MenuEntry MenuEntry5;

        public static MultiSampleType[] AA_SETTINGS = {
                                                          MultiSampleType.None,
                                                          MultiSampleType.TwoSamples,
                                                          MultiSampleType.FourSamples,
                                                          MultiSampleType.EightSamples
                                                      };

        public static MultiSampleType CURRENT_AA_SETTING = MultiSampleType.None;

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> OptionsMenuEntries
        {
            get { return optionsMenuEntries; }
        }

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

            SetMenuEntryText();

            MenuEntry backMenuEntry = new MenuEntry("Back");

            // Hook up menu event handlers.
            MenuEntry1.Selected += MenuEntry1Selected;
            MenuEntry2.Selected += MenuEntry2Selected;
            MenuEntry3.Selected += MenuEntry3Selected;
            MenuEntry4.Selected += MenuEntry4Selected;
            MenuEntry5.Selected += MenuEntry5Selected;
            backMenuEntry.Selected += OnCancel;
            
            // Add entries to the menu.
            OptionsMenuEntries.Add(MenuEntry1);
            OptionsMenuEntries.Add(MenuEntry2);
            OptionsMenuEntries.Add(MenuEntry3);
            OptionsMenuEntries.Add(MenuEntry4);
            OptionsMenuEntries.Add(MenuEntry5);
            OptionsMenuEntries.Add(backMenuEntry);
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

            if (changesApplied)
            {
                MenuEntry5.Text = "Changes Applied";
            }
            else
            {
                MenuEntry5.Text = "Apply Changes";
            }
        }


        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or cancelling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            // Move to the previous menu entry?
            if (input.IsMenuUp(ControllingPlayer))
            {
                selectedEntry--;

                if (selectedEntry < 0)
                    selectedEntry = optionsMenuEntries.Count - 1;
            }

            // Move to the next menu entry?
            if (input.IsMenuDown(ControllingPlayer))
            {
                selectedEntry++;

                if (selectedEntry >= optionsMenuEntries.Count)
                    selectedEntry = 0;
            }

            // Accept or cancel the menu? We pass in our ControllingPlayer, which may
            // either be null (to accept input from any player) or a specific index.
            // If we pass a null controlling player, the InputState helper returns to
            // us which player actually provided the input. We pass that through to
            // OnSelectEntry and OnCancel, so they can tell which player triggered them.
            PlayerIndex playerIndex;

            if (input.IsMenuSelect(ControllingPlayer, out playerIndex))
            {
                OnSelectEntry(selectedEntry, playerIndex);
            }
            else if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
            {
                OnCancel(playerIndex);
            }
        }


        /// <summary>
        /// Handler for when the user has chosen a menu entry.
        /// </summary>
        protected override void OnSelectEntry(int entryIndex, PlayerIndex playerIndex)
        {
            optionsMenuEntries[selectedEntry].OnSelectEntry(playerIndex);
        }


        /// <summary>
        /// Handler for when the user has cancelled the menu.
        /// </summary>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }


        /// <summary>
        /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
        /// </summary>
        protected override void OnCancel(object sender, PlayerIndexEventArgs e)
        {
            OnCancel(e.PlayerIndex);
        }

        /// <summary>
        /// Event handler for when the resolution menu entry is selected.
        /// </summary>
        void MenuEntry1Selected(object sender, PlayerIndexEventArgs e)
        {
            currentResolution = (currentResolution + 1) % this.validResolutions.Count;

            ScreenManager.graphics.PreferredBackBufferWidth = this.validResolutions[currentResolution].width;
            ScreenManager.graphics.PreferredBackBufferHeight = this.validResolutions[currentResolution].height;

            changesApplied = false;
            //screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the fullscreen menu entry is selected.
        /// </summary>
        void MenuEntry2Selected(object sender, PlayerIndexEventArgs e)
        {
            screenManager.graphics.IsFullScreen = !screenManager.graphics.IsFullScreen;

            changesApplied = false;
            //screenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the y-axis inversion menu entry is selected.
        /// </summary>
        void MenuEntry3Selected(object sender, PlayerIndexEventArgs e)
        {
            GameplayScreen.invertYAxis = !GameplayScreen.invertYAxis;

            changesApplied = false;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the antialiasing menu entry is selected.
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

            changesApplied = false;
            //ScreenManager.graphics.ApplyChanges();

            SetMenuEntryText();
        }

        /// <summary>
        /// Event handler for when the apply changes entry is selected.
        /// </summary>
        void MenuEntry5Selected(object sender, PlayerIndexEventArgs e)
        {
            screenManager.graphics.ApplyChanges();

            changesApplied = true;

            SetMenuEntryText();
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the menu.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Update each nested MenuEntry object.
            for (int i = 0; i < optionsMenuEntries.Count; i++)
            {
                bool isSelected = IsActive && (i == selectedEntry);

                optionsMenuEntries[i].Update(this, isSelected, gameTime);
            }
        }

        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            Vector2 position = setPosition(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth-
                                           15*screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth/16,
                                            screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight-
                                            10*screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight/12);

            spriteBatch.Begin();

            // Draw each menu entry in turn.
            for (int i = 0; i < optionsMenuEntries.Count; i++)
            {
                MenuEntry menuEntry = optionsMenuEntries[i];

                bool isSelected = IsActive && (i == selectedEntry);

                menuEntry.Draw(this, position, isSelected, gameTime);

                position.Y += menuEntry.GetHeight(this);
            }

            // Draw the menu title.
            Vector2 titlePosition = setPosition(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth-
                                                5*screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth/6,
                                                 screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight-
                                                 11*screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight/12);
            Vector2 titleOrigin = font.MeasureString(menuTitle) * 0.5f;
            Color titleColor = new Color(192, 192, 192, TransitionAlpha);
            float titleScale = 1.25f;
            
            spriteBatch.DrawString(font, menuTitle, titlePosition, titleColor, 0,
                                   titleOrigin, titleScale, SpriteEffects.None, 0);
            spriteBatch.End();
        }

        #endregion
    }
}
