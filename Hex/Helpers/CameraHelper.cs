using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Framework;
using Mogi.Helpers;
using Mogi.Inversion;
using System;

namespace Hex.Helpers
{
    public class CameraHelper : IUpdate<NormalUpdate>
    {
        #region Constants

        private const float ZOOM_SCALE_FACTOR_INCREMENT = 1 / 16f;
        private const float ZOOM_SCALE_MAXIMUM = 4f;
        private const float ZOOM_SCALE_MINIMUM = 1f;
        private const float POSITION_MOVE_INCREMENT = 5f;

        #endregion

        #region Constructors

        public CameraHelper(Func<Vector2> mapSizeGetter, Func<Vector2> viewportSizeGetter, Func<float> rotationGetter, InputHelper input, ClientWindow window)
        {
            this.MapSizeGetter = mapSizeGetter;
            this.ViewportSizeGetter = viewportSizeGetter;
            this.Input = input;
            this.Window = window;
            this.Position = Vector2.Zero;
            this.ZoomScaleFactor = 1.0f;
        }

        #endregion

        #region Properties

        protected Func<Vector2> MapSizeGetter { get; }
        protected Func<Vector2> ViewportSizeGetter { get; }
        protected InputHelper Input { get; }
        protected ClientWindow Window { get; }

        protected bool IsMoving { get; set; }
        protected Vector2 LastMovePosition { get; set; }

        public Vector2 Position { get; protected set; }
        public float ZoomScaleFactor { get; protected set; }

        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-this.Position.X, -this.Position.Y, 0) *
            Matrix.CreateScale(this.ZoomScaleFactor, this.ZoomScaleFactor, 1) *
            Matrix.CreateTranslation(new Vector3(this.MapSizeGetter() / 2f, 0));

        // not sure why this works, but without this when tilemapsize > viewport camera goes out of bounds top left
        public Vector2 MagicOffset
        {
            get
            {
                var mapSize = this.MapSizeGetter();
                var viewportSize = this.ViewportSizeGetter();
                return new Vector2(
                    (mapSize.X > viewportSize.X) ? (mapSize.X - viewportSize.X) / 2f / this.ZoomScaleFactor : 0,
                    (mapSize.Y > viewportSize.Y) ? (mapSize.Y - viewportSize.Y) / 2f / this.ZoomScaleFactor : 0);
            }
        }

        #endregion

        #region Public Methods

        public Vector2 ToScreen(Vector2 worldPosition) =>
            Vector2.Transform(worldPosition, this.TranslationMatrix);

        public Vector2 FromScreen(Vector2 screenPosition) =>
            Vector2.Transform(screenPosition, Matrix.Invert(this.TranslationMatrix));

        public void Update(GameTime gameTime)
        {
            this.HandleMouse();
            this.HandleKeys();
        }

        public void Center()
        {
            var (cameraMin, cameraMax) = this.GetBounds();
            this.Position = Vector2.Floor((cameraMin + cameraMax) / 2);
        }

        public void CenterOn(Vector2 position)
        {
            var (cameraMin, cameraMax) = this.GetBounds();
            var cameraCenter = (cameraMin + cameraMax) / 2f;

            this.Position = Vector2.Clamp(position, cameraMin, cameraMax);
        }

        public bool RequiresClamping(Vector2 cameraPosition)
        {
            var (cameraMin, cameraMax) = this.GetBounds();
            return ((cameraPosition.X < cameraMin.X) || (cameraPosition.Y < cameraMin.Y) ||
                    (cameraPosition.X > cameraMax.X) || (cameraPosition.Y > cameraMax.Y));
        }

        public void Clamp()
        {
            var (cameraMin, cameraMax) = this.GetBounds();
            this.Position = Vector2.Clamp(this.Position, cameraMin, cameraMax);
        }

        #endregion

        #region Helper Methods

        protected (Vector2 CameraMin, Vector2 CameraMax) GetBounds()
        {
            var mapSize = this.MapSizeGetter();
            var viewportSize = this.ViewportSizeGetter();
            var scaledViewportCenter = viewportSize / this.ZoomScaleFactor / 2f;

            var cameraMin = scaledViewportCenter + this.MagicOffset;
            var cameraMax = mapSize - scaledViewportCenter + this.MagicOffset;

            return (cameraMin, cameraMax);
        }

        protected void HandleMouse()
        {
            var mousePosition = this.Input.CurrentMouseVector;
            if (this.IsMoving && this.ZoomScaleFactor >= 1f)
            {
                this.Move(-(mousePosition - this.LastMovePosition));
                this.LastMovePosition = mousePosition;
                this.IsMoving = !this.Input.MouseReleased(MouseButton.Right);
            }
            if (this.ViewportSizeGetter().ToRectangle().Contains(this.Window.Translate(this.Input.CurrentMouseVector)))
            {
                if (this.Input.MouseScrolled())
                {
                    var zoomAmount = ZOOM_SCALE_FACTOR_INCREMENT * (this.Input.MouseScrolledUp() ? 2 : -2);
                    this.Zoom(zoomAmount);

                    if (this.ZoomScaleFactor < 1f)
                        this.Center();
                }
                if (!this.IsMoving && this.Input.MousePressed(MouseButton.Right))
                {
                    this.IsMoving = true;
                    this.LastMovePosition = mousePosition;
                }
            }
        }

        protected void HandleKeys()
        {
            if (!this.Input.KeysDownAny(Keys.Q, Keys.E, Keys.A, Keys.D, Keys.W, Keys.S))
                return;

            if (this.Input.KeyDown(Keys.Q))
                if (this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
                else
                    this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (this.Input.KeyDown(Keys.E))
                if (this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);
                else
                    this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);

            if (this.ZoomScaleFactor < 1f)
            {
                this.Center();
                return;
            }

            var cameraMovement = Vector2.Zero;
            if (this.Input.KeyDown(Keys.A))
                cameraMovement.X = +1;
            if (this.Input.KeyDown(Keys.D))
                cameraMovement.X = -1;
            if (this.Input.KeyDown(Keys.W))
                cameraMovement.Y = +1;
            if (this.Input.KeyDown(Keys.S))
                cameraMovement.Y = -1;

            // normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();

            cameraMovement *= POSITION_MOVE_INCREMENT;
            cameraMovement *= this.ZoomScaleFactor;
            this.Move(-cameraMovement);
        }

        protected void Zoom(float amount, float minAmount = ZOOM_SCALE_MINIMUM, float maxAmount = ZOOM_SCALE_MAXIMUM)
        {
            this.ZoomScaleFactor = Math.Clamp(this.ZoomScaleFactor + amount, minAmount, maxAmount);
            // TODO: preserve camera center after zooming
            this.Move(Vector2.Zero);
        }

        protected void Move(Vector2 amount)
        {
            var (cameraMin, cameraMax) = this.GetBounds();
            this.Position = Vector2.Clamp(this.Position + amount, cameraMin, cameraMax);
        }

        #endregion
    }
}