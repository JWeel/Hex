using Hex.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hex.Helpers
{
    public class CameraHelper
    {
        #region Constructors

        public CameraHelper()
        {
            this.Scale = 1.0f;
            this.Rotation = 0.0f;
            this.Position = Vector2.Zero;
        }

        #endregion

        #region Properties

        public Vector2 Position { get; protected set; }
        public float Scale { get; protected set; }
        public float Rotation { get; protected set; }

        public int ViewportWidth { get; set; } // should be protected
        public int ViewportHeight { get; set; }

        public Vector2 ViewportCenter =>
            new Vector2(this.ViewportWidth / 2f, this.ViewportHeight / 2f);

        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-(int) this.Position.X, -(int) this.Position.Y, 0) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateScale(new Vector3(this.Scale, this.Scale, 1)) *
            Matrix.CreateTranslation(new Vector3(this.ViewportCenter, 0));

        public Rectangle ViewportWorldBoundry
        {
            get
            {
                var viewPortCorner = this.ScreenToWorld(new Vector2(0, 0));
                var viewPortBottomCorner = this.ScreenToWorld(new Vector2(this.ViewportWidth, this.ViewportHeight));
                return new Rectangle((int) viewPortCorner.X,
                    (int) viewPortCorner.Y,
                    (int) (viewPortBottomCorner.X - viewPortCorner.X),
                    (int) (viewPortBottomCorner.Y - viewPortCorner.Y));
            }
        }


        #endregion

        #region Methods

        public void Zoom(float amount)
        {
            this.Scale = this.Scale.AddWithLimits(amount, 1f, 4f);
        }

        public void Move(Vector2 amount, bool clamp = true)
        {
            if (!clamp)
            {
                this.Position += amount;
                return;
            }
            // var x = this.Position.X.AddWithLimits(amount.X, 0, 1000);
            // var y = this.Position.Y.AddWithLimits(amount.Y, 0, 1000);
            // this.Position = new Vector2(x, y);
            this.Position = this.MapClampedPosition(this.Position + amount);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition) =>
            Vector2.Transform(screenPosition, Matrix.Invert(this.TranslationMatrix));

        public void CenterOn(Vector2 position) =>
            this.Position = position;

        public void CenterOn(Hexagon hex) =>
            this.Position = this.CenteredPosition(hex, true);

        protected Vector2 CenteredPosition(Hexagon hex, bool clamp = false)
        {
            var cameraPosition = new Vector2(hex.X * 25, hex.Y * 29);
            var cameraCenteredOnTilePosition = new Vector2(cameraPosition.X + 25 / 2, cameraPosition.Y + 29 / 2);
            if (clamp)
                return this.MapClampedPosition(cameraCenteredOnTilePosition);
            return cameraCenteredOnTilePosition;
        }

        protected Vector2 MapClampedPosition(Vector2 position)
        {
            var cameraMax = new Vector2(
                1280 - (this.ViewportWidth / this.Scale / 2),
                720 - (this.ViewportHeight / this.Scale / 2));
            return Vector2.Clamp(position,
                new Vector2(this.ViewportWidth / this.Scale / 2, this.ViewportHeight / this.Scale / 2),
                cameraMax);
        }

        public void HandleInput(InputHelper input)
        {
            Vector2 cameraMovement = Vector2.Zero;
            if (input.KeyPressed(Keys.A))
            {
                cameraMovement.X = +1;
            }
            else if (input.KeyPressed(Keys.D))
            {
                cameraMovement.X = -1;
            }
            if (input.KeyPressed(Keys.W))
            {
                cameraMovement.Y = +1;
            }
            else if (input.KeyPressed(Keys.S))
            {
                cameraMovement.Y = -1;
            }
            if (input.KeyPressed(Keys.Q))
            {
                this.Zoom(-0.25f);
            }
            else if (input.KeyPressed(Keys.E))
            {
                this.Zoom(+0.25f);
            }

            // Normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();

            // Multiply by defined increment
            cameraMovement *= 25f;
            this.Move(cameraMovement, clamp: true);
        }

        #endregion
    }
}