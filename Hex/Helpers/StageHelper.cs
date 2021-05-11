using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Models;
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

            this.VisibilityByFactionMap = new Dictionary<Faction, IDictionary<Hexagon, bool>>();
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

        /// <summary> The tile over which the cursor is hovering. </summary>
        public Hexagon FocusTile { get; protected set; }

        public Actor SourceActor { get; protected set; }

        /// <summary> A transform matrix that scales and moves the stage relative to its internal camera. </summary>
        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.Map.Count;
        public int TilemapRotationInterval => this.Tilemap.WraparoundRotationInterval;

        public Faction ActiveFaction => this.Faction.ActiveFaction;

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D HiddenTexture { get; }
        protected Texture2D BackgroundTexture { get; }

        protected CameraHelper Camera { get; set; }
        protected TilemapHelper Tilemap { get; set; }
        protected FactionHelper Faction { get; set; }
        protected ActorHelper Actor { get; set; }

        protected IDictionary<Faction, IDictionary<Hexagon, bool>> VisibilityByFactionMap { get; set; }

        protected Hexagon LastFocusTile { get; set; }

        /// <summary> Indicates whether tile focus was changed. </summary>
        protected bool FocusChanged =>
            (this.FocusTile != this.LastFocusTile);

        /// <summary> Indicates whether tile focus was changed and is now <see langword="not null"/>. </summary>
        protected bool FocusMoved =>
            ((this.FocusTile != this.LastFocusTile) && (this.FocusTile != null));

        /// <summary> Indicates whether tile focus went from <see langword="not null"/> to <see langword="null"/>. </summary>
        protected bool FocusLost =>
            ((this.LastFocusTile != null) && (this.FocusTile == null));

        /// <summary> Indicates whether tile focus went from <see langword="null"/> to <see langword="not null"/>. </summary>
        protected bool FocusGained =>
            ((this.LastFocusTile == null) && (this.FocusTile != null));

        #endregion

        #region Public Methods

        public void Register(DependencyHandler dependency)
        {
            using (new DependencyScope(dependency))
            {
                this.Camera = dependency.Register<CameraHelper>();
                this.Tilemap = dependency.Register<TilemapHelper>();
                this.Tilemap.OnRotate += this.CenterOnSourceActor;
                this.Faction = dependency.Register<FactionHelper>();
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
            this.VisibilityByFactionMap.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();
            if (this.Input.KeyPressed(Keys.H))
                this.CenterOnSourceActor();

            if (this.Input.KeyPressed(Keys.L))
            {
                this.Faction.Cycle();
                this.SourceActor = null;
                this.Tilemap.Unsource();
                this.Tilemap.ResetMovementOverlay();
                if ((this.Faction.ActiveFaction == null) || !this.VisibilityByFactionMap.ContainsKey(this.Faction.ActiveFaction))
                {
                    this.Tilemap.ResetVisibility();
                }
                else
                {
                    var visibility = this.VisibilityByFactionMap[this.Faction.ActiveFaction];
                    this.Tilemap.ApplyVisibility(visibility);
                }
            }

            if (this.Input.MouseMoved())
            {
                var virtualMouseVector = this.Input.CurrentVirtualMouseVector;
                if (this.Container.Contains(virtualMouseVector))
                {
                    var cameraTranslatedMouseVector = this.Camera.FromScreen(virtualMouseVector);
                    this.FocusTile = this.Tilemap.Locate(cameraTranslatedMouseVector);
                }
                else
                {
                    this.FocusTile = default;
                    // By resetting here, overlay is kept while mouse is inside container even if not over a tile
                    this.Tilemap.ResetMovementOverlay();
                }
            }

            if (this.Input.KeyPressed(Keys.K))
            {
                if (this.Faction.ActiveFaction == null)
                    return;

                var tile = this.FocusTile ?? this.Tilemap.Map.Values.Random();
                if (this.TileContainsActor(tile))
                    return;

                var actor = this.Actor.Add();
                this.Actor.Move(actor, tile);
                if (this.FocusTile != null)
                {
                    this.SourceActor = actor;
                    this.Tilemap.Source(actor.Tile);
                    this.Tilemap.ResetMovementOverlay();
                }

                var actorViewData = this.Actor.Actors
                    .Where(actor => (actor.Faction == this.Faction.ActiveFaction))
                    .Select(actor => (actor.Tile, actor.ViewDistance));
                var visibility = this.Tilemap.DetermineFogOfWar(actorViewData);
                this.VisibilityByFactionMap[this.Faction.ActiveFaction] = visibility;
                this.Tilemap.ApplyVisibility(visibility);
            }

            if (this.Input.MousePressed(MouseButton.Left))
            {
                var actorOnTile = this.FocusTile?.Into(tile => this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == tile)));
                if ((actorOnTile != default) && (actorOnTile != this.SourceActor))
                {
                    this.SourceActor = actorOnTile;
                    this.Tilemap.Source(actorOnTile.Tile);
                    this.Tilemap.ResetMovementOverlay();
                }
                else
                {
                    this.SourceActor = null;
                    this.Tilemap.Unsource();
                    this.Tilemap.ResetMovementOverlay();
                }
            }

            if (this.Input.KeysDownAny(Keys.LeftControl, Keys.RightControl) && (this.FocusTile != null))
                this.Tilemap.ApplyRing(this.FocusTile, radius: 2);
            if (this.Input.KeysReleasedAny(Keys.LeftControl, Keys.RightControl) || this.FocusLost)
                this.Tilemap.ResetEffects();

            if (this.FocusTile != null)
            {
                // TODO: Direction should be based on rotation, make GetNeighbor relative to Rotation
                if (this.Input.KeyPressed(Keys.Right))
                    this.FocusTile = this.Tilemap.GetNeighbor(this.FocusTile, Direction.Right) ?? this.FocusTile;
                if (this.Input.KeyPressed(Keys.Left))
                    this.FocusTile = this.Tilemap.GetNeighbor(this.FocusTile, Direction.Left) ?? this.FocusTile;
                if (this.Input.KeyPressed(Keys.Up))
                    this.FocusTile = this.Tilemap.GetNeighbor(this.FocusTile, Direction.UpRight) ?? this.FocusTile;
                if (this.Input.KeyPressed(Keys.Down))
                    this.FocusTile = this.Tilemap.GetNeighbor(this.FocusTile, Direction.DownLeft) ?? this.FocusTile;
            }

            if (this.FocusChanged)
                this.Tilemap.Focus(this.FocusTile);

            if (this.FocusMoved && !this.TileContainsActor(this.FocusTile) && (this.SourceActor != null))
            {
                var path = this.Tilemap
                    .DefinePath(this.SourceActor.Tile, this.FocusTile, this.SourceActor.MoveDistance, this.TileContainsHostileActor)
                    .Select(x => (x.Tile, Accessible: (x.InRange && !this.TileContainsActor(x.Tile))));
                this.Tilemap.ApplyMovementOverlay(path);
            }

            // This does not need to be stored in a property, as it can be stored in local at start of method.
            // But with the property, changes (FocusMoved, FocusLost, etc) are more readable and succinct.
            this.LastFocusTile = this.FocusTile;

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
                var color = ((actor == this.SourceActor) ? Color.LightGray : Color.White).Desaturate(actor.Faction.Color, .2f);
                var texture = actor.Texture;
                var textureScale = actor.TextureScale;
                // TBD should hidden actors be 'visible' in the fog of war? or should add an AwarenessMap?
                if ((this.Faction.ActiveFaction != null) && this.VisibilityByFactionMap.ContainsKey(this.Faction.ActiveFaction) &&
                    !this.VisibilityByFactionMap[this.Faction.ActiveFaction][actor.Tile])
                {
                    color = color.Blend(40);
                    texture = this.HiddenTexture;
                }
                var sizeOffset = (texture.ToVector() * textureScale) / 2;
                spriteBatch.DrawAt(texture, sourcePosition - sizeOffset, color, scale: textureScale, depth: .5f);
            }
        }

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

        protected bool TryGetActorOnTile(Hexagon tile, out Actor actor)
        {
            actor = this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == tile));
            return (actor != default);
        }

        protected bool TileContainsActor(Hexagon tile) =>
            this.TryGetActorOnTile(tile, out _);

        protected bool TileContainsHostileActor(Hexagon tile) =>
            (this.TryGetActorOnTile(tile, out var actor) && !actor.Faction.Allies.Contains(this.SourceActor.Faction));

        #endregion
    }
}