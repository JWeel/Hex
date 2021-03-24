using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Mogi.Inversion
{
    // TODO find way to support using only Update or only Draw
    /// <summary> Forwards delegates to the necessary methods for implementing <see cref="IUpdate{}"/> and <see cref="IDraw{}"/>.
    /// <br/> This enables any arbitrary class to be attached to classes implementing <see cref="IRoot"/>. </summary>
    /// <remarks> See also: <see cref="Extensions.Attach"/> </remarks>
    public class PhasedWrapper<TUpdatePhase, TDrawPhase> : IUpdate<TUpdatePhase>, IDraw<TDrawPhase>
        where TUpdatePhase : IPhase
        where TDrawPhase : IPhase
    {
        #region Constructors

        /// <summary> Initializes a new instance with delegates that allow it to implement <see cref="IUpdate{}"/> and <see cref="IDraw{}"/>. </summary>
        /// <param name="update"> The action to invoke when updating. </param>
        /// <param name="draw"> The action to invoke when drawing. </param>
        public PhasedWrapper(Action<GameTime> update, Action<SpriteBatch> draw)
        {
            this.UpdateAction = update;
            this.DrawAction = draw;
        }

        #endregion

        #region Properties

        protected Action<GameTime> UpdateAction { get; }
        protected Action<SpriteBatch> DrawAction { get; }

        #endregion

        #region Methods

        public void Update(GameTime gameTime)
        {
            this.UpdateAction?.Invoke(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.DrawAction?.Invoke(spriteBatch);
        }

        #endregion
    }
}