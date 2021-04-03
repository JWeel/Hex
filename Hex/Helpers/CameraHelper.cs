using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
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

        public CameraHelper(Func<Vector2> boundarySizeGetter, Func<Vector2> viewportSizeGetter, InputHelper input)
        {
            this.BoundarySizeGetter = boundarySizeGetter;
            this.ViewportSizeGetter = viewportSizeGetter;
            this.Input = input;
            this.Position = Vector2.Zero;
            this.ZoomScaleFactor = 1.0f;
        }

        #endregion

        #region Properties

        protected Func<Vector2> BoundarySizeGetter { get; }
        protected Func<Vector2> ViewportSizeGetter { get; }
        protected InputHelper Input { get; }

        protected bool IsMoving { get; set; }
        protected Vector2 LastMovePosition { get; set; }

        public Vector2 Position { get; protected set; }
        public float ZoomScaleFactor { get; protected set; }

        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-this.Position.X, -this.Position.Y, 0) *
            Matrix.CreateScale(this.ZoomScaleFactor, this.ZoomScaleFactor, 1) *
            Matrix.CreateTranslation(new Vector3(this.ViewportSizeGetter() / 2f, 0));

        #endregion

        #region Public Methods

        public Vector2 ToScreen(Vector2 worldPosition) =>
            worldPosition.Transform(this.TranslationMatrix);

        public Vector2 FromScreen(Vector2 screenPosition) =>
            screenPosition.Transform(this.TranslationMatrix.Invert());

        public void Update(GameTime gameTime)
        {
            this.HandleMouse();
            this.HandleKeys();
        }

        public void Center()
        {
            var (minimum, maximum) = this.GetPositionExtrema();
            this.Position = Vector2.Floor((minimum + maximum) / 2);
        }

        public void CenterOn(Vector2 position)
        {
            var (minimum, maximum) = this.GetPositionExtrema();
            var cameraCenter = (minimum + maximum) / 2f;
            this.Position = Vector2.Clamp(position, minimum, maximum);
        }

        #endregion

        #region Helper Methods

        protected (Vector2 Minimum, Vector2 Maximum) GetPositionExtrema()
        {
            // 'corners' are relative as camera position is the middle of viewport
            // example: viewport of 3 by 3 in boundary of 6 by 6
            // the actual corners are 0,0 and 5,5, but camera position corners are not
            // + - + - - +
            // | x |     |  -> if camera is in top left, position will be 1,1
            // + - +     |
            // |     + - +
            // |     | x |  -> if camera is in bottom right, position will be 4,4
            // + - - + - +
            var boundarySize = this.BoundarySizeGetter();
            var viewportSize = this.ViewportSizeGetter();
            var scaledViewportCenter = viewportSize / this.ZoomScaleFactor / 2f;
            var minimum = scaledViewportCenter;
            var maximum = boundarySize - scaledViewportCenter;
            return (minimum, maximum);
        }

        protected void HandleMouse()
        {
            var mousePosition = this.Input.CurrentVirtualMouseVector;
            if (this.IsMoving && this.ZoomScaleFactor >= 1f)
            {
                this.Move(-(mousePosition - this.LastMovePosition));
                this.LastMovePosition = mousePosition;
                this.IsMoving = !this.Input.MouseReleased(MouseButton.Right);
            }
            if (this.ViewportSizeGetter().ToRectangle().Contains(mousePosition))
            {
                if (this.Input.MouseScrolled())
                {
                    var currentZoom = this.ZoomScaleFactor;
                    var zoomAmount = ZOOM_SCALE_FACTOR_INCREMENT * (this.Input.MouseScrolledUp() ? 2 : -2);
                    this.Zoom(zoomAmount);

                    if (this.ZoomScaleFactor != currentZoom)
                    {
                        var zoomPosition = this.FromScreen(mousePosition);
                        // this zooms on a point defined on the line between previous zoom center and cursor (looks 'natural')
                        this.Position = zoomPosition + (currentZoom / this.ZoomScaleFactor) * (this.Position - zoomPosition);
                    }

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
                this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (this.Input.KeyDown(Keys.E))
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
            this.Move(Vector2.Zero);
        }

        protected void Move(Vector2 amount)
        {
            var (minimum, maximum) = this.GetPositionExtrema();
            this.Position = Vector2.Clamp(this.Position + amount, minimum, maximum);
        }

        #endregion
    }
}