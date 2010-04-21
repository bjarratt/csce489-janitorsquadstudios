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
    enum Dimension
    {
        FIRST,
        SECOND
    }

    abstract class Agent
    {

        #region Properties

        protected GraphicsDeviceManager graphics;
        protected ContentManager content;

        public Vector3 position;
        public Vector3 velocity;

        /// <summary>
        /// These keep track of which polygons the agent is on in the NavigationMesh.
        /// </summary>
        public int current_poly_index;
        public int prev_poly_index;

        protected float speed; // In meters per second
        protected float speedScale; // Scaling factor for speed (to make speed be actual m/s)
        protected float movement_speed_reg;
        protected Vector3 previousVelocity; // Velocity of previous frame

        protected float rotation;
        protected float turn_speed;
        protected float turn_speed_reg;

        public Quaternion orientation;
        public Matrix worldTransform;

        public Effect shader;
        public SkinnedModel model;
        protected string skinnedModelFile;

        public Matrix[] absoluteBoneTransforms;
        public Texture2D[] textures;
        public int max_textures;
        public RenderTarget2D[] render_targets;
        public int max_targets;

        public AnimationController controller;
        public int activeAnimationClip = 0;

        public int health;
        public bool isHit;

        protected Dimension currentDimension;

        public Dimension CurrentDimension
        {
            get
            {
                return currentDimension;
            }
            internal set
            {
                currentDimension = value;
            }
        }

        protected enum Tex_Select
        {
            model = 0,
            cel_tex
        }

        protected enum Target_Select
        {
            scene = 0,
            normalDepth
        }

        #endregion

        #region Constructor

        public Agent(GraphicsDeviceManager Graphics, ContentManager Content, string skinnedModelFile)
        {
            graphics = Graphics;
            content = Content;
            this.skinnedModelFile = skinnedModelFile;
        }

        #endregion

        #region Load Content

        public virtual void LoadContent()
        {
        }

        #endregion

        #region Dimension Hopping

        public void ChangeDimension(Dimension dim)
        {
            currentDimension = dim;
        }

        public void ChangeDimension()
        {
            if (currentDimension == Dimension.FIRST)
            {
                currentDimension = Dimension.SECOND;
            }
            else
            {
                currentDimension = Dimension.FIRST;
            }
        }

        #endregion

        #region Update

        public void Update()
        {

        }

        #endregion

        #region Draw

        public void Draw()
        {

        }

        #endregion

    }
}
