using System.ComponentModel;
using System.Linq;
using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Extensions;
using Hex.Models.Actors;
using Hex.Models.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class StageHelper : IRegister, IUpdate<NormalUpdate>, IDraw<BackgroundDraw>, IDraw<ForegroundDraw>
    {
        #region Constructors

        public StageHelper(InputHelper input, Texture2D blankTexture, ContentManager content)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;

            this.BackgroundTexture = content.Load<Texture2D>("graphics/background");
        }

        #endregion

        #region Data Members

        /// <summary> The rectangle of the widget, control, or component that contains this stage. </summary>
        public Rectangle Container { get; protected set; }

        /// <summary> The size of the widget, control, or component that contains this stage. </summary>
        public Vector2 ContainerSize => this.Container.Size.ToVector2();

        /// <summary> The bounding box which fully contains the tilemap in any rotation. </summary>
        /// <remarks> This is represented by a vector instead of a rectangle, because it specifies the coordinates of the corner opposite the origin: a rectangle can formed by taking origin plus this vector. </remarks>
        public Vector2 TilemapBoundingBox { get; protected set; }

        /// <summary> The unbound size of the stage. This is the max of <see cref="TilemapBoundingBox"/> and <see cref="ContainerSize"/>. </summary>
        public Vector2 StageSize { get; protected set; }

        public Hexagon CursorTile => this.Tilemap.CursorTile;
        public Hexagon SourceTile => this.Tilemap.SourceTile;

        public Actor SourceActor { get; protected set; }

        /// <summary> A transform matrix that scales and moves the stage relative to camera position. </summary>
        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.Map.Count;
        public int TilemapRotationInterval => this.Tilemap.WraparoundRotationInterval;

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D BackgroundTexture { get; }

        protected CameraHelper Camera { get; set; }
        protected TilemapHelper Tilemap { get; set; }
        protected ActorHelper Actor { get; set; }

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            using (new DependencyScope(dependency))
            {
                this.Camera = dependency.Register<CameraHelper>();
                this.Tilemap = dependency.Register<TilemapHelper>();
                this.Tilemap.OnRotate += this.CenterOnSourceTile;
                this.Actor = dependency.Register<ActorHelper>();
            }
        }

        public void Arrange(Rectangle container, string stagePath)
        {
            this.Container = container;
            this.Tilemap.Arrange(stagePath);

            // boundingbox should be all 4 corners of the bounding rectangle (the diagonal of tilemap size)
            // plus padding for when that corner is the center of rotation (half of containersize on each side)
            // below formula gives slightly more than necessary (might be tilesize?), but will do for now
            this.TilemapBoundingBox = new Vector2(this.Tilemap.TilemapSize.Length()) + this.ContainerSize;

            // the real size of the stage is the max of the tilemap bounding box and the containing rectangle
            this.StageSize = Vector2.Max(this.TilemapBoundingBox, this.ContainerSize);

            this.Tilemap.CalculateOffset(center: this.StageSize / 2);

            this.Camera.Arrange(this.StageSize, this.Container);
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();
            if (this.Input.KeyPressed(Keys.H))
                this.CenterOnSourceTile();

            if (this.Input.KeyPressed(Keys.K))
                this.Actor.Add(this.Tilemap.Map.Values.Random());

            if (this.Input.MouseMoved())
            {
                var virtualMouseVector = this.Input.CurrentVirtualMouseVector;
                var cameraTranslatedMouseVector = this.Camera.FromScreen(virtualMouseVector);
                if (this.Container.Contains(virtualMouseVector))
                    this.Tilemap.TrackTiles(cameraTranslatedMouseVector);
                else
                    this.Tilemap.UntrackTiles();
            }
            
            if ((this.Input.MousePressed(MouseButton.Left)) && (this.Tilemap.CursorTile != null))
            {
                var actorOnTile = this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == this.Tilemap.CursorTile));

                if (actorOnTile == this.SourceActor)
                    this.SourceActor = null;
                else
                    this.SourceActor = actorOnTile;
            }

            if (this.SourceActor != null)
                Static.Memo.AppendLine($"Actor: {this.SourceActor.Tile.Cube}");
        }

        void IDraw<BackgroundDraw>.Draw(SpriteBatch spriteBatch)
        {
            // spriteBatch.DrawTo(this.BlankTexture, this.Camera.CameraBox, new Color(20, 60, 90), depth: .05f);
            var size = this.Camera.Plane + new Vector2(2); // 1px rounding offset on each side
            spriteBatch.DrawTo(this.BackgroundTexture, size.ToRectangle(), Color.White, depth: .05f);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var actor in this.Actor.Actors)
            {
                var sourcePosition = actor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                var sizeOffset = actor.Texture.ToVector() / 2;

                var color = (actor == this.SourceActor) ? Color.Coral : Color.White;
                spriteBatch.DrawAt(actor.Texture, sourcePosition - sizeOffset, color, depth: .5f);
            }
        }

        // stage should be the one that tracks the SourceTile, CursorTile, SourceActor, CursorActor, etc
        // 'annotation' i.e. fog of war for actors should be here
        // question is how to handle drawing -> tilemap and tiles should give transformed coordinates
        // the tilemap transform matrix should not need to be used anywhere outside, similar to camera

        // the actorhelper does not draw the actors, the stagehelper should draw instead
        // actors should be replaced with shaded ? when not visible based on last position they were seen

        #endregion

        #region Helper Methods

        protected void CenterOnSourceTile()
        {
            // TODO setting to toggle this behavior on/off
            var setting = false;
            if (setting && this.SourceTile != null)
                this.Camera.CenterOn(this.Tilemap.SourceTileMiddle);
        }

        #endregion
    }
}