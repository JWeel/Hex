using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class Vector2Extensions
    {
        /// <summary> Gets a <see cref="Rectangle"/> located at origin and sized to this vector. </summary>
        public static Rectangle ToRectangle(this Vector2 value) =>
            new Rectangle(Point.Zero, value.ToPoint());
    }
}