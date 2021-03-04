using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGui.Extensions;

namespace Hex.Models
{
    public class DrawableRectangle
    {
        #region Constructors

        public DrawableRectangle(Texture2D texture, Rectangle rectangle, Color? color = default)
        {
            this.Texture = texture;
            this.Recangle = rectangle;
            this.Color = color ?? Color.White;
        }

        #endregion  

        #region Properties

        protected Texture2D Texture { get; }
        protected Rectangle Recangle { get; }
        protected Color Color { get; }

        #endregion

        #region Methods

        public void Draw(SpriteBatch spriteBatch, Color? color = default, float depth = 0f) =>
            spriteBatch.DrawTo(this.Texture, this.Recangle, color ?? this.Color, depth);

        public bool Contains(Vector2 position) =>
            this.Recangle.Contains(position);
            
        #endregion
    }
}