#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
#endregion

namespace caveGame
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

        enum Option
        {
            Toony,
            reallyToony,
            Llama,
        }

        static Option currentShader = Option.Toony;

        static string[] languages = { "English", "French", "Spanish" };
        static int currentLanguage = 0;

        static bool antialias = true;

        static bool sfx = true;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public InGameOptionsScreen()
            : base("Options")
        {
            // Flag that there is no need for the game to transition
            // off when the pause menu is on top of it.
            IsPopup = true;

            // Create our menu entries.
            MenuEntry1 = new MenuEntry(string.Empty);
            MenuEntry2 = new MenuEntry(string.Empty);
            MenuEntry3 = new MenuEntry(string.Empty);
            MenuEntry4 = new MenuEntry(string.Empty);
            MenuEntry backMenuEntry = new MenuEntry("Back");

            SetMenuEntryText();

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
            MenuEntry1.Text = "Shader: " + currentShader;
            MenuEntry2.Text = "Language: " + languages[currentLanguage];
            MenuEntry3.Text = "Anti-aliasing: " + (antialias ? "on" : "off"); ;
            MenuEntry4.Text = "Sound FX: " + (sfx ? "on" : "off"); ;
        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the 1st menu entry is selected.
        /// </summary>
        void MenuEntry1Selected(object sender, PlayerIndexEventArgs e)
        {
            currentShader++;

            if (currentShader > Option.Llama)
                currentShader = 0;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 2nd menu entry is selected.
        /// </summary>
        void MenuEntry2Selected(object sender, PlayerIndexEventArgs e)
        {
            currentLanguage = (currentLanguage + 1) % languages.Length;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 3rd menu entry is selected.
        /// </summary>
        void MenuEntry3Selected(object sender, PlayerIndexEventArgs e)
        {
            antialias = !antialias;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the 4th menu entry is selected.
        /// </summary>
        void MenuEntry4Selected(object sender, PlayerIndexEventArgs e)
        {
            sfx = !sfx;

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
