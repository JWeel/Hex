using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Mogi.Inversion
{
    /// <summary> Provides wrapping of arbitrary instances to support attaching to classes implementing <see cref="IRoot"/>. </summary>
    public class PhasedWrapper<TUpdatePhase, TDrawPhase> : IUpdate<TUpdatePhase>, IDraw<TDrawPhase>
        where TUpdatePhase : IPhase
        where TDrawPhase : IPhase
    {
        #region Constructors
            
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