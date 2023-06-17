using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using Mogi.Helpers;
using System;

namespace Mogi.Controls
{
    /// <summary> Base class for defining a graphical user interface element. </summary>
    public abstract class Control<TControl> : IControl
        where TControl : Control<TControl>
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
            this.IsActive = true;
        }

        #endregion

        #region Members

        public event Action<TControl> OnMouseEnter;
        public event Action<TControl> OnMouseLeave;

        /// <summary> A value that can be used to store custom information about this control. </summary>
        public object Tag { get; set; }

        /// <summary> The identifying name of the instance. </summary>
        public string Name { get; protected set; }

        public bool IsActive { get; protected set; }

        public Rectangle Destination { get; protected set; }
        protected Texture2D Texture { get; set; }
        protected Color Color { get; set; }

        protected bool ContainedMouse { get; set; }
        protected bool ContainsMouse { get; set; }

        // private Func<Vector2> _mousePositionGetter;
        protected Func<Vector2> MousePositionGetter { get; set; }
        // {
        //     get
        //     {
        //         if (_mousePositionGetter == null)
        //         {
        //             _mousePositionGetter = () => Mouse.GetState().ToVector2();
        //         }
        //         return _mousePositionGetter;
        //     }
        //     set => _mousePositionGetter = value;
        // }

        #endregion

        #region Methods

        /// <summary> Toggles the <see cref="IsActive"/> state of the control. </summary>
        public virtual void Toggle() =>
            this.IsActive = !this.IsActive;

        /// <summary> Set the <see cref="IsActive"/> state to true. </summary>
        public virtual void Activate() =>
            this.IsActive = true;

        /// <summary> Set the <see cref="IsActive"/> state to false. </summary>
        public virtual void Deactivate() =>
            this.IsActive = false;

        public virtual void Move(Point movement) =>
            this.Relocate(this.Destination.Move(movement));

        public virtual void Enlarge(Point delta) =>
            this.Relocate(this.Destination.Enlarge(delta));

        public virtual void Relocate(Rectangle rectangle) =>
            this.Destination = rectangle;

        public void Recolor(Color color) =>
            this.Color = color;

        public void Retexture(Texture2D texture) =>
            this.Texture = texture;

        /// <summary> Updates the state of the control. </summary>
        public virtual void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;

            if (this.MousePositionGetter == null)
                return;

            this.ContainedMouse = this.ContainsMouse;
            this.ContainsMouse = this.Destination.Contains(this.MousePositionGetter());

            if (!this.ContainedMouse && this.ContainsMouse)
                this.OnMouseEnter?.Invoke(this);
            if (this.ContainedMouse && !this.ContainsMouse)
                this.OnMouseLeave?.Invoke(this);
        }

        /// <summary> Draws the control using the specified spritebatch. </summary>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!this.IsActive)
                return;
            this.Draw(spriteBatch, this.Color);
        }

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

        public virtual TControl WithInput(InputHelper input)
        {
            this.MousePositionGetter = () => input.CurrentVirtualMouseVector;
            return this;
        }

        public virtual TControl WithTag(object tag)
        {
            this.Tag = tag;
            return this;
        }

        public virtual TControl WithName(string name)
        {
            this.Name = name;
            return this;
        }

        /// <summary> Implicitly converts the control from its abstract type to its real type. </summary>
        public static implicit operator TControl(Control<TControl> control) =>
            (TControl) control;

        #endregion
    }
}