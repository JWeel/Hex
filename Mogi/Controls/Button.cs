using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Enums;
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
        private static readonly Color DEFAULT_COLOR_WHEN_PRESSING = new Color(225, 225, 225);

        private const float COLOR_MULTIPLIER_BRIGHTEN = 1.2f;
        private const float COLOR_MULTIPLIER_DARKEN = 0.9f;

        #endregion

        #region Constructors

        /// <summary> Initializes a new instance with a target destination and a texture. Overlay colors for different button states will use default values. </summary>
        /// <remarks> The default overlay values are: ordinary=235; hovering=255; pressing=225. </remarks>
        public Button(Rectangle destination, Texture2D texture)
            : this(destination, texture, DEFAULT_COLOR_WHEN_ORDINARY, DEFAULT_COLOR_WHEN_HOVERING, DEFAULT_COLOR_WHEN_PRESSING)
        {
        }

        /// <summary> Initializes a new instance with a target destination and an overlay color for the 'ordinary' button state. Overlay colors for 'hovering' and 'pressing' states will be calculated automatically. </summary>
        /// <remarks> The multipliers used for the other buttons states are: hovering=1.2; pressing=0.9. Colors cannot be brightened or darkened beyond max/min values. </remarks>
        public Button(Rectangle destination, Color baseColor)
            : this(destination, texture: default, baseColor)
        {
        }

        /// <summary> Initializes a new instance with a target destination, a texture, and an overlay color for the 'ordinary' button state. Overlay colors for 'hovering' and 'pressing' states will be calculated automatically. </summary>
        /// <remarks> The multipliers used for the other buttons states are: hovering=1.2; pressing=0.9. Colors cannot be brightened or darkened beyond max/min values. </remarks>
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

        public Button NeighborLeft { get; protected set; }
        public Button NeighborAbove { get; protected set; }
        public Button NeighborRight { get; protected set; }
        public Button NeighborBelow { get; protected set; }

        protected Color ColorWhenHovering { get; }
        protected Color ColorWhenPressing { get; }

        protected bool PressedMouse { get; set; }
        protected bool PressingMouse { get; set; }

        // private Func<bool> _mouseLeftDownGetter;
        protected Func<bool> MouseLeftDownGetter { get; set; }
        // {
        //     get
        //     {
        //         if (_mouseLeftDownGetter == null)
        //         {
        //             _mouseLeftDownGetter = () => Mouse.GetState().LeftButton.IsPressed();
        //         }
        //         return _mouseLeftDownGetter;
        //     }
        //     set => _mouseLeftDownGetter = value;
        // }

        #endregion

        #region Members

        public event Action<Button> OnClick;

        #endregion

        #region Overridden Methods

        public override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;
                
            base.Update(gameTime);

            if (this.MouseLeftDownGetter == null)
                return;

            this.PressedMouse = this.PressingMouse;
            this.PressingMouse = (this.ContainsMouse && this.MouseLeftDownGetter());

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

        public override Button WithInput(InputHelper input)
        {
            this.MouseLeftDownGetter = () => input.MouseDown(MouseButton.Left);
            return base.WithInput(input);
        }

        public Button WithMouseEnter(Action<Button> action)
        {
            this.OnMouseEnter += action;
            return this;
        }

        public Button WithMouseLeave(Action<Button> action)
        {
            this.OnMouseLeave += action;
            return this;
        }

        public Button WithClick(Action<Button> action)
        {
            this.OnClick += action;
            return this;
        }

        public Button WithNeighbor(Button neighbor, NeighborDirection direction)
        {
            switch (direction)
            {
                case NeighborDirection.Left:
                    this.NeighborLeft = neighbor;
                    neighbor.NeighborRight = this;
                    break;
                case NeighborDirection.Right:
                    this.NeighborRight = neighbor;
                    neighbor.NeighborLeft = this;
                    break;
                case NeighborDirection.Above:
                    this.NeighborAbove = neighbor;
                    neighbor.NeighborBelow = this;
                    break;
                case NeighborDirection.Below:
                    this.NeighborBelow = neighbor;
                    neighbor.NeighborAbove = this;
                    break;
                default:
                    throw direction.Invalid();
            }
            return this;
        }

        #endregion
    }
}