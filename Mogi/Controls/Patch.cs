using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that uses 9-patch drawing. </summary>
    public class Patch : Control<Patch>
    {
        #region Constructors

        public Patch(Rectangle destination, Texture2D texture, int border)
            : this(destination, texture, border, Color.White)
        {
        }

        public Patch(Rectangle destination, Texture2D texture, int border, Color color)
            : base(destination, texture, color)
        {
            this.Border = border;
        }

        #endregion

        #region Properties

        protected int Border { get; set; }

        #endregion

        #region Overriden Methods

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            spriteBatch.DrawNinePatchRectangle(this.Texture, this.Destination, this.Border, color);
        }

        #endregion
    }
}