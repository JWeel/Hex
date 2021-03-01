using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hex.Controls
{
    public class Button : IControl
    {
        #region Constructors

        public Button(Texture2D texture, Rectangle rectangle, Color color, string label = null, int borderSize = 2, Action onClickAction = null)
        {
            this.Label = label ?? string.Empty;

            this.Outer = new DrawableRectangle(texture, rectangle, Color.Multiply(color, .75f));

            var borderWidth = Math.Clamp(borderSize, 0, rectangle.Width);
            var borderHeight = Math.Clamp(borderSize, 0, rectangle.Height);
            var innerRectangle = new Rectangle(rectangle.X + borderWidth, rectangle.Y + borderHeight, rectangle.Width - borderWidth * 2, rectangle.Height - borderHeight * 2);

            this.Inner = new DrawableRectangle(texture, innerRectangle, color);

            this.OnClick = onClickAction;
        }

        #endregion

        #region Properties

        public DrawableRectangle Outer { get; }
        public DrawableRectangle Inner { get; }
        protected string Label { get; set; }
        protected event Action OnClick;

        protected bool HasMouse;

        #endregion

        #region Methods

        /// <summary> Gets whether or not the provided <see cref="Vector2"/> lies within the bounds of this button. </summary>
        public bool Contains(Vector2 position) =>
            this.Outer.Contains(position);

        public void Update(Vector2 mousePosition)
        {
            this.HasMouse = this.Contains(mousePosition);
        }

        public void Draw(SpriteBatch spriteBatch, float depth = 0f)
        {
            if (this.HasMouse)
            {
                this.Outer.Draw(spriteBatch, Color.Multiply(Color.PapayaWhip, 1.1f));
                this.Inner.Draw(spriteBatch, Color.Multiply(Color.PapayaWhip, .9f));
            }
            else
            {
                this.Outer.Draw(spriteBatch, Color.PapayaWhip);
                this.Inner.Draw(spriteBatch, Color.Multiply(Color.PapayaWhip, .8f));
            }
        }

        #endregion
    }
}