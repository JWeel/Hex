using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using System;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that can be interacted with by clicking. </summary>
    public class Button : Control<Button>
    {
        #region Constants

        private static readonly Color DEFAULT_COLOR_WHEN_ORDINARY = new Color(235, 235, 235);
        private static readonly Color DEFAULT_COLOR_WHEN_HOVERING = new Color(255, 255, 255);
        private static readonly Color DEFAULT_COLOR_WHEN_PRESSING = new Color(215, 215, 215);

        private const float COLOR_MULTIPLIER_BRIGHTEN = 1.2f;
        private const float COLOR_MULTIPLIER_DARKEN = 0.8f;

        #endregion

        #region Constructors

        /// <summary> Initializes a new instance with a target destination and a texture. Overlay colors for different button states will use default values. </summary>
        /// <remarks> The default overlay values are: ordinary=235; hovering=255; pressing=215. </remarks>
        public Button(Rectangle destination, Texture2D texture)
            : this(destination, texture, DEFAULT_COLOR_WHEN_ORDINARY, DEFAULT_COLOR_WHEN_HOVERING, DEFAULT_COLOR_WHEN_PRESSING)
        {
        }

        /// <summary> Initializes a new instance with a target destination and an overlay color for the 'ordinary' button state. Overlay colors for 'hovering' and 'pressing' states will be calculated automatically. </summary>
        /// <remarks> The multipliers used for the other buttons states are: hovering=1.2; pressing=0.8. Colors cannot be brightened or darkened beyond max/min values. </remarks>
        public Button(Rectangle destination, Color baseColor)
            : this(destination, texture: default, baseColor)
        {
        }

        /// <summary> Initializes a new instance with a target destination, a texture, and an overlay color for the 'ordinary' button state. Overlay colors for 'hovering' and 'pressing' states will be calculated automatically. </summary>
        /// <remarks> The multipliers used for the other buttons states are: hovering=1.2; pressing=0.8. Colors cannot be brightened or darkened beyond max/min values. </remarks>
        public Button(Rectangle destination, Texture2D texture, Color baseColor)
            : this(destination, texture, baseColor, Color.Multiply(baseColor, COLOR_MULTIPLIER_BRIGHTEN), Color.Multiply(baseColor, COLOR_MULTIPLIER_DARKEN))
        {
        }

        /// <summary> Initializes a new instance with a target destination and overlay colors for each button state. </summary>
        public Button(Rectangle destination, Color colorWhenOrdinary, Color colorWhenHovering, Color colorWhenPressing)
            : this(destination, texture: default, colorWhenOrdinary, colorWhenHovering, colorWhenPressing)
        {
        }

        /// <summary> Initializes a new instance with a target destination, a texture, and overlay colors for each button state. </summary>
        public Button(Rectangle destination, Texture2D texture, Color colorWhenOrdinary, Color colorWhenHovering, Color colorWhenPressing)
            : base(destination, texture, colorWhenOrdinary)
        {
            this.ColorWhenHovering = colorWhenHovering;
            this.ColorWhenPressing = colorWhenPressing;
        }

        #endregion

        #region Properties

        protected Color ColorWhenHovering { get; }
        protected Color ColorWhenPressing { get; }

        protected bool PressedMouse { get; set; }
        protected bool PressingMouse { get; set; }

        private Func<bool> _mouseLeftClickGetter;
        protected Func<bool> MouseLeftClickedGetter
        {
            get
            {
                if (_mouseLeftClickGetter == null)
                {
                    _mouseLeftClickGetter = () => Mouse.GetState().LeftButton.IsPressed();
                }
                return _mouseLeftClickGetter;
            }
            set => _mouseLeftClickGetter = value;
        }

        #endregion

        #region Members

        public event Action<Button> OnClick;

        #endregion

        #region Overridden Methods

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.PressedMouse = this.PressingMouse;
            this.PressingMouse = (this.ContainsMouse && this.MouseLeftClickedGetter());

            if (this.PressedMouse && !this.PressingMouse && this.ContainsMouse)
                this.OnClick?.Invoke(this);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (this.PressingMouse)
                base.Draw(spriteBatch, this.ColorWhenPressing);
            else if (this.ContainsMouse)
                base.Draw(spriteBatch, this.ColorWhenHovering);
            else
                base.Draw(spriteBatch);
        }

        public override void WithInput(InputHelper input)
        {
            base.WithInput(input);
            _mouseLeftClickGetter = () => input.MousePressed(MouseButton.Left);
        }

        #endregion
    }
}