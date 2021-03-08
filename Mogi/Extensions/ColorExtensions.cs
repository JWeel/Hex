using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class ColorExtensions
    {
        /// <summary> Returns <see langword="true"/> if the alpha value of this color is 0, otherwise returns <see langword="false"/>. </summary>
        public static bool IsTransparent(this Color color) =>
            (color.A == 0);
    }
}