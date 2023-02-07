using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Controls;
using Mogi.Extensions;
using Mogi.Inversion;
using System;
using System.Text;

namespace Hex.Helpers
{
    public class JournalHelper : IDraw<ControlDraw>
    {
        #region Constructors

        public JournalHelper(ContentManager content, SpriteFont font)
        {
            this.Font = font;
            this.Texture = content.Load<Texture2D>("Graphics/panel");

            this.Builder = new StringBuilder();
            this.AppendLine($"The day is {DateTime.Today.ToLongDateString()}.");

            this.Panel = new Panel<NormalUpdate, ControlDraw>(isActive: true);
        }

        #endregion

        #region Properties

        protected SpriteFont Font { get; }
        protected Texture2D Texture { get; }

        protected StringBuilder Builder { get; }
        protected string Content { get; set; }

        protected Panel<NormalUpdate, ControlDraw> Panel { get; }

        #endregion

        #region Methods

        public void Arrange(Rectangle container)
        {
            this.Panel.Reset();

            this.Panel.Append(new Patch(container, this.Texture, border: 10, new Color(162, 178, 204))
                // .WithInput(this.Input)
                // .With(control =>
                // {
                //     control.OnMouseEnter += x => x.Recolor(Color.MediumSlateBlue);
                //     control.OnMouseLeave += x => x.Recolor(new Color(162, 178, 204));
                // })
            );
            this.Panel.Append(new Label(container.Move(new Point(16, 20)), this.Font, () => this.Content));
        }

        public void AppendLine(string line)
        {
            this.Builder.AppendLine(line);
            this.Content = this.Builder.ToString();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Panel.Draw(spriteBatch);
        }

        #endregion
    }
}