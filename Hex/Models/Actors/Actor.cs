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
            this.BaseViewDistance = 8;
            this.BaseMovementAllowed = 6;
            this.AnimationDuration = 1000;
        }

        #endregion

        #region Properties

        public Texture2D Texture
        {
            get
            {
                var interval = this.AnimationDuration / this.Textures.Length;
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

        public double MovementAllowed
        {
            get
            {
                return this.BaseMovementAllowed - this.MovementInRound;
            }
        }

        public Hexagon Tile { get; protected set; }

        public Faction Faction { get; set; }

        protected Texture2D[] Textures { get; }
        protected int BaseViewDistance { get; }
        protected double BaseMovementAllowed { get; }
        protected int AnimationDuration { get; }

        protected double MovementInRound { get; set; }
        protected double MovementOverall { get; set; }
        protected double ActionsInRound { get; set; }
        protected double ActionsOverall { get; set; }

        #endregion

        #region Methods

        public void Reset()
        {
            this.MovementInRound = 0;
            this.ActionsInRound = 0;
        }

        public void Move(Hexagon tile, double cost)
        {
            this.Tile = tile;
            this.MovementInRound += cost;
            this.MovementOverall += cost;
        }

        #endregion
    }
}