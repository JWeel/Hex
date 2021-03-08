using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using System;

namespace Mogi.Helpers
{
    /// <summary> Keeps track of application framerate. </summary>
    public class FramerateHelper
    {
        #region Constants

        private static readonly TimeSpan ONE_SECOND = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructors

        public FramerateHelper(Vector2 position, Action<Action<ContentManager>> subscribeToLoad, Action<Action<GameTime>> subscribeToUpdate, Action<Action<SpriteBatch>> subscribeToDraw)
        {
            subscribeToLoad(this.LoadContent);
            subscribeToUpdate(this.UpdateState);
            subscribeToDraw(this.DrawState);

            this.Position = position;
            this.Framerate = 0;
            this.FrameCounter = 0;
            this.ElapsedTime = TimeSpan.Zero;
        }

        #endregion

        #region Properties

        protected Vector2 Position { get; }

        protected int Framerate { get; set; }
        protected int FrameCounter { get; set; }
        protected TimeSpan ElapsedTime { get; set; }

        protected SpriteFont Font { get; set; }

        #endregion

        #region Methods

        protected void LoadContent(ContentManager content)
        {
            this.Font = content.Load<SpriteFont>("Alphabet/alphabet");
        }

        protected void UpdateState(GameTime gameTime)
        {
            this.ElapsedTime += gameTime.ElapsedGameTime;
            if (this.ElapsedTime < ONE_SECOND)
                return;

            this.ElapsedTime -= ONE_SECOND;
            this.Framerate = this.FrameCounter;
            this.FrameCounter = 0;
        }

        protected void DrawState(SpriteBatch spriteBatch)
        {
            this.FrameCounter++;
            string text = this.Framerate.ToString();
            spriteBatch.DrawText(this.Font, text, this.Position, Color.White, scale: 2f, depth: 0.9f);
        }

        #endregion
    }
}