using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class Vector2Extensions
    {
        #region ToRectangle

        /// <summary> Gets a <see cref="Rectangle"/> located at origin and sized to this vector. </summary>
        public static Rectangle ToRectangle(this Vector2 value) =>
            new Rectangle(Point.Zero, value.ToPoint());

        #endregion

        #region Swap

        /// <summary> Creates a new vector that contains the X and Y values of this vector swapped. </summary>
        public static Vector2 Swap(this Vector2 value) =>
            new Vector2(value.Y, value.X);

        #endregion

        #region Transform

        /// <summary> Creates a new <see cref="Vector2"/> that contains a 2-dimensional transform of this vector using the specified matrix. </summary>
        public static Vector2 Transform(this Vector2 vector, Matrix matrix) =>
            Vector2.Transform(vector, matrix);

        #endregion
    }
}