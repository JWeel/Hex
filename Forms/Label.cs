using Microsoft.Xna.Framework;

namespace Forms
{
    public class Label : Control
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }

        public Label()
        {
            BackgroundColor = Color.Transparent;
            TextColor = Color.Black;
        }

        internal override void Draw(DrawHelper helper, Vector2 offset)
        {
            var txtSize = helper.MeasureString(FontName, Text);
            var rectangle = new Rectangle((int) Location.X, (int) Location.Y, (int) txtSize.X, (int) txtSize.Y);
            rectangle.Offset(offset);

            if (BackgroundColor != Color.Transparent)
                helper.DrawRectangle(rectangle, BackgroundColor);
            helper.DrawString(this, Location + offset, Text, TextColor);
        }
    }
}