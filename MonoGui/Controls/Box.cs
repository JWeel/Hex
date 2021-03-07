using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGui.Controls
{
    public class Box : Control<Box>
    {
        #region Constructors

        public Box(Rectangle destination, Texture2D texture, int border, float depth)
            : base(destination, texture, border, depth)
        {
        }

        #endregion
    }
}