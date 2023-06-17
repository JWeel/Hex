using Extended.Extensions;
using Extended.Generators;
using Hex.Models.Nameplates;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Helpers
{
    public class NameplateHelper : IActivate, IUpdate<NormalUpdate>, IDraw<PortraitDraw>
    {
        #region Constructors

        public NameplateHelper(InputHelper input, SpriteFont font, ContentManager content)
        {
            this.Nameplates = new List<Nameplate>();
            this.Input = input;
            this.Font = font;
            this.PortraitTextures = Numeric.Range(1, 28)
                .Select(n => content.Load<Texture2D>($"graphics/portraits/{n:00}"))
                .ToArray();
            this.ButtonTexture = content.Load<Texture2D>("graphics/encounter/button");
        }

        #endregion

        #region Properties

        public bool IsActive { get; protected set; }

        protected List<Nameplate> Nameplates { get; }
        protected InputHelper Input { get; }
        protected SpriteFont Font { get; }
        protected Texture2D ButtonTexture { get; }
        protected Texture2D[] PortraitTextures { get; }

        protected Nameplate CurrentNameplate { get; set; }

        #endregion

        #region Methods

        public void Arrange(Rectangle container)
        {
            var random = new Random();

            var index1 = random.Next(24);
            var nameplate1 = new Nameplate(container, this.Font, this.ButtonTexture, this.PortraitTextures[index1], $"{index1 + 1:00}.png");
            this.Nameplates.Add(nameplate1);

            var index2 = random.Next(24);
            var nameplate2 = new Nameplate(container.Move(new Point(container.Width / 4, 0)), this.Font, this.ButtonTexture, this.PortraitTextures[index2], $"{index2 + 1:00}.png");
            this.Nameplates.Add(nameplate2);

            var index3 = random.Next(24);
            var nameplate3 = new Nameplate(container.Move(new Point(container.Width / 2, 0)), this.Font, this.ButtonTexture, this.PortraitTextures[index3], $"{index3 + 1:00}.png");
            this.Nameplates.Add(nameplate3);

            var index4 = random.Next(24);
            var nameplate4 = new Nameplate(container.Move(new Point(container.Width / 4 * 3, 0)), this.Font, this.ButtonTexture, this.PortraitTextures[index4], $"{index4 + 1:00}.png");
            this.Nameplates.Add(nameplate4);

            var rectangle = new Rectangle(250, 300, (int) Math.Ceiling(container.Width * (3 / 4f)), (int) Math.Ceiling(container.Height * 3 / 4f));
            this.Nameplates.Add(new Nameplate(rectangle, this.Font, this.ButtonTexture, this.PortraitTextures[25], "Orc", hideNumbers: true));
            var rectangle2 = new Rectangle(400, 300, (int) Math.Ceiling(container.Width * (3 / 4f)), (int) Math.Ceiling(container.Height * 3 / 4f));
            this.Nameplates.Add(new Nameplate(rectangle2, this.Font, this.ButtonTexture, this.PortraitTextures[25], "Orc", hideNumbers: true));

            this.Focus(nameplate1);
        }

        public void Activate()
        {
            this.IsActive = true;
            this.Nameplates.Each(nameplate => nameplate.Activate());
        }

        public void Deactivate()
        {
            this.IsActive = false;
            this.Nameplates.Each(nameplate => nameplate.Deactivate());
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.U))
                this.Focus(this.Nameplates[0]);
            if (this.Input.KeyPressed(Keys.I))
                this.Focus(this.Nameplates[1]);
            if (this.Input.KeyPressed(Keys.O))
                this.Focus(this.Nameplates[2]);
            if (this.Input.KeyPressed(Keys.P))
                this.Focus(this.Nameplates[3]);
        }

        // void IDraw<ControlDraw>.Draw(SpriteBatch spriteBatch)
        // {
        //     this.PlatePanel.Draw(spriteBatch);
        // }

        void IDraw<PortraitDraw>.Draw(SpriteBatch spriteBatch)
        {
            this.Nameplates.Each(nameplate => nameplate.Draw(spriteBatch));
        }

        #endregion

        #region Helper Methods

        protected void Focus(Nameplate nameplate)
        {
            if (this.CurrentNameplate == nameplate)
                return;
            this.CurrentNameplate?.Unfocus();
            nameplate.Focus();
            this.CurrentNameplate = nameplate;
        }

        #endregion
    }
}