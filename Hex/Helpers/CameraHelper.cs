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

        public CameraHelper(InputHelper input)
        {
            this.Input = input;
            this.ZoomFactor = 1.0f;
        }

        #endregion

        #region Properties

        protected InputHelper Input { get; }

        /// <summary> The area in which the camera moves. </summary>
        /// <remarks> This is represented by a vector instead of a rectangle, because it specifies the coordinates of the corner opposite the origin: a rectangle can formed by taking origin plus this vector. </remarks>
        protected Vector2 Plane { get; set; }
        
        /// <summary> The area of the plane shown by the camera. </summary>
        protected Rectangle Viewport { get; set; }

        /// <summary> The position of the camera within the plane. </summary>
        protected Vector2 Position { get; set; }

        /// <summary> A scale factor that represents camera zoom. </summary>
        protected float ZoomFactor { get; set; }

        /// <summary> A flag to keep track of movement.  </summary>
        protected bool IsMoving { get; set; }

        /// <summary> The coordinates of the middle of the viewport. </summary>
        protected Vector2 ViewportCenter { get; set; }

        /// <summary> A transform matrix that scales and moves to relative camera position. </summary>
        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-this.Position.X, -this.Position.Y, 0) *
            Matrix.CreateScale(this.ZoomFactor, this.ZoomFactor, 1) *
            Matrix.CreateTranslation(this.ViewportCenter.X, this.ViewportCenter.Y, 0);

        /// <summary> A rectangle which spans the area shown by the current camera translation matrix. </summary>
        public Rectangle CameraBox
        {
            get
            {
                // there is a small rounding error so add offset
                var roundingOffset = new Vector2(2);
                var viewportSize = this.Viewport.Size.ToVector2();
                var cameraCorner = this.Position - viewportSize / 2 / this.ZoomFactor - roundingOffset;
                var cameraBoxSize = viewportSize / this.ZoomFactor + roundingOffset * 2;
                return new Rectangle(cameraCorner.ToPoint(), cameraBoxSize.ToPoint());
            }
        }

        #endregion

        #region Public Methods

        public Vector2 ToScreen(Vector2 worldPosition) =>
            worldPosition.Transform(this.TranslationMatrix);

        public Vector2 FromScreen(Vector2 screenPosition) =>
            screenPosition.Transform(this.TranslationMatrix.Invert());

        public void Arrange(Vector2 plane, Rectangle viewport)
        {
            this.Plane = plane;
            this.Viewport = viewport;
            this.ViewportCenter = Vector2.Round(this.Viewport.Size.ToVector2() / 2f + this.Viewport.Location.ToVector2());
            this.Center();
        }

        public void Update(GameTime gameTime)
        {
            this.HandleMouse();
            this.HandleKeys();
        }

        public void Center()
        {
            var (minimum, maximum) = this.GetPositionExtrema();
            this.Position = Vector2.Round((minimum + maximum) / 2);
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
            // 'corners' are relative as camera position is the center of the camera viewport
            // example: viewport of 3 by 3 in boundary of 6 by 6
            // the actual corners are 0,0 and 5,5, but camera position corners are not
            // + - + - - +
            // | x |     |  -> if camera is in top left, position will be 1,1
            // + - +     |
            // |     + - +
            // |     | x |  -> if camera is in bottom right, position will be 4,4
            // + - - + - +
            var bottomRightCorner = this.Plane;
            var viewportSize = this.Viewport.Size.ToVector2();
            var scaledViewportCenter = viewportSize / this.ZoomFactor / 2f;
            var minimum = scaledViewportCenter;
            var maximum = bottomRightCorner - scaledViewportCenter;
            return (minimum, maximum);
        }

        protected void HandleMouse()
        {
            var mousePosition = this.Input.CurrentVirtualMouseVector;
            if (this.IsMoving)
            {
                this.Move(-(mousePosition - this.Input.PreviousVirtualMouseVector));
                this.IsMoving = !this.Input.MouseReleased(MouseButton.Right);
            }
            if (this.Viewport.Contains(mousePosition))
            {
                if (this.Input.MouseScrolled())
                {
                    var currentZoom = this.ZoomFactor;
                    var zoomAmount = ZOOM_SCALE_FACTOR_INCREMENT * (this.Input.MouseScrolledUp() ? 2 : -2);
                    this.Zoom(zoomAmount);

                    if (this.ZoomFactor != currentZoom)
                    {
                        var zoomPosition = this.FromScreen(mousePosition);
                        // to make zooming look natural: set position relatively between previous zoom center and cursor
                        this.Position = zoomPosition + (currentZoom / this.ZoomFactor) * (this.Position - zoomPosition);
                    }

                    if (this.ZoomFactor < 1f)
                        this.Center();
                }
                if (!this.IsMoving && this.Input.MousePressed(MouseButton.Right))
                {
                    this.IsMoving = true;
                }
            }
        }

        protected void HandleKeys()
        {
            if (!this.Input.KeysDownAny(Keys.Q, Keys.E, Keys.A, Keys.D, Keys.W, Keys.S, Keys.B))
                return;

            if (this.Input.KeyDown(Keys.Q))
                this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (this.Input.KeyDown(Keys.E))
                this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);
            if (this.Input.KeyDown(Keys.B))
                this.ZoomFactor = 1f;

            var cameraMovement = Vector2.Zero;
            if (this.Input.KeyDown(Keys.A))
                cameraMovement.X = -1;
            if (this.Input.KeyDown(Keys.D))
                cameraMovement.X = +1;
            if (this.Input.KeyDown(Keys.W))
                cameraMovement.Y = -1;
            if (this.Input.KeyDown(Keys.S))
                cameraMovement.Y = +1;

            // normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();

            cameraMovement *= POSITION_MOVE_INCREMENT;
            cameraMovement *= this.ZoomFactor;
            this.Move(-cameraMovement);
        }

        protected void Zoom(float amount, float minAmount = ZOOM_SCALE_MINIMUM, float maxAmount = ZOOM_SCALE_MAXIMUM)
        {
            this.ZoomFactor = Math.Clamp(this.ZoomFactor + amount, minAmount, maxAmount);
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