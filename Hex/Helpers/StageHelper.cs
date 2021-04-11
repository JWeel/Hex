using System;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class StageHelper : IRegister, IUpdate<NormalUpdate>, IDraw<BackgroundDraw>
    {
        #region Constructors

        public StageHelper(InputHelper input, Texture2D blankTexture)
        {
            this.Input = input;
            this.Camera = new CameraHelper(() => this.StageSize, () => this.Container, input);
            this.BlankTexture = blankTexture;
        }

        #endregion

        #region Data Members

        /// <summary> The rectangle of the widget, control, or component that contains this stage. </summary>
        public Rectangle Container { get; protected set; }

        /// <summary> The size of the widget, control, or component that contains this stage. </summary>
        public Vector2 ContainerSize => this.Container.Size.ToVector2();

        /// <summary> The size required to fully contain the tilemap in any rotation. </summary>
        public Vector2 TilemapBoundingBoxSize { get; protected set; }

        /// <summary> The unbound size of the stage. This is the max of <see cref="TilemapBoundingBoxSize"/> and <see cref="ContainerSize"/>. </summary>
        public Vector2 StageSize { get; protected set; }

        public Hexagon CursorTile => this.Tilemap.CursorTile;
        public Hexagon SourceTile => this.Tilemap.SourceTile;

        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.Map.Count;

        protected InputHelper Input { get; }
        protected CameraHelper Camera { get; }
        protected Texture2D BlankTexture { get; }
        protected TilemapHelper Tilemap { get; set; }
        protected ActorHelper Actor { get; set; }

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            dependency.Register(this.Camera);
            this.Tilemap = dependency.Register<TilemapHelper>();
            this.Tilemap.OnRotate += this.CenterOnSourceTile;
            this.Actor = dependency.Register<ActorHelper>();
        }

        public void Arrange(Rectangle container, string placeholder)
        {
            this.Container = container;
            this.Tilemap.Arrange();

            // boundingbox should be all 4 corners of the bounding rectangle (the diagonal of tilemap size)
            // plus padding for when that corner is the center of rotation (half of containersize on each side)
            // below formula gives slightly more than necessary (might be tilesize?), but will do for now
            this.TilemapBoundingBoxSize = new Vector2(this.Tilemap.TilemapSize.Length()) + this.ContainerSize;

            // the real size of the stage is the max of the tilemap bounding box and the containing rectangle
            this.StageSize = Vector2.Max(this.TilemapBoundingBoxSize, this.ContainerSize);

            this.Tilemap.CalculateOffset(center: this.StageSize / 2);
            this.Camera.Center();
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();
            if (this.Input.KeyPressed(Keys.H))
                this.CenterOnSourceTile();
            if (this.Input.MouseMoved())
            {
                var virtualMouseVector = this.Input.CurrentVirtualMouseVector;
                var cameraTranslatedMouseVector = this.Camera.FromScreen(virtualMouseVector);
                if (this.Container.Contains(virtualMouseVector))
                    this.Tilemap.TrackTiles(cameraTranslatedMouseVector);
                // clear after leaving container
                else if (this.CursorTile != default)
                    this.Tilemap.UntrackTiles();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawTo(this.BlankTexture, this.Camera.CameraBox, new Color(20, 60, 90), depth: .05f);
        }

        #endregion

        #region Helper Methods

        protected void CenterOnSourceTile()
        {
            // TODO setting to toggle this behavior on/off
            if (this.SourceTile != null)
                this.Camera.CenterOn(this.Tilemap.SourceTileMiddle);
        }

        #endregion
    }
}