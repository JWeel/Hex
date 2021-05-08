using Hex.Models.Tiles;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hex.Models.Actors
{
    public class Actor
    {
        #region Constructors

        public Actor(Texture2D[] textures)
        {
            this.Textures = textures;
            this.TextureScale = .7f;
            this.BaseViewDistance = 9;
            this.BaseMoveDistance = 6;
        }

        #endregion

        #region Properties

        public Texture2D Texture
        {
            get
            {
                var interval = 1000 / this.Textures.Length;
                var ms = DateTime.Now.Millisecond;
                var index = Math.Min(ms / interval, this.Textures.Length - 1);
                return this.Textures[index];
            }
        }

        public float TextureScale { get; }

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

        public Faction Faction { get; set; }

        protected Texture2D[] Textures { get; }

        public Actor(int baseViewDistance, int baseMoveDistance)
        {
            this.BaseViewDistance = baseViewDistance;
            this.BaseMoveDistance = baseMoveDistance;

        }
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