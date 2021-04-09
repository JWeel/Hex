using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class RectangleExtensions
    {
        #region Move

        /// <summary> Gets a new <see cref="Rectangle"/> with the size of this rectangle and located at a specified point. </summary>
        public static Rectangle Relocate(this Rectangle rectangle, Point location) =>
            new Rectangle(location, rectangle.Size);

        #endregion
    }
}