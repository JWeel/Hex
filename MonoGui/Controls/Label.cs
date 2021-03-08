using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGui.Extensions;

namespace MonoGui.Controls
{
    /// <summary> Represents a graphical element that displays text. </summary>
    public class Label : Control<Patch>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area, a font renderer, and a text value to display. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text)
            : this(destination, spriteFont, text, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, and a scaling factor for the font. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, float scale)
            : this(destination, spriteFont, text, scale, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, Color color)
            : this(destination, spriteFont, text, scale: 1f, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a font renderer, a text value to display, a scaling factor for the font, and a color in which to display the text. </summary>
        public Label(Rectangle destination, SpriteFont spriteFont, string text, float scale, Color color)
            : base(destination, texture: default, color)
        {
            this.Location = destination.Location.ToVector2();
            this.SpriteFont = spriteFont;
            this.Text = text;
            this.Scale = scale;
        }

        #endregion

        #region Properties

        protected Vector2 Location { get; }
        protected SpriteFont SpriteFont { get; }
        protected string Text { get; set; }
        protected float Scale { get; set; }

        #endregion

        #region Overriden Methods

        public override void Draw(SpriteBatch spriteBatch, Color color)
        {
            spriteBatch.DrawText(this.SpriteFont, this.Text, this.Location, color, this.Scale);
        }

        #endregion
    }
}