using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Controls
{
    /// <summary> Represents a basic graphical element with no special logic. </summary>
    public class Basic : Control<Patch>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area and a texture. </summary>
        public Basic(Rectangle destination, Texture2D texture)
            : this(destination, texture, Color.White)
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