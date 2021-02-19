using Microsoft.Xna.Framework;

namespace Hex.Extensions
{
    public static class Vector2Extensions
    {
        #region Print Methods

        public static string Print(this Vector2 vector) =>
            $"({vector.X}, {vector.Y})";

        #endregion
    }
}