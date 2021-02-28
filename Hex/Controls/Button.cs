using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Hex.Controls
{
    public class Button
    {
        #region Constructors

        public Button(Rectangle rectangle, Color color, string label = null, int borderSize = 2, Action onClickAction = null)
        {
            this.Label = label ?? string.Empty;

            this.OuterRectangle = rectangle;

            var borderWidth = Math.Clamp(borderSize, 0, rectangle.Width);
            var borderHeight = Math.Clamp(borderSize, 0, rectangle.Height);
            this.InnerRectangle = new Rectangle(rectangle.X + borderWidth, rectangle.Y + borderHeight, rectangle.Width - borderWidth*2, rectangle.Height - borderHeight*2);

            this.OuterColor = Color.Multiply(color, .75f);
            this.InnerColor = color;

            this.OnClick = onClickAction;
        }

        #endregion

        #region Properties

        protected string Label { get; set; }
        protected Rectangle OuterRectangle { get; }
        protected Rectangle InnerRectangle { get; }
        protected Color OuterColor { get; }
        protected Color InnerColor { get; }
        protected event Action OnClick;

        #endregion

        #region Methods

        /// <summary> Gets whether or not the provided <see cref="Vector2"/> lies within the bounds of this button. </summary>
        public bool Contains(Vector2 position) =>
            this.OuterRectangle.Contains(position);

        public void Update(Vector2 mousePosition)
        {

        }

        public IEnumerable<(Rectangle Rectangle, Color Color)> EnumerateDrawInfo()
        {
            // TODO depend on state -> change color
            yield return (this.OuterRectangle, this.OuterColor);
            yield return (this.InnerRectangle, this.InnerColor);
        }

        #endregion
    }

    public enum ButtonState
    {
        Normal,
        Hover,
        Click,
        Disabled,
        Hidden
    }
}