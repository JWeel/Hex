using Hex.Models;
using Hex.Models.Actors;
using Hex.Models.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using System.Collections.Generic;

namespace Hex.Helpers
{
    public class ActorHelper
    {
        #region Constructors

        public ActorHelper(InputHelper input, ContentManager content)
        {
            this.Input = input;
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

        public InputHelper Input { get; }

        public IList<Actor> Actors { get; }

        protected Texture2D[] Textures { get; }

        protected Faction Player { get; } = new Faction { Color = Color.Blue } ;
        protected Faction Monster { get; } = new Faction { Color = Color.Red };

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
            actor.Faction = (this.Actors.Count % 2 == 0) ? this.Player : this.Monster;
            return actor;
        }

        public void Move(Actor actor, Hexagon tile)
        {
            actor.Move(tile);
        }

        #endregion
    }
}