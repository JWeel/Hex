using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGui.Extensions;
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

        public Control(Rectangle destination, Texture2D texture, int border, float depth)
        {
            this.Destination = destination;
            this.Texture = texture;
            this.Border = border;
            this.Depth = depth;
        }

        #endregion

        #region Members

        public event Action<Control<T>> OnMouseEnter;
        public event Action<Control<T>> OnMouseLeave;

        protected Rectangle Destination { get; set; }
        protected Texture2D Texture { get; set; }
        protected float Depth { get; set; }
        protected int Border { get; set; }

        protected bool ContainedMouse { get; set; }
        protected bool ContainsMouse { get; set; }

        #endregion

        #region Methods

        public virtual void Update(GameTime gameTime)
        {
            this.ContainedMouse = this.ContainsMouse;
            this.ContainsMouse = this.Destination.Contains(Mouse.GetState().Position);

            if (!this.ContainedMouse && this.ContainsMouse)
                this.OnMouseEnter?.Invoke(this);
            if (this.ContainedMouse && !this.ContainsMouse)
                this.OnMouseLeave?.Invoke(this);
        }

        public virtual void Draw(SpriteBatch spriteBatch) =>
            this.Draw(spriteBatch, Color.White);

        public virtual void Draw(SpriteBatch spriteBatch, Color color) =>
            spriteBatch.DrawRoundedRectangle(this.Texture, this.Destination, this.Border, color, this.Depth);

        #endregion
    }
}