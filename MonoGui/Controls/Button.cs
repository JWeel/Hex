using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGui.Extensions;

namespace MonoGui.Controls
{
    public class Button : Control<Button>
    {
        #region Constants

        private static readonly Color PLAIN = new Color(230, 230, 230);
        private static readonly Color HOVER = new Color(250, 250, 250);
        private static readonly Color PRESS = new Color(210, 210, 210);

        #endregion

        #region Constructors

        public Button(Rectangle destination, Texture2D texture, int border, float depth)
            : base(destination, texture, border, depth)
        {
        }

        #endregion

        #region Properties

        protected bool PressedMouse { get; set; }
        protected bool PressingMouse { get; set; }

        #endregion

        #region Members

        public event Action<Button> OnClick;

        #endregion

        #region Overridden Methods

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.PressedMouse = this.PressingMouse;
            this.PressingMouse = (this.ContainsMouse && Mouse.GetState().LeftButton.IsPressed());

            if (this.PressedMouse && !this.PressingMouse && this.ContainsMouse)
                this.OnClick?.Invoke(this);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (this.PressingMouse)
                base.Draw(spriteBatch, PRESS);
            else if (this.ContainsMouse)
                base.Draw(spriteBatch, HOVER);
            else
                base.Draw(spriteBatch, PLAIN);
        }

        #endregion
    }
}