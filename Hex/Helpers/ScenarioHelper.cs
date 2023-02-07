using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Models;
using Hex.Models.Actors;
using Hex.Models.Tiles;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Helpers
{
    public class ScenarioHelper : IRegister, IUpdate<NormalUpdate>, IDraw<BackgroundDraw>, IDraw<ForegroundDraw>, IActivate
    {
        #region Constructors

        public ScenarioHelper(InputHelper input, Texture2D blankTexture, ContentManager content)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;

            this.HiddenTexture = content.Load<Texture2D>("graphics/hidden");
            this.BackgroundTexture = content.Load<Texture2D>("graphics/background");
            this.OverlayFullTexture = content.Load<Texture2D>("graphics/xxip");
            this.OverlayHalfTexture = content.Load<Texture2D>("graphics/xxiph");

            this.DiscoveryByFactionMap = new Dictionary<Faction, IDictionary<Hexagon, bool>>();
            this.VisibilityByFactionMap = new Dictionary<Faction, IDictionary<Hexagon, bool>>();

            this.LastSourceActorMap = new Dictionary<Faction, Actor>();
        }

        #endregion

        #region Data Members

        public bool IsActive { get; protected set; }

        /// <summary> The rectangle of the widget, control, or component that contains this scenario. </summary>
        public Rectangle Container { get; protected set; }

        /// <summary> The size of the widget, control, or component that contains this scenario. </summary>
        public Vector2 ContainerSize => this.Container.Size.ToVector2();

        /// <summary> The bounding box which fully contains the tilemap in any rotation. </summary>
        /// <remarks> This is represented by a vector instead of a rectangle, because it specifies the coordinates of the corner opposite the origin: a rectangle can formed by taking origin plus this vector. </remarks>
        public Vector2 TilemapBoundingBox { get; protected set; }

        /// <summary> The unbound size of the scenario. This is the max of <see cref="TilemapBoundingBox"/> and <see cref="ContainerSize"/>. </summary>
        public Vector2 ScenarioSize { get; protected set; }

        /// <summary> The tile over which the cursor is hovering. </summary>
        public Hexagon FocusTile { get; protected set; }

        private Actor _sourceActor;
        public Actor SourceActor
        {
            get => _sourceActor;
            protected set
            {
                if (_sourceActor == value)
                    return;

                _sourceActor = value;
                this.OnSourceActorChange?.Invoke(_sourceActor);
            }
        }

        /// <summary> A transform matrix that scales and moves the scenario relative to its internal camera. </summary>
        public Matrix TranslationMatrix => this.Camera.TranslationMatrix;

        public int TileCount => this.Tilemap.Map.Count;
        public int TilemapRotationInterval => this.Tilemap.WraparoundRotationInterval;

        public Faction SourceFaction => this.Faction.ActiveFaction;

        public event Action<Actor> OnSourceActorChange;

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected Texture2D HiddenTexture { get; }
        protected Texture2D BackgroundTexture { get; }
        protected Texture2D OverlayFullTexture { get; }
        protected Texture2D OverlayHalfTexture { get; }

        protected ConfigurationHelper Configuration { get; set; }
        protected CameraHelper Camera { get; set; }
        protected TilemapHelper Tilemap { get; set; }
        protected FactionHelper Faction { get; set; }
        protected ActorHelper Actor { get; set; }
        protected TurnHelper Turn { get; set; }

        protected IDictionary<Faction, IDictionary<Hexagon, bool>> DiscoveryByFactionMap { get; }
        protected IDictionary<Faction, IDictionary<Hexagon, bool>> VisibilityByFactionMap { get; }
        protected IDictionary<Faction, Actor> LastSourceActorMap { get; }

        protected IDictionary<Hexagon, Color> TileEffectMap { get; set; }

        protected (Hexagon Tile, bool Accessible, double Accrue)[] SourcePath { get; set; }

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
            this.Configuration = dependency.Register<ConfigurationHelper>();
            using (new DependencyScope(dependency))
            {
                this.Camera = dependency.Register<CameraHelper>();
                this.Tilemap = dependency.Register<TilemapHelper>();
                this.Tilemap.OnRotate += this.CenterOnSourceActor;
                this.Tilemap.OnRotate += this.SortActorDrawOrder;
                this.Faction = dependency.Register<FactionHelper>();
                this.Actor = dependency.Register<ActorHelper>();
                this.Turn = dependency.Register<TurnHelper>();
            }
        }

        public void Arrange(Rectangle container, string scenarioPath)
        {
            this.Container = container;
            this.Tilemap.Arrange(scenarioPath);
            this.ResetScenario();
        }

        public void Arrange(Rectangle container, Shape shape)
        {
            this.Container = container;
            this.Tilemap.Arrange(shape, 8, 4);
            this.ResetScenario();
        }

        protected void ResetScenario()
        {
            // boundingbox should be all 4 corners of the bounding rectangle (the diagonal of tilemap size)
            // plus padding for when that corner is the center of rotation (half of containersize on each side)
            // below formula gives slightly more than necessary (might be tilesize?), but will do for now
            this.TilemapBoundingBox = new Vector2(this.Tilemap.TilemapSize.Length()) + this.ContainerSize;

            // the real size of the scenario is the max of the tilemap bounding box and the containing rectangle
            this.ScenarioSize = Vector2.Max(this.TilemapBoundingBox, this.ContainerSize);

            this.Tilemap.ApplyOffsetToCenter(center: this.ScenarioSize / 2);

            this.Camera.Arrange(this.ScenarioSize, this.Container);

            this.Actor.Reset();

            this.DiscoveryByFactionMap.Clear();
            this.VisibilityByFactionMap.Clear();

            this.LastSourceActorMap.Clear();
            this.SourceActor = null;
            this.SourcePath = null;
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();
            if (this.Input.KeyPressed(Keys.H))
                this.CenterOnSourceActor();

            if (this.Input.KeyPressed(Keys.Y))
            {
                if (this.SourceFaction != null)
                    this.LastSourceActorMap[this.SourceFaction] = this.SourceActor;

                this.Faction.Toggle(); // modifies SourceFaction (TBD more elegant to not rely on side effects)
                if (this.SourceFaction == null)
                {
                    this.SourceActor = null;
                }
                else
                {
                    this.SourceActor = this.LastSourceActorMap.GetOrDefault(this.SourceFaction);
                    this.SourcePath = null;
                }
            }

            if (this.Input.KeyPressed(Keys.L))
            {
                this.LastSourceActorMap[this.SourceFaction] = this.SourceActor;
                this.Turn.Next(); // modifies SourceFaction (TBD more elegant to not rely on side effects)

                // TODO check if no longer exists?
                this.SourceActor = this.LastSourceActorMap.GetOrDefault(this.SourceFaction);
                this.SourcePath = null;
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
                    // The alternative is resetting if Tilemap.Locate returns default, which looks less nice
                    this.SourcePath = null;
                }
            }

            if (this.Input.KeyPressed(Keys.K))
            {
                if (this.SourceFaction == null)
                    return;

                var tile = this.FocusTile ?? this.Tilemap.Map.Values.Random();
                if (this.TileContainsActor(tile))
                    return;

                var actor = this.Actor.Add();
                this.Actor.Move(actor, tile, cost: 0d);
                this.SortActorDrawOrder();
                if (this.FocusTile != null)
                {
                    this.SourceActor = actor;
                }

                var actorViewData = this.Actor.Actors
                    .Where(actor => (actor.Faction == this.SourceFaction))
                    .Select(actor => (actor.Tile, actor.ViewDistance));
                var visibility = this.Tilemap.DetermineFogOfWar(actorViewData);
                this.VisibilityByFactionMap[this.SourceFaction] = visibility;

                // TBD - initial discovery may come from somewhere else
                var discovery = this.DiscoveryByFactionMap.GetOrSet(this.SourceFaction, () => visibility.ToDictionary());
                visibility
                    .Where(x => x.Value)
                    .Each(x => discovery[x.Key] = true);

                this.SourcePath = null;
            }

            if (this.Input.MousePressed(MouseButton.Left))
            {
                var actorOnTile = this.FocusTile?.Into(tile => this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == tile)));
                if ((actorOnTile != default) && (actorOnTile != this.SourceActor) && (actorOnTile.Faction == this.SourceFaction))
                {
                    this.SourceActor = actorOnTile;
                    this.SourcePath = null;
                }
                else
                {
                    if ((this.SourceActor != null) && (this.SourceActor.MovementAllowed > 1) &&
                        (this.FocusTile != null) && (this.SourceActor.Tile != this.FocusTile) &&
                        (this.SourcePath != null) && this.SourcePath.Any(x => (x.Tile == this.FocusTile)))
                    {
                        var tile = (this.SourcePath.First(x => (x.Tile == this.FocusTile)).Accessible) ?
                            this.FocusTile :
                            this.SourcePath.Last(x => x.Accessible).Tile;
                        var cost = this.SourcePath.First(x => (x.Tile == tile)).Accrue;

                        this.Actor.Move(this.SourceActor, tile, cost);

                        // this.Tilemap.Focus(tile);
                        // this.Tilemap.Source(tile);

                        var actorViewData = this.Actor.Actors
                            .Where(actor => (actor.Faction == this.SourceFaction))
                            .Select(actor => (actor.Tile, actor.ViewDistance));
                        var visibility = this.Tilemap.DetermineFogOfWar(actorViewData);
                        this.VisibilityByFactionMap[this.SourceFaction] = visibility;

                        var discovery = this.DiscoveryByFactionMap[this.SourceFaction];
                        visibility
                            .Where(x => x.Value)
                            .Each(x => discovery[x.Key] = true);

                        if (tile == this.FocusTile)
                            this.SourcePath = null;
                        else
                        {
                            var path = this.Tilemap
                                .DefinePath(this.SourceActor.Tile, this.FocusTile, this.SourceActor.MovementAllowed, this.IsTileInaccessible, this.TileIsVisible)
                                .ToArray();
                            this.SourcePath = path;
                        }
                    }
                    else
                    {
                        this.SourceActor = null;
                        this.SourcePath = null;
                    }
                }
            }

            if (this.Input.KeysDownAny(Keys.LeftControl, Keys.RightControl) && (this.FocusTile != null))
                this.TileEffectMap = this.Tilemap.DetermineRingEffect(this.FocusTile, radius: 2);
            if (this.Input.KeysReleasedAny(Keys.LeftControl, Keys.RightControl) || this.FocusLost)
                this.TileEffectMap = default;

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

            if (this.FocusMoved && !this.TileContainsActor(this.FocusTile) && (this.SourceActor != null) &&
                this.DiscoveryByFactionMap[this.SourceFaction][this.FocusTile])
            {
                var path = this.Tilemap
                    .DefinePath(this.SourceActor.Tile, this.FocusTile, this.SourceActor.MovementAllowed, this.IsTileInaccessible, this.TileIsVisible)
                    .Select(x => (x.Tile, Accessible: (x.InRange && !this.TileContainsActor(x.Tile)), x.Accrue))
                    .ToArray();
                this.SourcePath = path;
            }

            // This does not need to be stored in a property, as it can be stored in local at start of method.
            // But with the property, changes (FocusMoved, FocusLost, etc) are more readable and succinct.
            this.LastFocusTile = this.FocusTile;

            if (this.SourceActor != null)
            {
                Static.Memo.AppendLine($"Actor: {this.SourceActor.Tile.Cube} > {this.SourceActor.MovementAllowed}");
            }
            Static.Memo.AppendLine($"Turn: {this.Turn.TurnCount}");
        }

        void IDraw<BackgroundDraw>.Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawTo(this.BlankTexture, this.Camera.CameraBox, new Color(30, 30, 30), depth: .04f);

            var size = this.ScenarioSize + new Vector2(2); // 1px rounding offset on each side
            spriteBatch.DrawTo(this.BackgroundTexture, size.ToRectangle(), Color.White, depth: .05f);
        }

        void IDraw<ForegroundDraw>.Draw(SpriteBatch spriteBatch)
        {
            foreach (var actor in this.Actor.Actors)
            {
                if ((this.SourceFaction != null) && this.DiscoveryByFactionMap.TryGetValue(this.SourceFaction, out var discoveryMap) && !discoveryMap[actor.Tile])
                    continue;

                var sourcePosition = actor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                var color = Color.White.Desaturate(actor.Faction.Color, .1f);
                var texture = actor.Texture;
                var textureScale = actor.TextureScale;
                if ((this.SourceFaction != null) && this.VisibilityByFactionMap.TryGetValue(this.SourceFaction, out var visibilityMap) && !visibilityMap[actor.Tile])
                {
                    color = color.Blend(144);
                    textureScale *= 1.33f;
                    texture = this.HiddenTexture;
                }
                var sizeOffset = new Vector2(texture.Width / 2f, texture.Height / 4f * 3) * textureScale;
                spriteBatch.DrawAt(texture, sourcePosition - sizeOffset, color, scale: textureScale, depth: .45f);
            }

            var movementOverlay = this.SourcePath?.ToDictionary(x => x.Tile, x => x.Accessible);
            foreach (var tile in this.Tilemap.Map.Values)
            {
                var position = tile.Position.Transform(this.Tilemap.RenderRotationMatrix);
                if (this.DiscoveryByFactionMap.TryGetValue(this.SourceFaction, out var discovery))
                {
                    if (discovery.NotNullTryGetValue(tile, out var explored) && !explored)
                    {
                        spriteBatch.DrawAt(this.OverlayFullTexture, position, Color.Black, this.Tilemap.Rotation, depth: .1f);
                        continue;
                    }
                }

                if (tile == this.SourceActor?.Tile)
                    spriteBatch.DrawAt(this.OverlayFullTexture, position, Color.Ivory.Blend(224), this.Tilemap.Rotation, depth: .16f);

                if (tile == this.FocusTile)
                    spriteBatch.DrawAt(this.OverlayFullTexture, position, Color.White.Blend(144), this.Tilemap.Rotation, depth: .16f);

                if (this.TileEffectMap.NotNullTryGetValue(tile, out var effectColor))
                    spriteBatch.DrawAt(this.OverlayFullTexture, position, effectColor, this.Tilemap.Rotation, depth: .31f);

                if ((tile != this.SourceActor?.Tile) && movementOverlay.NotNullTryGetValue(tile, out var accessible))
                {
                    // TODO color should be customizable, i.e. blue for movement, red if targeting foe, etc
                    var color = accessible ? new Color(100, 150, 200).Blend(144) : new Color(50, 50, 50).Blend(64);
                    spriteBatch.DrawAt(this.OverlayHalfTexture, position, color, this.Tilemap.Rotation, depth: .32f);
                }

                if (this.VisibilityByFactionMap.TryGetValue(this.SourceFaction, out var visibility))
                {
                    if (visibility.NotNullTryGetValue(tile, out var visible) && !visible)
                        spriteBatch.DrawAt(this.OverlayFullTexture, position, new Color(100, 100, 100).Blend(128), this.Tilemap.Rotation, depth: .4f);
                }
            }

            Static.FocalSquares?.Each(square => spriteBatch.DrawTo(this.BlankTexture, square, Color.Red));
        }

        public void Activate()
        {
            this.IsActive = true;
            // TODO should do this automatically
            // maybe can leverage IRegister, have it auto get this logic somehow
            // issue is cannot rely on DependencyHelper.Root since that's Core
            // (Tilemap and Camera get Attached to Core, not to scenario)
            // so it would have to happen inside Register but the registering instance is not known
            // in other words: dunno how to do this
            this.Tilemap.Activate();
            this.Camera.Activate();
        }

        public void Deactivate()
        {
            this.IsActive = false;
            // TODO should do this automatically
            this.Tilemap.Deactivate();
            this.Camera.Deactivate();
        }

        #endregion

        #region Helper Methods

        protected void CenterOnSourceActor()
        {
            if (this.Configuration.CenterTilemapRotationOnSource && (this.SourceActor?.Tile != null))
            {
                var position = this.SourceActor.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                this.Camera.CenterOn(Vector2.Round(position));
            }
        }

        protected void SortActorDrawOrder()
        {
            var comparer = Comparer<Actor>.Create((left, right) =>
            {
                var leftPosition = left.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                var rightPosition = right.Tile.Middle.Transform(this.Tilemap.RenderRotationMatrix);
                return leftPosition.Y.CompareTo(rightPosition.Y);
            });
            this.Actor.Actors.Sort(comparer);
        }

        protected bool TryGetActorOnTile(Hexagon tile, out Actor actor)
        {
            actor = this.Actor.Actors.FirstOrDefault(actor => (actor.Tile == tile));
            return (actor != default);
        }

        protected bool TileContainsActor(Hexagon tile) =>
            this.TryGetActorOnTile(tile, out _);

        protected bool IsTileInaccessible(Hexagon tile) =>
            (!this.TileIsDiscovered(tile) || this.TileContainsHostileVisibleActor(tile));

        protected bool TileIsDiscovered(Hexagon tile) =>
            this.DiscoveryByFactionMap
                .GetOrDefault(this.SourceFaction)
                .NotNullGetOrDefault(tile);

        protected bool TileIsVisible(Hexagon tile) =>
            // check if can use index accessor instead
            this.VisibilityByFactionMap
                .GetOrDefault(this.SourceFaction)
                .NotNullGetOrDefault(tile);

        protected bool TileContainsHostileVisibleActor(Hexagon tile) =>
            (this.VisibilityByFactionMap[this.SourceFaction].TryGetValue(tile, out var visible) &&
                this.TryGetActorOnTile(tile, out var actor) &&
                    !actor.Faction.Allies.Contains(this.SourceActor.Faction));

        #endregion
    }
}