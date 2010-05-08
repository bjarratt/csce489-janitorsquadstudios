#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System.IO;
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
    class LoadingMenuScreen : MenuScreen
    {
        #region Fields

        List<MenuEntry> loadingMenuEntries = new List<MenuEntry>();
        int selectedEntry = 0;
        string menuTitle = "Load Game";

        MenuEntry MenuEntry1;
        MenuEntry MenuEntry2;
        MenuEntry MenuEntry3;
        //MenuEntry MenuEntry4;

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> LoadingMenuEntries
        {
            get { return loadingMenuEntries; }
        }

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public LoadingMenuScreen(ScreenManager sm)
            : base("Load Game", sm)
        {
            // Create our menu entries.
            MenuEntry1 = new MenuEntry(string.Empty);
            MenuEntry2 = new MenuEntry(string.Empty);
            MenuEntry3 = new MenuEntry(string.Empty);
           //MenuEntry4 = new MenuEntry(string.Empty);

            SetMenuEntryText();

            MenuEntry backMenuEntry = new MenuEntry("Back");

            // Hook up menu event handlers.
            MenuEntry1.Selected += MenuEntry1Selected;
            MenuEntry2.Selected += MenuEntry2Selected;
            MenuEntry3.Selected += MenuEntry3Selected;
            //MenuEntry4.Selected += MenuEntry4Selected;
            backMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            LoadingMenuEntries.Add(MenuEntry1);
            LoadingMenuEntries.Add(MenuEntry2);
            LoadingMenuEntries.Add(MenuEntry3);
            //LoadingMenuEntries.Add(MenuEntry4);
            LoadingMenuEntries.Add(backMenuEntry);
        }

        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            MenuEntry1.Text = "Save File 1";
            MenuEntry2.Text = "Save File 2";
            MenuEntry3.Text = "Save File 3";
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
                    selectedEntry = loadingMenuEntries.Count - 1;
            }

            // Move to the next menu entry?
            if (input.IsMenuDown(ControllingPlayer))
            {
                selectedEntry++;

                if (selectedEntry >= loadingMenuEntries.Count)
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
            loadingMenuEntries[selectedEntry].OnSelectEntry(playerIndex);
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
            try
            {
                StreamReader reader = new StreamReader("save1.txt");
                LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                                   new GameplayScreen(this.ScreenManager, "save1.txt"));
            }
            catch (FileNotFoundException ex)
            {
                System.Windows.Forms.MessageBox.Show("This file does not exist!", "Load Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }
            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the fullscreen menu entry is selected.
        /// </summary>
        void MenuEntry2Selected(object sender, PlayerIndexEventArgs e)
        {
            try
            {
                StreamReader reader = new StreamReader("save2.txt");
                LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                                   new GameplayScreen(this.ScreenManager, "save2.txt"));
            }
            catch (FileNotFoundException ex) 
            {
                System.Windows.Forms.MessageBox.Show("This file does not exist!", "Load Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }
            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the y-axis inversion menu entry is selected.
        /// </summary>
        void MenuEntry3Selected(object sender, PlayerIndexEventArgs e)
        {
            try
            {
                StreamReader reader = new StreamReader("save3.txt");
                LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                                   new GameplayScreen(this.ScreenManager, "save3.txt"));
            }
            catch (FileNotFoundException ex)
            {
                System.Windows.Forms.MessageBox.Show("This file does not exist!", "Load Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }
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
            for (int i = 0; i < loadingMenuEntries.Count; i++)
            {
                bool isSelected = IsActive && (i == selectedEntry);

                loadingMenuEntries[i].Update(this, isSelected, gameTime);
            }
        }

        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            Vector2 position = setPosition(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth -
                                           15 * screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 16,
                                            screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight -
                                            10 * screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 12);

            spriteBatch.Begin();

            // Draw each menu entry in turn.
            for (int i = 0; i < loadingMenuEntries.Count; i++)
            {
                MenuEntry menuEntry = loadingMenuEntries[i];

                bool isSelected = IsActive && (i == selectedEntry);

                menuEntry.Draw(this, position, isSelected, gameTime);

                position.Y += menuEntry.GetHeight(this);
            }

            // Draw the menu title.
            Vector2 titlePosition = setPosition(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth -
                                                7 * screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 9,
                                                 screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight -
                                                 11 * screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 12);

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
