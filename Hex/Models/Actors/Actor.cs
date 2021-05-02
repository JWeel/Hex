using Hex.Models.Tiles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Hex.Models.Actors
{
    public class Actor
    {
        #region Constructors

        public Actor(Texture2D texture)
        {
            this.Texture = texture;
            this.BaseViewDistance = 9;
            this.BaseMoveDistance = 4;
        }

        #endregion

        #region Properties

        public Texture2D Texture { get; }

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

        public Hexagon Tile { get; protected set; }

        public Faction Faction { get; protected set; }

        protected int BaseViewDistance { get; }
        protected int BaseMoveDistance { get; }

        #endregion

        #region Methods

        public void Move(Hexagon tile)
        {
            this.Tile = tile;
        }
            
        #endregion
    }
}