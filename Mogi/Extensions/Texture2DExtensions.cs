using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mogi.Extensions
{
    public static class Texture2DExtensions
    {
        #region Size

        /// <summary> Gets a <see cref="Vector2"/> containing the width and height of this texture. </summary>
        public static Vector2 ToVector(this Texture2D texture) =>
            new Vector2(texture.Width, texture.Height);

        #endregion
    }
}