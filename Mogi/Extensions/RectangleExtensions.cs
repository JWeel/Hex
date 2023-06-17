using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mogi.Extensions
{
    public static class RectangleExtensions
    {
        /// <summary> Determines whether or not the provided <see cref="MouseState"/> lies within the bounds of this <see cref="Rectangle"/>. </summary>
        public static bool Contains(this Rectangle rectangle, MouseState mouseState) =>
            rectangle.Contains(mouseState.ToPoint());

        /// <summary> Gets a new <see cref="Rectangle"/> with the size of this rectangle but located at a specified point. </summary>
        public static Rectangle Relocate(this Rectangle rectangle, Point location) =>
            new Rectangle(location, rectangle.Size);

        /// <summary> Gets a new <see cref="Rectangle"/> with the same location as this rectangle but with its size modified. </summary>
        public static Rectangle Enlarge(this Rectangle rectangle, Point delta) =>
            new Rectangle(rectangle.Location, rectangle.Size + delta);

        /// <summary> Gets a new <see cref="Rectangle"/> with the same location as this rectangle but with a specified size. </summary>
        public static Rectangle Resize(this Rectangle rectangle, Point size) =>
            new Rectangle(rectangle.Location, size);

        /// <summary> Gets a new <see cref="Rectangle"/> with the size of this rectangle but moved to a different point. </summary>
        public static Rectangle Move(this Rectangle rectangle, Point distance) =>
            new Rectangle(rectangle.Location + distance, rectangle.Size);

        /// <summary> Gets a <see cref="Vector2"/> that contains the coordinates of the center of this rectangle. </summary>
        public static Vector2 Center(this Rectangle rectangle) =>
            rectangle.Location.ToVector2() + rectangle.Size.ToVector2() / 2;
    }
}