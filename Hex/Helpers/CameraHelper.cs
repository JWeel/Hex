using Hex.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Hex.Helpers
{
    public class CameraHelper
    {
        #region Constants

        private const float ZOOM_SCALE_FACTOR_INCREMENT = 1/16f;
        private const float POSITION_MOVE_INCREMENT = 100f;

        #endregion

        #region Constructors

        public CameraHelper(Func<int> viewportWidthGetter, Func<int> viewportHeightGetter)
        {
            // possibly needs to be relative to map size
            this.ViewportWidthGetter = viewportWidthGetter;
            // possibly needs to be relative to map size
            this.ViewportHeightGetter = viewportHeightGetter;
            this.Position = Vector2.Zero;
            this.ZoomScaleFactor = 1.0f;
            this.Rotation = 0.0f;
        }

        #endregion

        #region Properties

        public Vector2 Position { get; protected set; }
        public float ZoomScaleFactor { get; protected set; }
        public float Rotation { get; protected set; }

        protected Func<int> ViewportWidthGetter { get; }
        protected Func<int> ViewportHeightGetter { get; }

        public Vector2 ViewportCenter =>
            new Vector2(this.ViewportWidthGetter() / 2f, this.ViewportHeightGetter() / 2f);

        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-(int) this.Position.X, -(int) this.Position.Y, 0) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateScale(new Vector3(this.ZoomScaleFactor, this.ZoomScaleFactor, 1)) *
            Matrix.CreateTranslation(new Vector3(this.ViewportCenter, 0));

        #endregion

        #region Methods

        public void Zoom(float amount) =>
            this.ZoomScaleFactor = this.ZoomScaleFactor.AddWithLimits(amount, 1f, 4f);

        public void Move(Vector2 amount, bool clamp = true)
        {
            if (!clamp)
            {
                this.Position += amount;
                return;
            }
            this.Position = this.MapClampedPosition(this.Position + amount);
        }

        public Vector2 WorldToScreen(Vector2 worldPosition) =>
            Vector2.Transform(worldPosition, this.TranslationMatrix);

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
                Core.BASE_MAP_WIDTH - (this.ViewportWidthGetter() / this.ZoomScaleFactor / 2),
                Core.BASE_MAP_HEIGHT - (this.ViewportHeightGetter() / this.ZoomScaleFactor / 2));
            return Vector2.Clamp(position,
                new Vector2(this.ViewportWidthGetter() / this.ZoomScaleFactor / 2, this.ViewportHeightGetter() / this.ZoomScaleFactor / 2),
                cameraMax);
        }

        public void HandleInput(InputHelper input)
        {
            if (input.KeyDown(Keys.Q))
                this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (input.KeyDown(Keys.E))
                this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);

            Vector2 cameraMovement = Vector2.Zero;
            if (input.KeyDown(Keys.A))
                cameraMovement.X = +POSITION_MOVE_INCREMENT;
            if (input.KeyDown(Keys.D))
                cameraMovement.X = -POSITION_MOVE_INCREMENT;
            if (input.KeyDown(Keys.W))
                cameraMovement.Y = +POSITION_MOVE_INCREMENT;
            if (input.KeyDown(Keys.S))
                cameraMovement.Y = -POSITION_MOVE_INCREMENT;

            // Normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();
            cameraMovement *= this.ZoomScaleFactor;
            this.Move(cameraMovement, clamp: true);
        }

        #endregion
    }
}