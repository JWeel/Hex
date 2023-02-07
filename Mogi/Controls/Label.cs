using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using System;

namespace Mogi.Controls
{
    /// <summary> Represents a graphical element that displays text. </summary>
    public class Label : Control<Label>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area, a font renderer, and a text value to display. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text)
            : this(destination, spriteFont, () => text, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, and a function that returns a text value to display. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, Func<string> textFunc)
            : this(destination, spriteFont, textFunc, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, and a scaling factor for the font. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, float scale)
            : this(destination, spriteFont, () => text, scale, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a function that returns a text value to display, and a scaling factor for the font. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, Func<string> textFunc, float scale)
            : this(destination, spriteFont, textFunc, scale, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, Color color)
            : this(destination, spriteFont, () => text, scale: 1f, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a function that returns a text value to display, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, Func<string> textFunc, Color color)
            : this(destination, spriteFont, textFunc, scale: 1f, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, a scaling factor for the font, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, float scale, Color color)
            : this(destination, spriteFont, () => text, scale, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a function that returns a text value to display, a scaling factor for the font, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, Func<string> textFunc, float scale, Color color)
            : base(destination, texture: default, color)
        {
            this.Location = destination.Location.ToVector2();
            this.SpriteFont = spriteFont;
            this.TextFunc = textFunc;
            this.Scale = scale;
        }

        #endregion

        #region Properties

        protected Vector2 Location { get; }
        protected SpriteFont SpriteFont { get; }
        protected Func<string> TextFunc { get; set; }
        protected float Scale { get; set; }

        #endregion

        #region Public Methods

        public void SetText(string text) =>
            this.SetText(() => text);

        public void SetText(Func<string> textFunc) =>
            this.TextFunc = textFunc;

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            var text = this.TextFunc();
            spriteBatch.DrawText(this.SpriteFont, text, this.Location, color, this.Scale);
        }

        #endregion
    }
}