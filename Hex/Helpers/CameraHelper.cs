using Hex.Enums;
using Hex.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Hex.Helpers
{
    public class CameraHelper
    {
        #region Constants

        private const float ZOOM_SCALE_FACTOR_INCREMENT = 1 / 16f;
        private const float ZOOM_SCALE_MAXIMUM = 4f;
        private const float ZOOM_SCALE_MINIMUM = 1f;
        private const float ZOOM_SCALE_MINIMUM_EXTREME = 1 / 4f;
        private const float POSITION_MOVE_INCREMENT = 5f;

        #endregion

        #region Constructors

        public CameraHelper(Func<Vector2> mapSizeGetter, Func<Vector2> viewportSizeGetter, Func<Rectangle> panelGetter)
        {
            this.MapSizeGetter = mapSizeGetter;
            this.ViewportSizeGetter = viewportSizeGetter;
            this.PanelGetter = panelGetter;
            this.Position = Vector2.Zero;
            this.ZoomScaleFactor = 1.0f;
            this.Rotation = 0.0f;
        }

        #endregion

        #region Properties

        protected Func<Vector2> MapSizeGetter { get; }
        protected Func<Vector2> ViewportSizeGetter { get; }
        protected Func<Rectangle> PanelGetter { get; }

        protected bool IsMoving { get; set; }
        protected Vector2 LastMovePosition { get; set; }

        public Vector2 Position { get; protected set; }
        public float ZoomScaleFactor { get; protected set; }
        protected float Rotation { get; set; }

        // TODO come up with way to cache this and only recalculate when needed, e.g. use the above setters
        public Matrix TranslationMatrix =>
            Matrix.CreateTranslation(-this.Position.X, -this.Position.Y, 0) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateScale(this.ZoomScaleFactor, this.ZoomScaleFactor, 1) *
            Matrix.CreateTranslation(new Vector3(this.MapSizeGetter() / 2f, 0));

        #endregion

        #region Public Methods

        public Vector2 ToScreen(Vector2 worldPosition) =>
            Vector2.Transform(worldPosition, this.TranslationMatrix);

        public Vector2 FromScreen(Vector2 screenPosition) =>
            Vector2.Transform(screenPosition, Matrix.Invert(this.TranslationMatrix));

        public void HandleInput(InputHelper input)
        {
            // TODO use event handler instead and add inputhelper to it
            this.HandleMouse(input);
            this.HandleKeys(input);
        }

        public void Center()
        {
            var mapSize = this.MapSizeGetter();
            var viewportSize = this.ViewportSizeGetter();
            var viewportCorner = viewportSize / this.ZoomScaleFactor / 2f;
            var offset = new Vector2(
                (mapSize.X > viewportSize.X) ? (mapSize.X - viewportSize.X) / 2f / this.ZoomScaleFactor : 0,
                (mapSize.Y > viewportSize.Y) ? (mapSize.Y - viewportSize.Y) / 2f / this.ZoomScaleFactor : 0);
            var cameraMin = viewportCorner + offset;
            var cameraMax = mapSize - viewportCorner + offset;
            this.Position = Vector2.Floor((cameraMax + cameraMin) / 2);
        }

        // TODO: fix these methods, also it shouldnt know anything about hexagon class
        public void CenterOn(Vector2 position) =>
            this.Position = position;
        // public void CenterOn(Hexagon hex) =>
        //     this.Position = this.CenterOn(hex, clamp: true);

        #endregion

        #region Helper Methods

        protected void HandleMouse(InputHelper input)
        {
            var mousePosition = input.CurrentMouseState.ToVector2();
            if (this.IsMoving && this.ZoomScaleFactor >= 1f)
            {
                this.Move(-(mousePosition - this.LastMovePosition), clamp: true);
                this.LastMovePosition = mousePosition;
                this.IsMoving = !input.MouseReleased(MouseButton.Right);
            }
            if (this.PanelGetter().Contains(input.CurrentMouseState))
            {
                if (input.MouseScrolled())
                {
                    var zoomAmount = ZOOM_SCALE_FACTOR_INCREMENT * (input.MouseScrolledUp() ? 2 : -2);
                    // TODO add zoomOrigin to zoom
                    if (input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                        this.Zoom(zoomAmount, minAmount: ZOOM_SCALE_MINIMUM_EXTREME);
                    else
                        this.Zoom(zoomAmount);
                        
                    if (this.ZoomScaleFactor < 1f)
                        this.Center();
                }
                if (!this.IsMoving && input.MousePressed(MouseButton.Right))
                {
                    this.IsMoving = true;
                    this.LastMovePosition = mousePosition;
                }
            }
        }

        protected void HandleKeys(InputHelper input)
        {
            if (!input.KeysDownAny(Keys.Q, Keys.E, Keys.A, Keys.D, Keys.W, Keys.S))
                return;

            if (input.KeyDown(Keys.Q))
                if (input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT, minAmount: ZOOM_SCALE_MINIMUM_EXTREME);
                else
                    this.Zoom(-ZOOM_SCALE_FACTOR_INCREMENT);
            if (input.KeyDown(Keys.E))
                if (input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT, minAmount: ZOOM_SCALE_MINIMUM_EXTREME);
                else
                    this.Zoom(+ZOOM_SCALE_FACTOR_INCREMENT);

            if (this.ZoomScaleFactor < 1f)
            {
                this.Center();
                return;
            }

            var cameraMovement = Vector2.Zero;
            if (input.KeyDown(Keys.A))
                cameraMovement.X = +1;
            if (input.KeyDown(Keys.D))
                cameraMovement.X = -1;
            if (input.KeyDown(Keys.W))
                cameraMovement.Y = +1;
            if (input.KeyDown(Keys.S))
                cameraMovement.Y = -1;

            // normalizing is needed when changing two directions at once
            if (cameraMovement != Vector2.Zero)
                cameraMovement.Normalize();

            cameraMovement *= POSITION_MOVE_INCREMENT;
            cameraMovement *= this.ZoomScaleFactor;
            this.Move(-cameraMovement, clamp: true);
        }

        protected void Zoom(float amount, float minAmount = ZOOM_SCALE_MINIMUM, float maxAmount = ZOOM_SCALE_MAXIMUM)
        {
            this.ZoomScaleFactor = this.ZoomScaleFactor.AddWithLimits(amount, minAmount, maxAmount);
            this.Move(Vector2.Zero, clamp: true);
        }

        protected void Move(Vector2 amount, bool clamp)
        {
            if (!clamp)
            {
                this.Position += amount;
                return;
            }
            this.Position = this.MapClampedPosition(this.Position + amount);
        }

        protected Vector2 MapClampedPosition(Vector2 position)
        {
            var mapSize = this.MapSizeGetter();
            var viewportSize = this.ViewportSizeGetter();
            var viewportCorner = viewportSize / this.ZoomScaleFactor / 2f;
            var offset = new Vector2(
                (mapSize.X > viewportSize.X) ? (mapSize.X - viewportSize.X) / 2f / this.ZoomScaleFactor : 0,
                (mapSize.Y > viewportSize.Y) ? (mapSize.Y - viewportSize.Y) / 2f / this.ZoomScaleFactor : 0);
            var cameraMin = viewportCorner + offset;
            var cameraMax = mapSize - viewportCorner + offset;
            return Vector2.Clamp(position, Vector2.Floor(cameraMin), Vector2.Floor(cameraMax));
        }

        // TODO: fix this, also it shouldnt know anything about hexagon class
        // protected Vector2 CenterOn(Hexagon hex, bool clamp = false)
        // {
        //     var cameraPosition = new Vector2(hex.Q * 25, hex.R * 29);
        //     var cameraCenteredOnTilePosition = new Vector2(cameraPosition.X + 25 / 2, cameraPosition.Y + 29 / 2);
        //     if (clamp)
        //         return this.MapClampedPosition(cameraCenteredOnTilePosition);
        //     return cameraCenteredOnTilePosition;
        // }

        #endregion
    }
}