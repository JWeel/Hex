using Extended.Extensions;
using Hex.Auxiliary;
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
using System.Collections.Generic;
using System.Linq;

namespace Hex.Helpers
{
    public class StageHelper : IRegister, IUpdate<NormalUpdate>, IDraw<BackgroundDraw>, IDraw<ForegroundDraw>
    {
        #region Constructors

        public StageHelper(InputHelper input, Texture2D blankTexture, ContentManager content)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;

            this.HiddenTexture = content.Load<Texture2D>("graphics/hidden");
            this.BackgroundTexture = content.Load<Texture2D>("graphics/background");

            this.VisibilityMap = new Dictionary<Actor, IDictionary<Hexagon, bool>>();
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

        // TODO focus tile should only be in this class.
        public Hexagon FocusTile => this.Tilemap.FocusTile;

        public Actor SourceActor { get; protected set; }

        /// <summary> A transform matrix that scales and moves the stage relative to camera position. </summary>
        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.Map.Count;
        public int TilemapRotationInterval => this.Tilemap.WraparoundRotationInterval;

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D HiddenTexture { get; }
        protected Texture2D BackgroundTexture { get; }

        protected CameraHelper Camera { get; set; }
        protected TilemapHelper Tilemap { get; set; }
        protected ActorHelper Actor { get; set; }

        protected IDictionary<Actor, IDictionary<Hexagon, bool>> VisibilityMap;

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            using (new DependencyScope(dependency))
            {
                this.Camera = dependency.Register<CameraHelper>();
                this.Tilemap = dependency.Register<TilemapHelper>();
                this.Tilemap.OnRotate += this.CenterOnSourceActor;
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

            this.Actor.Reset();
            this.VisibilityMap.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();
            if (this.Input.KeyPressed(Keys.H))
                this.CenterOnSourceActor();

            if (this.Input.KeyPressed(Keys.K))
            {
                var tile = this.FocusTile ?? this.Tilemap.Map.Values.Random();
                if (this.Actor.Actors.Any(actor => (actor.Tile == tile)))
                    return;

                var actor = this.Actor.Add();
                this.Actor.Move(actor, tile);
                this.VisibilityMap[actor] = this.Tilemap.DetermineFogOfWar(actor.Tile, actor.ViewDistance);
                if (this.FocusTile != null)
                {
                    this.SourceActor = actor;
                    this.Tilemap.ApplyVisibility(this.VisibilityMap[actor]);
                    this.Tilemap.ResetMovementOverlay();
                }
            }

            if (this.Input.MouseMoved())
            {
                var virtualMouseVector = this.Input.CurrentVirtualMouseVector;
                var cameraTranslatedMouseVector = this.Camera.FromScreen(virtualMouseVector);

                if (this.Container.Contains(virtualMouseVector))
                {
                    if (this.Tilemap.Focus(cameraTranslatedMouseVector))
                    {
                        if ((this.SourceActor != null) && (this.FocusTile != this.SourceActor.Tile))
                        {
                            // TODO if FocusTile contains another actor
                            // check up to six neighbor tiles
                            //  if neighbor exists and does not have actor
                            //      create path to them
                            //      (path should also return cost)
                            // get min-cost neighbor path
                            //  if exists
                            //      apply that path in movement overlay
                            //  else
                            //      do not apply movement overlay (i.e. target is unreachable)

                            this.Tilemap.ApplyMovementOverlay(this.SourceActor.Tile, this.FocusTile, this.SourceActor.MoveDistance);
                        }


                    }
                }
                else
                {
                    this.Tilemap.Unfocus();
                }
            }

            if (this.Input.MousePressed(MouseButton.Left))
            {
                var actorOnTile = this.FocusTile?.Into(tile => this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == tile)));
                if ((this.FocusTile != default) && (actorOnTile != default) && (actorOnTile != this.SourceActor))
                {
                    this.SourceActor = actorOnTile;
                    this.Tilemap.ApplyVisibility(this.VisibilityMap[this.SourceActor]);
                    this.Tilemap.ResetMovementOverlay();
                }
                else
                {
                    this.SourceActor = null;
                    this.Tilemap.ResetVisibility();
                    this.Tilemap.ResetMovementOverlay();
                }
            }

            // todo dont call these every loop
            // should change focus a little: add a FocusChanged flag, maybe limited to this update method scope
            // if focuschanged and keydown ctrl, then apply
            // if keyup ctrl or focus changed AND focus is null, reset
            if (this.Input.KeysDownAny(Keys.LeftControl, Keys.RightControl) && (this.FocusTile != null))
                this.Tilemap.ApplyRing(this.FocusTile, radius: 2);
            if (this.Input.KeysReleased(Keys.LeftControl, Keys.RightControl) || (this.FocusTile == null))
                this.Tilemap.ResetEffects();

            if (this.SourceActor != null)
                Static.Memo.AppendLine($"Actor: {this.SourceActor.Tile.Cube}");
        }

        void IDraw<BackgroundDraw>.Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawTo(this.BlankTexture, this.Camera.CameraBox, new Color(30, 30, 30), depth: .04f);

            var size = this.StageSize + new Vector2(2); // 1px rounding offset on each side
            spriteBatch.DrawTo(this.BackgroundTexture, size.ToRectangle(), Color.White, depth: .05f);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var actor in this.Actor.Actors)
            {
                var sourcePosition = actor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                var color = (actor == this.SourceActor) ? Color.Coral : Color.White;
                var texture = actor.Texture;
                var textureScale = actor.TextureScale;

                if ((this.SourceActor != null) && (!this.VisibilityMap[this.SourceActor][actor.Tile]))
                {
                    color = color.Blend(40);
                    texture = this.HiddenTexture;
                }

                var sizeOffset = (texture.ToVector() * textureScale) / 2;
                spriteBatch.DrawAt(texture, sourcePosition - sizeOffset, color, scale: textureScale, depth: .5f);
            }
        }

        // stage should be the one that tracks the SourceTile, CursorTile, SourceActor, CursorActor, etc
        // actors should be replaced with shaded ? when not visible based on last position they were seen

        #endregion

        #region Helper Methods

        protected void CenterOnSourceActor()
        {
            // TODO setting to toggle this behavior on/off
            var setting = false;
            if (setting && this.SourceActor.Tile != null)
            {
                var position = this.SourceActor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                this.Camera.CenterOn(Vector2.Round(position));
            }
        }

        #endregion
    }
}