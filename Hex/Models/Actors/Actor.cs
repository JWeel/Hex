using Hex.Models.Tiles;
using Microsoft.Xna.Framework.Graphics;

namespace Hex.Models.Actors
{
    public class Actor
    {
        #region Constructors

        public Actor(Texture2D texture, Hexagon tile)
        {
            this.Texture = texture;
            this.Tile = tile;
            this.BaseViewDistance = 9;
            this.BaseMoveDistance = 4;
        }

        #endregion

        #region Properties

        public Texture2D Texture { get; }
        public Hexagon Tile { get; set; }

        public int ViewDistance
        {
            get
            {
                return this.BaseViewDistance;
            }
        }

        public int MoveDistance
        {
            get
            {
                return this.BaseMoveDistance;
            }
        }

        protected int BaseViewDistance { get; }
        protected int BaseMoveDistance { get; }

        #endregion
    }
}