using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

using XNAnimation;
using XNAnimation.Controllers;
using XNAnimation.Effects;

namespace WorldTest
{
    class ToolTips
    {
        public Texture2D backButton;
        public Texture2D startButton;
        public Texture2D YButton;
        public Texture2D XButton;
        public Texture2D AButton;
        public Texture2D BButton;
        public Texture2D RBumper;
        public Texture2D LBumper;
        public Texture2D RTrigger;
        public Texture2D LTrigger;

        public Texture2D attackHud;
        public bool drawAttackHud;
        public List<Texture2D> displayList;
        public List<bool> boolList;


        public ToolTips()
        {
            displayList = new List<Texture2D>();
            boolList = new List<bool>();
        }

        public void LoadContent(GraphicsDevice theDevice, ContentManager content)
        {
            backButton = content.Load<Texture2D>("button_back");
            startButton = content.Load<Texture2D>("button_start");
            YButton = content.Load<Texture2D>("button_y");
            XButton = content.Load<Texture2D>("button_x");
            AButton = content.Load<Texture2D>("button_a");
            BButton = content.Load<Texture2D>("button_b");
            RBumper = content.Load<Texture2D>("bumper_right");
            LBumper = content.Load<Texture2D>("bumper_left");
            RTrigger = content.Load<Texture2D>("trigger_right");
            LTrigger = content.Load<Texture2D>("trigger_left");
            attackHud = content.Load<Texture2D>("attack_hud");

            displayList.Add(backButton);
            displayList.Add(startButton);
            displayList.Add(YButton);
            displayList.Add(XButton);
            displayList.Add(AButton);
            displayList.Add(BButton);
            displayList.Add(RBumper);
            displayList.Add(LBumper);
            displayList.Add(RTrigger);
            displayList.Add(LTrigger);

            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);
            boolList.Add(false);

            drawAttackHud = false;
        }

        public void Update(GameTime theGameTime, ref Player player, ref Level theLevel, ControlState state, ref List<Portal> dimensionPortals)
        {
            boolList[4] = false;
            
            for (int i = 0; i < dimensionPortals.Count; i++)
            {
                if (player.RayCircleIntersect(new Ray(player.position + new Vector3(0, 50, 0), Vector3.Down), dimensionPortals[i].Origin, dimensionPortals[i].Radius))
                {
                    boolList[4] = true;
                    break;
                }
            }

            if (state.currentGamePadState.Triggers.Right != 0) drawAttackHud = true;
            else drawAttackHud = false;
        }

        public void Draw(SpriteBatch theSpriteBatch, PresentationParameters currentParams)
        {
            for (int i = 0; i < displayList.Count; i++)
            {
                if (boolList[i] == true)
                {
                    theSpriteBatch.Draw(displayList[i], new Rectangle((int)(currentParams.BackBufferWidth * 0.5f) - 20, 
                        currentParams.BackBufferHeight - 80, 64, 64), Color.White);
                }
                if (drawAttackHud == true)
                {
                    theSpriteBatch.Draw(attackHud, new Rectangle(20, currentParams.BackBufferHeight - 150, 120, 120), Color.White);
                }
            }
        }
    }
}
