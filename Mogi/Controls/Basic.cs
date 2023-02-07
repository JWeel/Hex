using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Controls
{
    /// <summary> Represents a basic graphical element with no special logic. </summary>
    public class Basic : Control<Basic>
    {
        #region Constructors

        /// <summary> Initializes a new instance with the dimensions of the destination area area and a texture. </summary>
        public Basic(int x, int y, int width, int height, Texture2D texture)
            : this(new Rectangle(x, y, width, height), texture, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area and a texture. </summary>
        public Basic(Rectangle destination, Texture2D texture)
            : this(destination, texture, Color.White)
        {
        }

        /// <summary> Initializes a new instance with the dimensions of the destination area, a texture, and an overlay color. </summary>
        public Basic(int x, int y, int width, int height, Texture2D texture, Color color)
            : this(new Rectangle(x, y, width, height), texture, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a texture, and an overlay color. </summary>
        public Basic(Rectangle destination, Texture2D texture, Color color)
            : base(destination, texture, color)
        {
        }

        #endregion
    }
}