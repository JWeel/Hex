using Microsoft.Xna.Framework;

namespace Mogi.Extensions
{
    public static class ColorExtensions
    {
        /// <summary> Returns <see langword="true"/> if the alpha value of this color is 0, otherwise returns <see langword="false"/>. </summary>
        public static bool IsTransparent(this Color color) =>
            (color.A == 0);

        /// <summary> Returns a color with its alpha value replaced by a floating point equivalent of a given premultiplied alpha integer value.  </summary>
        /// <remarks> This can be used to write code with integer values while using alpha blending for drawing. <br/>
        /// The distinction is needed because <see cref="Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend"/> uses range [0.0f, 1.0f], whereas <see cref="Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied"/> uses range [0,255]. </remarks>
        public static Color Blend(this Color color, int alpha) =>
            color * (alpha / 255f);
    }
}