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

        public ActorHelper(InputHelper input, ContentManager content)
        {
            this.Input = input;
            this.Actors = new List<Actor>();

            this.ActorTexture = content.Load<Texture2D>("Graphics/spook");
        }

        #endregion

        #region Properties

        public InputHelper Input { get; }

        public IList<Actor> Actors { get; }

        protected Texture2D ActorTexture { get; set; }

        #endregion

        #region Methods

        public void Reset()
        {
            this.Actors.Clear();
        }

        public Actor Add()
        {
            var actor = new Actor(this.ActorTexture);
            this.Actors.Add(actor);
            return actor;
        }

        public void Move(Actor actor, Hexagon tile)
        {
            actor.Move(tile);
        }

        #endregion
    }
}