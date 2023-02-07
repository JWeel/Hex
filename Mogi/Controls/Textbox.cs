using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using Mogi.Helpers;
using System;
using System.Text;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that contains text which can be scrolled through. </summary>
    public class Textbox : Control<Textbox>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area, a font renderer, and two scroll textures. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture)
            : this(destination, spriteFont, scrollUpTexture, scrollDownTexture, scrollButtonColor: Color.Gainsboro)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, and two scroll textures. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture, Color scrollButtonColor)
            : this(destination, spriteFont, scrollUpTexture, scrollDownTexture, scrollButtonColor, scale: 1f)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, two scroll textures, and a scaling factor for the font. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture, Color scrollButtonColor, float scale)
            : this(destination, spriteFont, scrollUpTexture, scrollDownTexture, scrollButtonColor, scale, fontColor: Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, two scroll textures, a scaling factor for the font, and a color in which to display the text. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture, Color scrollButtonColor, float scale, Color fontColor)
            : this(destination, spriteFont, scrollUpTexture, scrollDownTexture, scrollButtonColor, scale, fontColor, scrollButtonSize: new Point(16, 16))
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, two scroll textures, a scaling factor for the font, a color in which to display the text, and the width of the scrollbar. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture, Color scrollButtonColor, float scale, Color fontColor, Point scrollButtonSize)
            : this(destination, spriteFont, scrollUpTexture, scrollDownTexture, scrollButtonColor, scale, fontColor, scrollButtonSize, initialText: default)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, two scroll textures, a scaling factor for the font, a color in which to display the text, the width of the scrollbar, and the initial text. </summary>
        public Textbox(Rectangle destination, SpriteFont spriteFont, Texture2D scrollUpTexture, Texture2D scrollDownTexture, Color scrollButtonColor, float scale, Color fontColor, Point scrollButtonSize, string initialText)
            : base(destination, texture: default, fontColor)
        {
            this.Location = destination.Location.ToVector2();
            this.SpriteFont = spriteFont;
            this.ScrollButtonSize = scrollButtonSize;
            this.ScrollUpButton = new Button(destination, scrollUpTexture, scrollButtonColor);
            this.ScrollUpButton.OnClick += this.ScrollUpClick;
            this.ScrollDownButton = new Button(destination, scrollDownTexture, scrollButtonColor);
            this.ScrollDownButton.OnClick += this.ScrollDownClick;
            this.RecalculateSizes(destination);

            this.Scale = scale;
            this.Builder = new StringBuilder();
            if (!initialText.IsNullOrEmpty())
                this.AppendLine(initialText);
        }

        #endregion

        #region Properties

        protected Vector2 Location { get; }
        protected SpriteFont SpriteFont { get; }
        protected Point ScrollButtonSize { get; }
        protected Button ScrollUpButton { get; }
        protected Button ScrollDownButton { get; }
        protected StringBuilder Builder { get; }

        protected float Scale { get; set; }
        protected int WritableWidth { get; set; }
        protected int ScrollingOffset { get; set; }
        protected string Content { get; set; }

        // private Func<bool> _mouseScrolledUpGetter;
        protected Func<bool> MouseScrolledUpGetter { get; set; }
        // {
        //     get
        //     {
        //         if (_mouseScrolledUpGetter == null)
        //         {
        //             _mouseScrolledUpGetter = () => false;
        //         }
        //         return _mouseScrolledUpGetter;
        //     }
        //     set => _mouseScrolledUpGetter = value;
        // }

        // private Func<bool> _mouseScrolledDownGetter;
        protected Func<bool> MouseScrolledDownGetter { get; set; }
        // {
        //     get
        //     {
        //         if (_mouseScrolledDownGetter == null)
        //         {
        //             _mouseScrolledDownGetter = () => false;
        //         }
        //         return _mouseScrolledDownGetter;
        //     }
        //     set => _mouseScrolledDownGetter = value;
        // }

        #endregion

        #region Public Methods

        public void AppendLine(string text)
        {
            this.Builder.AppendLine(text);
            this.Content = this.Builder.ToString();

            // TODO recalculate total text height, also set position to show newest line
        }

        public override void Relocate(Rectangle rectangle)
        {
            base.Relocate(rectangle);
            this.RecalculateSizes(rectangle);
        }

        public override Textbox WithInput(InputHelper input)
        {
            this.MouseScrolledUpGetter = input.MouseScrolledUp;
            this.MouseScrolledDownGetter = input.MouseScrolledDown;
            this.ScrollUpButton.WithInput(input);
            this.ScrollDownButton.WithInput(input);
            return base.WithInput(input);
        }

        public override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;

            base.Update(gameTime);
            this.ScrollUpButton.Update(gameTime);
            this.ScrollDownButton.Update(gameTime);

            if ((this.MouseScrolledUpGetter == null) || (this.MouseScrolledDownGetter == null))
                return;

            if (this.MouseScrolledUpGetter())
                this.ScrollUpClick(default);
            if (this.MouseScrolledDownGetter())
                this.ScrollDownClick(default);
        }

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            spriteBatch.DrawText(this.SpriteFont, this.Content, this.Location, color, this.Scale);
            this.ScrollUpButton.Draw(spriteBatch);
            this.ScrollDownButton.Draw(spriteBatch);
        }

        #endregion

        #region Protected Methods

        protected void RecalculateSizes(Rectangle rectangle)
        {
            this.WritableWidth = rectangle.Width - this.ScrollButtonSize.X;
            this.ScrollUpButton.Relocate(new Rectangle(
                rectangle.X + rectangle.Width - this.ScrollButtonSize.X,
                rectangle.Y,
                this.ScrollButtonSize.X,
                this.ScrollButtonSize.Y));
            this.ScrollDownButton.Relocate(new Rectangle(
                rectangle.X + rectangle.Width - this.ScrollButtonSize.X,
                rectangle.Y + rectangle.Height - this.ScrollButtonSize.Y,
                this.ScrollButtonSize.X,
                this.ScrollButtonSize.Y));
        }

        protected void ScrollUpClick(Button button)
        {
            // set scroll position clamped to 0
        }

        protected void ScrollDownClick(Button button)
        {
            // set scroll position clamped to calculated max from AppendLine
        }

        #endregion
    }
}