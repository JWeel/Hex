using System.Collections.Generic;
using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class ActorHelper : IUpdate<NormalUpdate>, IDraw<ForegroundDraw>
    {
        #region Constructors

        public ActorHelper()
        {
            this.Actors = new List<Actor>();
        }

        #endregion

        #region Properties

        public IList<Actor> Actors { get; }

        public Actor SourceActor { get; protected set; }
            
        #endregion

        #region Methods

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Actors.Each(actor => actor.Draw(spriteBatch));
        }

        #endregion
    }
}