using Extended.Extensions;
using Extended.Patterns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using System.Linq;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that depicts a sequence of textures shown one after the other. </summary>
    public class Slideshow : Control<Slideshow>
    {
        #region Constructors

        /// <summary> Initializes a new instance with the dimensions of the destination area area and an arbitrary number of textures. </summary>
        public Slideshow(int x, int y, int width, int height, Texture2D firstTexture, params Texture2D[] additionalTextures)
            : this(new Rectangle(x, y, width, height), firstTexture, additionalTextures)
        {
        }

        /// <summary> Initializes a new instance with a destination area, and an arbitrary number of textures. </summary>
        public Slideshow(Rectangle destination, Texture2D firstTexture, params Texture2D[] additionalTextures)
            : base(destination, firstTexture, color: Color.White)
        {
            this.Textures = firstTexture.Yield().Concat(additionalTextures).ToArray();
            this.TextureIndex = Cyclic.Range(this.Textures.Length);
            // TODO add overloads that take interval
            this.IntervalMilliseconds = 800d;
        }

        #endregion

        #region Properties

        protected Texture2D[] Textures { get; set; }

        protected Cyclic<int> TextureIndex { get; set; }

        protected double IntervalMilliseconds { get; set; }

        protected double LastTimestamp { get; set; }

        #endregion

        #region Methods

        public override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;
                
            base.Update(gameTime);

            if ((gameTime.TotalGameTime.TotalMilliseconds - this.LastTimestamp) > this.IntervalMilliseconds)
            {
                this.LastTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
                this.TextureIndex.Advance();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            if (color.IsTransparent())
                return;

            if (this.Textures.None())
                return;

            spriteBatch.DrawTo(this.Textures[this.TextureIndex], this.Destination, color);
        }

        #endregion
    }
}