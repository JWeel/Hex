using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hex.Extensions
{
    public static class RectangleExtensions
    {
        /// <summary> Gets whether or not the X and Y properties of the provided <see cref="MouseState"/> lie within the bounds of this <see cref="Rectangle"/>. </summary>
        public static bool Contains(this Rectangle source, MouseState mouseState) =>
            source.Contains(mouseState.ToPoint());
    }
}