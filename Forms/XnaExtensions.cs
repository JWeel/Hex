using Microsoft.Xna.Framework;

namespace Forms
{
    public static class XnaExtensions
    {
        #region Blend

        public static Color Blend(this Color color, Color blendColor)
        {
            return new Color(
                (byte) (color.R * blendColor.R / 255),
                (byte) (color.G * blendColor.G / 255),
                (byte) (color.B * blendColor.B / 255),
                (byte) (color.A * blendColor.A / 255));
        }

        #endregion
    }
}