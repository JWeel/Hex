using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that intermittently turns invisible. </summary>
    public class Blinker : Control<Blinker>
    {
        #region Constructors

        /// <summary> Initializes a new instance with the dimensions of the destination area area, a texture, and an interval. </summary>
        public Blinker(int x, int y, int width, int height, Texture2D texture, double interval)
            : this(new Rectangle(x, y, width, height), texture, Color.White, interval)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a texture, and an interval. </summary>
        public Blinker(Rectangle destination, Texture2D texture, double interval)
            : this(destination, texture, Color.White, interval)
        {
        }

        /// <summary> Initializes a new instance with the dimensions of the destination area, a texture, an overlay color, and an interval. </summary>
        public Blinker(int x, int y, int width, int height, Texture2D texture, Color color, double interval)
            : this(new Rectangle(x, y, width, height), texture, color, interval)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a texture, an overlay color, and an interval. </summary>
        public Blinker(Rectangle destination, Texture2D texture, Color color, double interval)
            : base(destination, texture, color)
        {
            this.IntervalMilliseconds = interval;
            this.IsVisible = true;
        }

        #endregion

        #region Properties

        protected double IntervalMilliseconds { get; set; }

        protected double LastTimestamp { get; set; }

        protected bool IsVisible { get; set; }

        protected bool ForcedVisible { get; set; }

        #endregion

        #region Methods

        /// <summary> Forces the blinker to be visible and resets the interval. </summary>
        public void Reveal()
        {
            this.IsVisible = true;
            this.ForcedVisible = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;
                
            base.Update(gameTime);

            if (this.ForcedVisible)
            {
                this.LastTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
                this.ForcedVisible = false;
            }

            var delta = (gameTime.TotalGameTime.TotalMilliseconds - this.LastTimestamp);
            if (delta > this.IntervalMilliseconds)
            {
                this.LastTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
                this.IsVisible = !this.IsVisible;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            if (!this.IsVisible)
                return;

            base.Draw(spriteBatch, color);
        }

        #endregion
    }
}