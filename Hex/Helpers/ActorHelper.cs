using Hex.Models.Actors;
using Hex.Models.Tiles;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using System;
using System.Collections.Generic;

namespace Hex.Helpers
{
    public class ActorHelper
    {
        #region Constructors

        public ActorHelper(InputHelper input, FactionHelper faction, ContentManager content)
        {
            this.Input = input;
            this.Faction = faction;
            this.Actors = new List<Actor>();

            this.TexturesGrimion = new[]
            {
                content.Load<Texture2D>("Graphics/grimion1"),
                content.Load<Texture2D>("Graphics/grimion2"),
                content.Load<Texture2D>("Graphics/grimion3"),
            };
            this.TexturesIron = new[]
            {
                content.Load<Texture2D>("Graphics/iron1"),
                content.Load<Texture2D>("Graphics/iron2"),
            };
        }

        #endregion

        #region Properties

        public List<Actor> Actors { get; }

        protected InputHelper Input { get; }
        protected FactionHelper Faction { get; }

        protected Texture2D[] TexturesGrimion { get; }
        protected Texture2D[] TexturesIron { get; }

        #endregion

        #region Methods

        public void Reset()
        {
            this.Actors.Clear();
        }

        public Actor Add()
        {
            // TODO better way to set up actors and textures
            var textures = (this.Faction.ActiveFaction?.Name == "Monster") ? this.TexturesGrimion : this.TexturesIron;
            
            var actor = new Actor(textures);
            this.Actors.Add(actor);
            actor.Faction = this.Faction.ActiveFaction ?? throw new InvalidOperationException("No active faction.");
            return actor;
        }

        public void Move(Actor actor, Hexagon tile, double cost)
        {
            actor.Move(tile, cost);
        }

        #endregion
    }
}