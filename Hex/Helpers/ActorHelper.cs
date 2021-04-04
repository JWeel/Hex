using System.Collections.Generic;
using System.Linq;
using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Enums;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class ActorHelper : IUpdate<NormalUpdate>, IDraw<ForegroundDraw>
    {
        #region Constructors

        public ActorHelper(InputHelper input, TilemapHelper tilemap, ContentManager content)
        {
            this.Input = input;
            this.Tilemap = tilemap;
            this.Actors = new List<Actor>();

            this.ActorTexture = content.Load<Texture2D>("spook");
        }


        #endregion

        #region Properties

        public InputHelper Input { get; }

        public TilemapHelper Tilemap { get; }

        public IList<Actor> Actors { get; }

        public Actor SourceActor { get; protected set; }

        protected Texture2D ActorTexture { get; set; }

        #endregion

        #region Methods

        public void Update(GameTime gameTime)
        {
            if (this.Input.MousePressed(MouseButton.Left))
            {
                this.Actors.Add(new Actor(this.ActorTexture,
                    this.Tilemap.HexagonMap.Values.Random().Position + this.Tilemap.TilemapOffset));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Actors.Each(actor => actor.Draw(spriteBatch));
        }

        #endregion
    }
}