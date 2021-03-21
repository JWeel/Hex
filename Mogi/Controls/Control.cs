using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;
using Mogi.Helpers;
using System;

namespace Mogi.Controls
{
    /// <summary> Base class for defining a graphical user interface element. </summary>
    public abstract class Control<T> : IControl
        where T : Control<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area. A texture will be assigned the first time the control is drawn. </summary>
        /// <param name="destination"> The destination area in the client window where the control will be drawn. </param>
        public Control(Rectangle destination)
            : this(destination, texture: default)
        {
        }

        /// <summary> Initializes a new instance with a destination area and a base overlay color. A texture will be assigned the first time the control is drawn. </summary>
        /// <param name="destination"> The destination area in the client window where the control will be drawn. </param>
        /// <param name="color"> A base color used to overlay the drawn texture when no other color is provided. </param>
        public Control(Rectangle destination, Color color)
            : this(destination, texture: default, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area and a texture. </summary>
        /// <param name="destination"> The destination area in the client window where the control will be drawn. </param>
        /// <param name="texture"> The texture that will be drawn to the destination area. </param>
        public Control(Rectangle destination, Texture2D texture)
            : this(destination, texture, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a texture, and a base overlay color. </summary>
        /// <param name="destination"> The destination area in the client window where the control will be drawn. </param>
        /// <param name="texture"> The texture that will be drawn to the destination area. </param>
        /// <param name="color"> A base color used to overlay the drawn texture when no other color is provided. </param>
        public Control(Rectangle destination, Texture2D texture, Color color)
        {
            this.Destination = destination;
            this.Texture = texture;
            this.Color = color;
        }

        #endregion

        #region Members

        public event Action<Control<T>> OnMouseEnter;
        public event Action<Control<T>> OnMouseLeave;

        public bool IsActive { get; protected set; }

        protected Rectangle Destination { get; set; }
        protected Texture2D Texture { get; set; }
        protected Color Color { get; set; }

        /// <summary> Determines the priority of drawing this control. </summary>
        /// <remarks> Note that out of the box this is used only when drawing standalone controls using <see cref="DependencyHelper"/>. Controls contained in a <see cref="Panel"/> are still drawn according to panel ordering. </remarks>
        protected int Priority { get; set; }

        protected bool ContainedMouse { get; set; }
        protected bool ContainsMouse { get; set; }

        private Func<Vector2> _mousePositionGetter;
        protected Func<Vector2> MousePositionGetter
        {
            get
            {
                if (_mousePositionGetter == null)
                {
                    _mousePositionGetter = () => Mouse.GetState().ToVector2();
                }
                return _mousePositionGetter;
            }
            set => _mousePositionGetter = value;
        }

        #endregion

        #region Methods

        /// <summary> Toggles the <see cref="IsActive"/> state of the control. </summary>
        public void Toggle() =>
            this.IsActive = !this.IsActive;

        /// <summary> Updates the state of the control. </summary>
        public virtual void Update(GameTime gameTime)
        {
            this.ContainedMouse = this.ContainsMouse;
            this.ContainsMouse = this.Destination.Contains(this.MousePositionGetter());

            if (!this.ContainedMouse && this.ContainsMouse)
                this.OnMouseEnter?.Invoke(this);
            if (this.ContainedMouse && !this.ContainsMouse)
                this.OnMouseLeave?.Invoke(this);
        }

        /// <summary> Draws the control using the specified spritebatch. </summary>
        public virtual void Draw(SpriteBatch spriteBatch) =>
            this.Draw(spriteBatch, this.Color);

        /// <summary> Draws the control using the specified spritebatch with a specified overlay color. </summary>
        public virtual void Draw(SpriteBatch spriteBatch, Color color)
        {
            if (color.IsTransparent())
                return;

            if (this.Texture == default)
            {
                // TBD if there are hundreds of controls why create new blank textures for each? 
                // TBD caching it statically will work but requires a non-generic containing class
                var blankTexture = new Texture2D(spriteBatch.GraphicsDevice, width: 1, height: 1);
                blankTexture.SetData(new Color[] { Color.White });
                this.Texture = blankTexture;
            }

            spriteBatch.DrawTo(this.Texture, this.Destination, color);
        }

        public virtual void WithInput(InputHelper input)
        {
            _mousePositionGetter = () => input.CurrentVirtualMouseVector;
        }

        #endregion

        #region IPrioritize Implementation

        public int GetPriority() => this.Priority;

        public int SetPriority(int value) => this.Priority = value;

        #endregion
    }
}