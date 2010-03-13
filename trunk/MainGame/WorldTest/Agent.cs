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
    struct Light 
    {
        public Vector3 position;
        public Vector3 color;

        public Light(Vector3 pos, Vector3 col)
        {
            position = pos;
            color = col;
        }
    };

    abstract class Agent
    {

        #region Properties

        protected GraphicsDeviceManager graphics;
        protected ContentManager content;

        public Vector3 position;
        public Vector3 velocity;
        protected float movement_speed_reg;

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
        public int activeAnimationClip;

        public enum Tex_Select
        {
            model = 0,
            cel_tex
        }

        public enum Target_Select
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

        public abstract void LoadContent();

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
