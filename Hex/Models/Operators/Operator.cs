using Extended.Patterns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Models.Operators
{
    public class Operator : IUpdate, IDraw
    {
        #region Constants

        private const int DIRECTION_POINTER_DOWN = 0;
        private const int DIRECTION_POINTER_LEFT = 1;
        private const int DIRECTION_POINTER_RIGHT = 2;
        private const int DIRECTION_POINTER_UP = 3;

        #endregion

        #region Constructors

        public Operator(InputHelper input, ContentManager content)
        {
            this.Input = input;

            this.SpritesheetBody = content.Load<Texture2D>("graphics/operation/spritesheetBody1");
            this.SpritesheetHair = content.Load<Texture2D>("graphics/operation/spritesheetHair1");
            this.ShadowTexture = content.Load<Texture2D>("graphics/operation/shadow1");

            this.TextureSize = 32;
            this.StandingPointer = Cyclic.FromValues(1);
            this.WalkingPointer = Cyclic.FromValues(0, 1, 2, 1);

            this.AnimationPointer = this.StandingPointer;

            this.WalkingSpeed = 1.5f;
            this.HairColor = Color.Orange;
        }

        #endregion

        #region Properties

        public Vector2 Position { get; protected set; }

        protected InputHelper Input { get; }
        protected Texture2D SpritesheetBody { get; }
        protected Texture2D SpritesheetHair { get; }
        protected Texture2D ShadowTexture { get; }
        protected int TextureSize { get; }

        protected Cyclic<int> StandingPointer { get; }
        protected Cyclic<int> WalkingPointer { get; }

        protected Cyclic<int> AnimationPointer { get; set; }
        protected int DirectionPointer { get; set; }

        protected Color HairColor { get; set; }
        protected float WalkingSpeed { get; set; }

        public bool IsWalking { get; protected set; }
        protected bool IsRolling { get; set; }
        protected bool IsJumping { get; set; }
        protected bool IsSlashing { get; set; }
        protected bool IsShooting { get; set; }

        protected double LastTimestamp { get; set; }
        protected bool ForceNewTimestamp { get; set; }

        // could also update it in Position setter or on the outside when setting position
        protected Vector2 DrawPosition => this.Position - new Vector2(this.TextureSize / 2);

        // all sprites are expected to be in a single spritesheet, each animation in a row with unique frames by column
        protected Rectangle SourceRegion =>
            new Rectangle(this.AnimationPointer * this.TextureSize, this.DirectionPointer * this.TextureSize,
                this.TextureSize, this.TextureSize);

        #endregion

        #region Methods

        public void StartWalking()
        {
            this.IsWalking = true;
            this.AnimationPointer = this.WalkingPointer;
            this.AnimationPointer.Restart();
            this.ForceNewTimestamp = true;
        }

        public void StopWalking()
        {
            this.IsWalking = false;
            this.AnimationPointer = this.StandingPointer;
            this.AnimationPointer.Restart();
            this.ForceNewTimestamp = true;
        }

        public void MoveTo(Vector2 position)
        {
            this.Position = position;
        }

        public void Move(Vector2 movement)
        {
            if (movement.Y < 0)
                this.DirectionPointer = DIRECTION_POINTER_UP;
            else if (movement.Y > 0)
                this.DirectionPointer = DIRECTION_POINTER_DOWN;
            if (movement.X < 0)
                this.DirectionPointer = DIRECTION_POINTER_LEFT;
            else if (movement.X > 0)
                this.DirectionPointer = DIRECTION_POINTER_RIGHT;

            var multiplier = this.IsWalking ? this.WalkingSpeed : 0;
            this.Position += movement * multiplier;
        }

        public void Update(GameTime gameTime)
        {
            if (this.ForceNewTimestamp)
            {
                this.LastTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
                this.ForceNewTimestamp = false;
            }
            var delta = (gameTime.TotalGameTime.TotalMilliseconds - this.LastTimestamp);
            if (delta > 200) // TODO should be configurable per animation
            {
                this.LastTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
                this.AnimationPointer.Advance();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawAt(this.ShadowTexture,
                this.Position + new Vector2(this.ShadowTexture.Width / -4, this.ShadowTexture.Height), depth: .15f);

            spriteBatch.DrawAt(this.SpritesheetBody, this.DrawPosition, depth: 0.5f,
                sourceRectangle: this.SourceRegion);

            spriteBatch.DrawAt(this.SpritesheetHair, this.DrawPosition, this.HairColor, depth: 0.6f,
                sourceRectangle: this.SourceRegion);
        }

        #endregion
    }
}