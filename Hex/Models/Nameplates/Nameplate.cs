using System;
using System.Linq;
using Extended.Generators;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Controls;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Models.Nameplates
{
    public class Nameplate : IActivate, IDraw
    {
        #region Constructors

        public Nameplate(Rectangle container, SpriteFont font, Texture2D plateTexture, Texture2D portraitTexture,
            string name, bool hideNumbers = false)
        {
            this.Font = font;
            this.PlateTexture = plateTexture;
            this.PortraitTexture = portraitTexture;
            this.Name = name;
            this.HideNumbers = hideNumbers;

            this.PortraitPanel = new Panel<NormalUpdate, PortraitDraw>(isActive: true);

            var width = container.Width / 4;
            var rectangle = new Rectangle(container.Location, new Point(width, container.Height));

            this.PlatePatch = new Patch(rectangle, plateTexture, border: 5, Color.SeaGreen);
            this.PortraitPanel.Append(this.PlatePatch);

            var portraitRectangle = new Rectangle(rectangle.Location.X + (int) Math.Ceiling(rectangle.Width * (15 / 32f)),
                rectangle.Location.Y + (int) Math.Ceiling(rectangle.Height * (3 / 32f)),
                width / 2, width / 2);
            var portraitOutlineRectangle = new Rectangle(portraitRectangle.Location - new Point(2, 2),
                portraitRectangle.Size + new Point(4, 4));

            this.PortraitPanel.Append(new Basic(portraitOutlineRectangle, plateTexture, Color.Gainsboro));
            this.PortraitPanel.Append(new Basic(portraitRectangle, portraitTexture));

            var nameRectangle = portraitRectangle.Relocate(new Point(rectangle.Location.X + rectangle.Width / 8, portraitRectangle.Location.Y + rectangle.Height / 16));
            this.NameLabel = new Label(nameRectangle, this.Font, () => this.Name);
            this.PortraitPanel.Append(this.NameLabel);

            if (!hideNumbers)
            {
                var healthRectangle = new Rectangle(rectangle.Location.X + rectangle.Width / 8,
                    rectangle.Location.Y + rectangle.Height / 2,
                    width / 4, rectangle.Height / 8);
                this.HealthLabel = new Label(healthRectangle, this.Font, "40/40");
                this.PortraitPanel.Append(this.HealthLabel);

                var manaRectangle = healthRectangle.Move(new Point(0, (int) Math.Ceiling(healthRectangle.Height * 4 / 3f)));
                this.ManaLabel = new Label(manaRectangle, this.Font, "10/10");
                this.PortraitPanel.Append(this.ManaLabel);

                var levelRectangle = manaRectangle.Move(new Point(0, (int) Math.Ceiling(manaRectangle.Height * 4 / 3f)));
                var levelLable = new Label(levelRectangle, this.Font, "L20");
                this.PortraitPanel.Append(levelLable);
            }
        }

        #endregion

        #region Properties

        public bool IsActive { get; protected set; }

        protected SpriteFont Font { get; }

        protected Texture2D PlateTexture { get; }
        protected Texture2D PortraitTexture { get; }

        protected string Name { get; }

        // protected Panel<NormalUpdate, ControlDraw> PlatePanel { get; set; }
        protected Panel<NormalUpdate, PortraitDraw> PortraitPanel { get; }
        public Patch PlatePatch { get; }
        public Label NameLabel { get; }
        public Label HealthLabel { get; }
        public Label ManaLabel { get; }

        protected bool HideNumbers { get; set; }
        protected Point Location { get; set; }

        #endregion

        #region Methods

        public void Activate()
        {
            this.IsActive = true;
        }

        public void Deactivate()
        {
            this.IsActive = false;
        }

        public void Focus()
        {
            this.PlatePatch.Enlarge(new Point(0, 16));
            this.PortraitPanel.Move(new Point(0, -16));
        }

        public void Unfocus()
        {
            this.PlatePatch.Enlarge(new Point(0, -16));
            this.PortraitPanel.Move(new Point(0, 16));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.PortraitPanel.Draw(spriteBatch);
        }

        #endregion
    }
}