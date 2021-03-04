using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGui.Controls
{
    public abstract class Control<T> : IControl 
        where T : Control<T>
    {
        #region Constructors

        public Control()
        {
        }

        #endregion

        #region Members

        public event Action<T> OnMouseEnter;
        public event Action<T> OnMouseLeave;
            
        #endregion

        #region Methods

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }

        #endregion
    }
}