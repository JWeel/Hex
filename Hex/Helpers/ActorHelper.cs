using System.Collections.Generic;
using System.Linq;
using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            if (this.Input.KeyPressed(Keys.K))
            {
                this.Actors.Add(new Actor(this.ActorTexture,
                    this.Tilemap.Map.Values.Random().Position));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // this works but doesnt scale!
            // really both helpers need to be inside a third helper 'WorldHelper' name tbd
            // this class contains the camera and applies it to both.
            // it also handles the separate background and foreground in its own draw, uses camera matrix for both

            // this.Actors.Each(actor =>
            // {
            //     var hexagonSize = new Vector2(25, 29); // shouldnt be magic number
            //     var position = (actor.Position + hexagonSize / 2)
            //         .Transform(this.Tilemap.TilemapRotationMatrix)
            //         .Transform(this.Tilemap.CameraTranslationMatrix);

            //     var offset = actor.Texture.ToVector() / 2 * this.Tilemap.Camera.ZoomScaleFactor;

            //     spriteBatch.DrawAt(actor.Texture, position - offset, scale: this.Tilemap.Camera.ZoomScaleFactor);
            // });
        }

        #endregion
    }
}