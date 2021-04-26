using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Models;
using Hex.Models.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using System.Collections.Generic;
using System.Linq;

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

            this.ActorTexture = content.Load<Texture2D>("Graphics/spook");
        }

        #endregion

        #region Properties

        public InputHelper Input { get; }

        // TODO should actor rely on tilemap?
        public TilemapHelper Tilemap { get; }

        public IList<Actor> Actors { get; }

        public Actor SourceActor { get; protected set; }

        protected Texture2D ActorTexture { get; set; }

        #endregion

        #region Methods

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.K))
            {
                this.Actors.Add(new Actor(this.ActorTexture,
                    this.Tilemap.Map.Values.Random()));
            }
            
            if ((this.Input.MousePressed(MouseButton.Left)) && (this.Tilemap.CursorTile != null))
                this.SourceActor = this.Actors.FirstOrDefault(actor => (actor.Tile == this.Tilemap.CursorTile));

            if (this.SourceActor != null)
                Static.Memo.AppendLine($"Actor: {this.SourceActor.Tile.Cube}");
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var actor in this.Actors)
            {
                var sourcePosition = actor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                var sizeOffset = actor.Texture.ToVector() / 2;

                var color = (actor == this.SourceActor) ? Color.Coral : Color.White;
                spriteBatch.DrawAt(actor.Texture, sourcePosition - sizeOffset, color);
            }
        }

        #endregion
    }
}