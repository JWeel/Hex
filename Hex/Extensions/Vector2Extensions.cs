using Microsoft.Xna.Framework;

namespace Hex.Extensions
{
    public static class Vector2Extensions
    {
        #region Print

        public static string Print(this Vector2 vector) =>
            $"({vector.X}, {vector.Y})";

        public static string PrintRounded(this Vector2 vector) =>
            $"({vector.X:0}, {vector.Y:0})";

        #endregion

        #region Floored

        // cannot call it floor because thats a void instance method (which mutates the struct? yuck)
        public static Vector2 Floored(this Vector2 vector) =>
            Vector2.Floor(vector);

        #endregion
    }
}