using Microsoft.Xna.Framework.Graphics;

namespace Hex.Models
{
    public class Actor
    {
        #region Constructors

        public Actor(Texture2D texture, Hexagon tile)
        {
            this.Texture = texture;
            this.Tile = tile;
        }

        #endregion

        #region Properties

        public Texture2D Texture { get; }
        public Hexagon Tile { get; set; }

        #endregion
    }
}