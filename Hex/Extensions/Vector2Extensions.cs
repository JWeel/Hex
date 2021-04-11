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
    }
}