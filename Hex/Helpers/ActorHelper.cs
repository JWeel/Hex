using System.Collections.Generic;
using System.Linq;
using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Enums;
using Mogi.Extensions;
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
                    this.Tilemap.HexagonMap.Values.Random().Position));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // this works but doesnt scale!
            // really both helpers need to be inside a third helper 'WorldHelper' name tbd
            // this class contains the camera and applies it to both.
            // it also handles the separate background and foreground in its own draw, uses camera matrix for both

            this.Actors.Each(actor =>
            {
                var position = (actor.Position + new Vector2(25 / 2f, 29 / 2f))
                    .Transform(this.Tilemap.TilemapRotationMatrix * this.Tilemap.CameraTranslationMatrix);

                var size = new Vector2(actor.Texture.Width, actor.Texture.Height);
                var matrix = Matrix.CreateRotationZ(this.Tilemap.Rotation);
                spriteBatch.DrawAt(actor.Texture, position - size/2);
            });
        }

        #endregion
    }
}