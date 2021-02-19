using Hex.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Hex.Helpers
{
    public class CameraHelper
    {
        #region Constants

        private const float ZOOM_SCALE_FACTOR_INCREMENT = 1 / 16f;
        private const float POSITION_MOVE_INCREMENT = 5f;

        #endregion

        #region Constructors

        public CameraHelper(Func<int> mapWidthGetter, Func<int> mapHeightGetter, Func<int> viewportWidthGetter, Func<int> viewportHeightGetter)
        {
            this.MapWidthGetter = mapWidthGetter;
            this.MapHeightGetter = mapHeightGetter;
            this.ViewportWidthGetter = viewportWidthGetter;
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
        public bool IsMoving { get; protected set; }
        protected Vector2 LastMovePosition { get; set; }

        protected Func<int> MapWidthGetter { get; }
        protected Func<int> MapHeightGetter { get; }
        protected Func<int> ViewportWidthGetter { get; }
        protected Func<int> ViewportHeightGetter { get; }

        public Vector2 ViewportCenter =>
            new Vector2(this.MapWidthGetter() / 2f, this.MapHeightGetter() / 2f);

        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-(int) this.Position.X, -(int) this.Position.Y, 0) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateScale(this.ZoomScaleFactor, this.ZoomScaleFactor, 1) *
            Matrix.CreateTranslation(new Vector3(this.ViewportCenter, 0));

        public Rectangle ViewportWorldBoundry
        {
            get
            {
                var viewPortTopLeftCorner = this.ScreenToCamera(Vector2.Zero);
                var viewPortBottomRightCorner = this.ScreenToCamera(new Vector2(this.MapWidthGetter(), this.MapHeightGetter()));
                return new Rectangle((int) viewPortTopLeftCorner.X, (int) viewPortTopLeftCorner.Y,
                    (int) (viewPortBottomRightCorner.X - viewPortTopLeftCorner.X),
                    (int) (viewPortBottomRightCorner.Y - viewPortTopLeftCorner.Y));
            }
        }

        #endregion

        #region Methods

        public void Zoom(float amount) 
        {
            this.ZoomScaleFactor = this.ZoomScaleFactor.AddWithLimits(amount, .5f, 4f);
            this.Move(Vector2.Zero, clamp: true);
        }

        public void Move(Vector2 amount, bool clamp)
        {
            if (!clamp)
            {
                this.Position += amount;
                return;
            }
            this.Position = this.MapClampedPosition(this.Position + amount);
        }

        public Vector2 CameraToScreen(Vector2 worldPosition) =>
            Vector2.Transform(worldPosition, this.TranslationMatrix);

        public Vector2 ScreenToCamera(Vector2 screenPosition) =>
            Vector2.Transform(screenPosition, Matrix.Invert(this.TranslationMatrix));

        // this stuff all broken
        public void CenterOn(Vector2 position) =>
            this.Position = position;
        public void CenterOn(Hexagon hex) =>
            this.Position = this.CenteredPosition(hex, clamp: true);
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
            var mapWidth = this.MapWidthGetter();
            var mapHeight = this.MapHeightGetter();
            var viewportWidth = this.ViewportWidthGetter();
            var viewportHeight = this.ViewportHeightGetter();

            var viewportCorner = new Vector2(viewportWidth, viewportHeight) / this.ZoomScaleFactor / 2f;

            var offset = new Vector2(
                (mapWidth > viewportWidth) ? (mapWidth - viewportWidth) / 2f / this.ZoomScaleFactor : 0,
                (mapHeight > viewportHeight) ? (mapHeight - viewportHeight) / 2f / this.ZoomScaleFactor : 0);

            var cameraMin = viewportCorner + offset;
            var cameraMax = new Vector2(mapWidth, mapHeight) - viewportCorner + offset;
            
            return Vector2.Clamp(position, cameraMin, cameraMax);
        }

        public void HandleInput(InputHelper input)
        {
            // use event handler instead and add inputhelper to it

            if (!input.KeysDownAny(Keys.Q, Keys.E, Keys.A, Keys.D, Keys.W, Keys.S))
                return;

            if (input.KeyDown(Keys.Q))
                this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (input.KeyDown(Keys.E))
                this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);

            var cameraMovement = Vector2.Zero;
            if (input.KeyDown(Keys.A))
                cameraMovement.X = +1;
            if (input.KeyDown(Keys.D))
                cameraMovement.X = -1;
            if (input.KeyDown(Keys.W))
                cameraMovement.Y = +1;
            if (input.KeyDown(Keys.S))
                cameraMovement.Y = -1;

            // Normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();

            cameraMovement *= POSITION_MOVE_INCREMENT;
            cameraMovement *= this.ZoomScaleFactor;
            this.Move(-cameraMovement, clamp: true);
        }

        public void StartMouseMove(Vector2 startPosition)
        {
            this.IsMoving = true;
            this.LastMovePosition = startPosition;
        }

        public void MouseMove(Vector2 position)
        {
            this.Move(-(position - this.LastMovePosition), clamp: true);
            this.LastMovePosition = position;
        }

        public void StopMouseMove()
        {
            this.IsMoving = false;
        }

        #endregion
    }
}