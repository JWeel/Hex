using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;

namespace Hex.Extensions
{
    public static class RectangleExtensions
    {
        /// <summary> Gets whether or not the X and Y properties of the provided <see cref="MouseState"/> lie within the bounds of this <see cref="Rectangle"/>. </summary>
        public static bool Contains(this Rectangle source, MouseState mouseState) =>
            source.Contains(mouseState.ToPoint());

        /// <summary> Gets a new <see cref="Rectangle"/> with the size of this rectangle but located at a specified point. </summary>
        public static Rectangle Relocate(this Rectangle rectangle, Point location) =>
            new Rectangle(location, rectangle.Size);
    }
}