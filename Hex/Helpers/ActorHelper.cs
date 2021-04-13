using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using System.Collections.Generic;

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
                    this.Tilemap.Map.Values.Random().Position));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var actor in this.Actors)
            {
                // TODO hexagonsize should not be random number
                // dunno if texture fits in Hexagon class but maybe size does
                // and definitely middle position should
                var hexagonSize = new Vector2(25, 29);
                var position = (actor.Position + hexagonSize / 2).Transform(this.Tilemap.RotationMatrix);
                var offset = actor.Texture.ToVector() / 2;
                spriteBatch.DrawAt(actor.Texture, position - offset);
            }
        }

        #endregion
    }
}