using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using Mogi.Inversion;
using System;

namespace Mogi.Helpers
{
    /// <summary> Keeps track of application framerate. </summary>
    public class FramerateHelper : IUpdate<CriticalUpdate>, IDraw<MenuDraw>
    {
        #region Constants

        private static readonly Vector2 DEFAULT_POSITION = new Vector2(20, 20);
        private static readonly TimeSpan ONE_SECOND = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructors

        public FramerateHelper(SpriteFont spriteFont)
        {
            this.Font = spriteFont;
            this.Position = DEFAULT_POSITION;
            this.Framerate = 0;
            this.FrameCounter = 0;
            this.ElapsedTime = TimeSpan.Zero;
        }

        #endregion

        #region Properties

        protected SpriteFont Font { get; set; }
        protected Vector2 Position { get; }
        protected int Framerate { get; set; }
        protected int FrameCounter { get; set; }
        protected TimeSpan ElapsedTime { get; set; }

        #endregion

        #region Methods

        public void Update(GameTime gameTime)
        {
            this.ElapsedTime += gameTime.ElapsedGameTime;
            if (this.ElapsedTime < ONE_SECOND)
                return;

            this.ElapsedTime -= ONE_SECOND;
            this.Framerate = this.FrameCounter;
            this.FrameCounter = 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.FrameCounter++;
            string text = this.Framerate.ToString();
            spriteBatch.DrawText(this.Font, text, this.Position, Color.White, scale: 2f, depth: 0.9f);
        }

        #endregion
    }
}