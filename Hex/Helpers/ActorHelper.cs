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

            this.Textures = new[]
            {
                content.Load<Texture2D>("Graphics/grimion1"),
                content.Load<Texture2D>("Graphics/grimion2"),
                content.Load<Texture2D>("Graphics/grimion3"),
            };
        }

        #endregion

        #region Properties

        public IList<Actor> Actors { get; }

        protected InputHelper Input { get; }
        protected FactionHelper Faction { get; }

        protected Texture2D[] Textures { get; }

        #endregion

        #region Methods

        public void Reset()
        {
            this.Actors.Clear();
        }

        public Actor Add()
        {
            var actor = new Actor(this.Textures);
            this.Actors.Add(actor);
            actor.Faction = this.Faction.ActiveFaction ?? throw new InvalidOperationException("No active faction.");
            return actor;
        }

        public void Move(Actor actor, Hexagon tile)
        {
            actor.Move(tile);
        }

        #endregion
    }
}