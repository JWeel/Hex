using Extended.Generators;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Controls;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using System;
using System.Linq;

namespace Hex.Helpers
{
    public class EncounterHelper : IRegister, IUpdate<NormalUpdate>, IDraw<ControlDraw>, IDraw<PortraitDraw>, IActivate
    {
        #region Constructors

        public EncounterHelper(InputHelper input, SpriteFont font, Texture2D blankTexture, ContentManager content)
        {
            this.Input = input;
            this.Font = font;
            this.BlankTexture = blankTexture;
            this.BackgroundTexture = content.Load<Texture2D>("graphics/encounter/backgroundPlains");
            this.ButtonTexture = content.Load<Texture2D>("graphics/encounter/button");
            this.SelectorTexture = content.Load<Texture2D>("graphics/encounter/selector");

            this.PortraitTextures = Numeric.Range(1, 21)
                .Select(n => content.Load<Texture2D>($"graphics/portraits/{n:00}"))
                .ToArray();
        }

        #endregion

        #region Data Members

        public bool IsActive { get; protected set; }

        protected InputHelper Input { get; }
        protected SpriteFont Font { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D BackgroundTexture { get; }
        protected Texture2D ButtonTexture { get; }
        protected Texture2D SelectorTexture { get; }

        protected Texture2D[] PortraitTextures { get; }

        protected Basic Background { get; set; }

        protected Panel<NormalUpdate, ControlDraw> BarOfBars { get; set; }
        protected Label BarLabel { get; set; }

        protected Panel<NormalUpdate, ControlDraw> PreparationBar { get; set; }

        protected Panel<NormalUpdate, ControlDraw> EngagementBar { get; set; }

        protected Panel<NormalUpdate, PortraitDraw> PortraitBar { get; set; }

        protected Blinker BarSelector { get; set; }
        protected Button SelectedButton { get; set; }

        protected Rectangle Container { get; set; }

        protected string BarLabelText { get; set; }

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            using (new DependencyScope(dependency))
            {
            }
        }

        public void Arrange(Rectangle container)
        {
            this.Container = container;

            var bottomHeight = (int) (this.Container.Size.Y * (13 / 100f));
            var topHeight = (int) (this.Container.Size.Y * (17 / 100f));
            var heightOffset = (int) (this.Container.Size.Y * (7 / 10f));
            var topRectangle = new Rectangle(this.Container.X, this.Container.Y + heightOffset, this.Container.Width, topHeight);
            var bottomRectangle = new Rectangle(this.Container.X, this.Container.Y + heightOffset + topHeight, this.Container.Width, bottomHeight);

            this.Background = new Basic(0, 0, this.Container.Width, heightOffset, this.BackgroundTexture);

            this.PreparationBar = new Panel<NormalUpdate, ControlDraw>(isActive: true);

            var bottomControlSize = (int) Math.Ceiling(this.Container.Size.X / 8d);
            var bottomLabelRectangle = new Rectangle(this.Container.X, bottomRectangle.Y, bottomControlSize * 2, bottomHeight);
            var labelOffset = new Point(bottomLabelRectangle.Size.X / 5, bottomLabelRectangle.Size.Y / 2 - 7);
            this.BarLabel = new Label(bottomLabelRectangle.Move(labelOffset), this.Font, () => this.BarLabelText, scale: 1.25f);
            this.BarLabelText = "Choose >>";

            var bottomButtonRectangle1 = new Rectangle(bottomLabelRectangle.X + bottomLabelRectangle.Width, bottomLabelRectangle.Y, bottomControlSize, bottomHeight);
            var bottomFightButton = new Button(bottomButtonRectangle1, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue)
                .WithInput(this.Input)
                .WithName("Engage")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick);

            var bottomButtonRectangle2 = new Rectangle(bottomButtonRectangle1.X + bottomButtonRectangle1.Width, bottomButtonRectangle1.Y, bottomButtonRectangle1.Width, bottomButtonRectangle1.Height);
            var bottomFlightButton = new Button(bottomButtonRectangle2, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue)
                .WithInput(this.Input)
                .WithName("Withdraw")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(bottomFightButton, NeighborDirection.Left);

            var bottomButtonRectangle3 = new Rectangle(bottomButtonRectangle2.X + bottomButtonRectangle1.Width, bottomButtonRectangle1.Y, bottomButtonRectangle1.Width, bottomButtonRectangle1.Height);
            var bottomButtonRectangle4 = new Rectangle(bottomButtonRectangle3.X + bottomButtonRectangle1.Width, bottomButtonRectangle1.Y, bottomButtonRectangle1.Width, bottomButtonRectangle1.Height);
            var bottomButtonRectangle5 = new Rectangle(bottomButtonRectangle4.X + bottomButtonRectangle1.Width, bottomButtonRectangle1.Y, bottomButtonRectangle1.Width, bottomButtonRectangle1.Height);
            var bottomButtonRectangle6 = new Rectangle(bottomButtonRectangle5.X + bottomButtonRectangle1.Width, bottomButtonRectangle1.Y, bottomButtonRectangle1.Width, bottomButtonRectangle1.Height);

            this.PreparationBar.Append(bottomFightButton);
            this.PreparationBar.Append(bottomFlightButton);

            this.EngagementBar = new Panel<NormalUpdate, ControlDraw>();
            // this.EngagementBar.Append(new Patch(topRectangle, this.ButtonTexture, 68, Color.SlateBlue));

            var engagementButton1 = new Button(bottomButtonRectangle1, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Strike")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick);
            this.EngagementBar.Append(engagementButton1);
            var engagementButton2 = new Button(bottomButtonRectangle2, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Cast")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(engagementButton1, NeighborDirection.Left);
            this.EngagementBar.Append(engagementButton2);
            var engagementButton3 = new Button(bottomButtonRectangle3, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Guard")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(engagementButton2, NeighborDirection.Left);
            this.EngagementBar.Append(engagementButton3);
            var engagementButton4 = new Button(bottomButtonRectangle4, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Use")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(engagementButton3, NeighborDirection.Left);
            this.EngagementBar.Append(engagementButton4);
            var engagementButton5 = new Button(bottomButtonRectangle5, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Analyze")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(engagementButton4, NeighborDirection.Left);
            this.EngagementBar.Append(engagementButton5);
            var engagementButton6 = new Button(bottomButtonRectangle6, this.ButtonTexture, Color.SlateBlue, new Color(146, 130, 225), Color.DarkSlateBlue).WithInput(this.Input)
                .WithName("Cancel")
                .WithMouseEnter(this.HandleButtonEnter)
                .WithMouseLeave(this.HandleButtonLeave)
                .WithClick(this.HandleClick)
                .WithNeighbor(engagementButton5, NeighborDirection.Left)
                .WithNeighbor(engagementButton1, NeighborDirection.Right);
            this.EngagementBar.Append(engagementButton6);

            this.PortraitBar = new Panel<NormalUpdate, PortraitDraw>();
            var characterPanel1 = this.GetCharacterPanel(topRectangle, topRectangle.Location);
            var characterPanel2 = this.GetCharacterPanel(topRectangle, topRectangle.Location + new Point(topRectangle.Width / 4, 0));
            var characterPanel3 = this.GetCharacterPanel(topRectangle, topRectangle.Location + new Point(topRectangle.Width / 2, 0));
            var characterPanel4 = this.GetCharacterPanel(topRectangle, topRectangle.Location + new Point(topRectangle.Width / 4 * 3, 0));
            this.PortraitBar.Append(characterPanel1);
            this.PortraitBar.Append(characterPanel2);
            this.PortraitBar.Append(characterPanel3);
            this.PortraitBar.Append(characterPanel4);

            this.EngagementBar.OnActivate += () => this.PortraitBar.Activate();
            this.EngagementBar.OnDeactivate += () => this.PortraitBar.Deactivate();

            this.SelectedButton = bottomFightButton;
            this.BarSelector = new Blinker(bottomButtonRectangle1,
                this.SelectorTexture, interval: 530d);

            this.BarOfBars = new Panel<NormalUpdate, ControlDraw>(isActive: true);
            this.BarOfBars.Append(new Basic(
                new Rectangle(topRectangle.X, topRectangle.Y, this.Container.Width, this.Container.Height - heightOffset),
                this.BlankTexture, new Color(48, 48, 46)));
            this.BarOfBars.Append(new Patch(bottomLabelRectangle, this.ButtonTexture, 8, Color.SlateBlue));
            this.BarOfBars.Append(this.BarLabel);
            this.BarOfBars.Append(this.EngagementBar);
            this.BarOfBars.Append(this.PreparationBar);
            this.BarOfBars.Append(this.BarSelector);
        }

        public void Update(GameTime gameTime)
        {
            this.BarOfBars.Update(gameTime);

            if (this.BarSelector.IsActive && (this.SelectedButton != null))
            {
                if (this.Input.KeyPressed(Keys.Enter))
                {
                    this.HandleClick(this.SelectedButton);
                }
                if (this.Input.KeysPressedAny(Keys.Right, Keys.D))
                {
                    var neighbor = this.SelectedButton.NeighborRight;
                    if (neighbor != null)
                        this.MoveSelection(neighbor);
                }
                if (this.Input.KeysPressedAny(Keys.Left, Keys.A))
                {
                    var neighbor = this.SelectedButton.NeighborLeft;
                    if (neighbor != null)
                        this.MoveSelection(neighbor);
                }
            }

            if (this.Input.KeysPressedAny(Keys.Right, Keys.D, Keys.Left, Keys.A, Keys.Enter))
            {
                if (this.SelectedButton == null)
                {
                    if (this.PreparationBar.IsActive)
                        this.SelectedButton = this.PreparationBar.Controls.OfType<Button>().First();
                    else if (this.EngagementBar.IsActive)
                        this.SelectedButton = this.EngagementBar.Controls.OfType<Button>().First();
                }

                if (!this.BarSelector.IsActive)
                {
                    this.BarSelector.Toggle();
                    this.BarSelector.Reveal();
                    this.BarLabelText = this.SelectedButton?.Name ?? "Choose >>";
                }
            }
        }

        void IDraw<ControlDraw>.Draw(SpriteBatch spriteBatch)
        {
            this.Background.Draw(spriteBatch);
            this.BarOfBars.Draw(spriteBatch);
        }

        void IDraw<PortraitDraw>.Draw(SpriteBatch spriteBatch)
        {
            this.PortraitBar.Draw(spriteBatch);
        }

        public void Activate()
        {
            this.IsActive = true;

            // TODO have a setting that decides whether to turn on non-mouse-based selecting on activate
            // e.g. EncounterStartsWithoutMouse

            // if (this.SelectedButton == null)
            //     this.SelectedButton = this.BottomFightButton;
            if (!this.BarSelector.IsActive)
                this.BarSelector.Toggle();
            this.BarSelector.Reveal();
            // this.BarSelector.Relocate(this.BottomFightButton.Destination);
            this.BarLabelText = this.SelectedButton?.Name ?? "Choose >>";
        }

        public void Deactivate()
        {
            this.IsActive = false;
        }

        #endregion

        #region Protected Methods

        protected void MoveSelection(Button newSelection)
        {
            this.SelectedButton = newSelection;
            this.BarLabelText = newSelection.Name;
            this.BarSelector.Relocate(newSelection.Destination);
            this.BarSelector.Reveal();
        }

        protected void HandleButtonEnter(Button button)
        {
            this.BarLabelText = button.Name;
            if (this.BarSelector.IsActive)
                this.BarSelector.Toggle();
        }

        protected void HandleButtonLeave(Button button)
        {
            if (this.BarLabelText == button.Name)
                this.BarLabelText = "Choose >>";
            if (this.BarSelector.IsActive)
                this.BarSelector.Toggle();
        }

        protected void HandleClick(Button button)
        {
            switch (button.Name)
            {
                case "Engage":
                    this.PreparationBar.Deactivate();
                    this.EngagementBar.Activate();
                    if (this.SelectedButton != null)
                        this.MoveSelection(this.EngagementBar.Controls.OfType<Button>().First());
                    break;

                case "Cancel":
                    this.EngagementBar.Deactivate();
                    this.PreparationBar.Activate();
                    if (this.SelectedButton != null)
                        this.MoveSelection(this.PreparationBar.Controls.OfType<Button>().First());
                    break;
            }
        }

        protected Panel<NormalUpdate, ControlDraw> GetCharacterPanel(Rectangle container, Point location)
        {
            var panel = new Panel<NormalUpdate, ControlDraw>(isActive: false);

            var width = container.Width / 4;
            var rectangle = new Rectangle(location, new Point(width, container.Height));
            panel.Append(new Patch(rectangle, this.ButtonTexture, 5, Color.SeaGreen));

            var chosenIndex = new Random().Next(this.PortraitTextures.Length);
            var portraitTexture = this.PortraitTextures[chosenIndex];

            var portraitRectangle = new Rectangle(rectangle.Location.X + (int) Math.Ceiling(rectangle.Width * (15/32f)),
                rectangle.Location.Y + (int) Math.Ceiling(rectangle.Height * (3/32f)),
                width / 2, width / 2);

            var portraitOutlineRectangle = new Rectangle(portraitRectangle.Location - new Point(2, 2),
                portraitRectangle.Size + new Point(4, 4));
            panel.Append(new Basic(portraitOutlineRectangle, this.ButtonTexture, Color.Gainsboro));

            panel.Append(new Basic(portraitRectangle, portraitTexture));

            var nameRectangle = portraitRectangle.Relocate(new Point(rectangle.Location.X + rectangle.Width / 8, portraitRectangle.Location.Y + rectangle.Height / 16));
            panel.Append(new Label(nameRectangle, this.Font, $"{chosenIndex+1:00}.png"));

            var healthRectangle = new Rectangle(rectangle.Location.X + rectangle.Width / 8,
                rectangle.Location.Y + rectangle.Height / 2,
                width / 4, rectangle.Height / 8);
            panel.Append(new Label(healthRectangle, this.Font, "40/40"));

            var manaRectangle = healthRectangle.Move(new Point(0, (int) Math.Ceiling(healthRectangle.Height * 4 / 3f)));
            panel.Append(new Label(manaRectangle, this.Font, "10/10"));

            return panel;
        }

        #endregion
    }
}