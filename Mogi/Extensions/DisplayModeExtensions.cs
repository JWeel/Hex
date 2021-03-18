using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Extensions
{
    public static class DisplayModeExtensions
    {
        /// <summary> Creates a new vector that contains <see cref="DisplayMode.Width"/> and <see cref="DisplayMode.Height"/>. </summary>
        public static Vector2 ToSizeVector(this DisplayMode displayMode) =>
            new Vector2(displayMode.Width, displayMode.Height);
    }
}