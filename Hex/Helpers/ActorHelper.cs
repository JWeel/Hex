using Hex.Models.Actors;
using Hex.Models.Tiles;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Helpers;
using System.Collections.Generic;

namespace Hex.Helpers
{
    public class ActorHelper
    {
        #region Constructors

        public ActorHelper(InputHelper input, TilemapHelper tilemap, ContentManager content)
        {
            this.Input = input;
            this.Tilemap = tilemap;
            this.Actors = new List<Actor>();

            this.ActorTexture = content.Load<Texture2D>("Graphics/spook");
            this.FogOfWarByActorMap = new Dictionary<Actor, IDictionary<Hexagon, bool>>();
        }

        #endregion

        #region Properties

        public InputHelper Input { get; }

        // TODO should actor rely on tilemap?
        public TilemapHelper Tilemap { get; }

        public IList<Actor> Actors { get; }

        public IDictionary<Actor, IDictionary<Hexagon, bool>> FogOfWarByActorMap;

        protected Texture2D ActorTexture { get; set; }

        #endregion

        #region Methods

        public void Add(Hexagon tile)
        {
            var actor = new Actor(this.ActorTexture, tile);
            this.Actors.Add(actor);
            this.FogOfWarByActorMap[actor] = this.Tilemap.DetermineFogOfWar(actor.Tile, actor.ViewDistance);
        }

        #endregion
    }
}