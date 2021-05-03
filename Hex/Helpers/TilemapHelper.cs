using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Models;
using Hex.Models.Tiles;
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
using System.IO;
using System.Linq;

namespace Hex.Helpers
{
    public class TilemapHelper : IUpdate<NormalUpdate>, IDraw<BackgroundDraw>
    {
        #region Constants

        // no idea what these divisions are (possibly to account for hexagon borders sharing pixels?)
        // without them there are small gaps or overlap between hexagons, especially as coordinates increase
        // note that these are specific to 25/29 (pointy) sizes!
        private const double SHORT_OVERLAP_DIVISOR = 1.80425; // this seems to be the offset for odd rows(pointy)/cols(flat)
        private const double LONG_OVERLAP_DIVISOR = 2.07137; // but this one no idea, doesn't seem to match any offset

        // no idea why this works, but without it mouse to hexagon conversion is off and gets worse as it moves further from origin
        private const int SCREEN_TO_HEX_MAGIC_OFFSET_NUMBER = 169;

        /// <summary> An error-margin that can be used to always push Lerp operations in the same direction when a point is exactly between two cubes. </summary>
        private static readonly Vector3 EPSILON = new Vector3(0.000001f, 0.000002f, -0.000003f);

        private static Direction[] DIRECTIONS { get; } = new[] { Direction.UpRight, Direction.Right, Direction.DownRight, Direction.DownLeft, Direction.Left, Direction.UpLeft };

        #endregion

        #region Constructors

        public TilemapHelper(InputHelper input, ContentManager content, Texture2D blankTexture, SpriteFont font)
        {
            this.Input = input;
            this.BlankTexture = blankTexture;
            this.Font = font;

            this.HexagonOuterTexture = content.Load<Texture2D>("Graphics/xop");
            this.HexagonInnerTexture = content.Load<Texture2D>("Graphics/xip");
            this.HexagonBorderPointyTexture = content.Load<Texture2D>("Graphics/xbp");
            this.HexagonBorderFlattyTexture = content.Load<Texture2D>("Graphics/xbf");

            this.HexagonBorderUpLeftTexture = content.Load<Texture2D>("Graphics/xbulp");
            this.HexagonBorderLeftTexture = content.Load<Texture2D>("Graphics/xblp");
            this.HexagonBorderDownLeftTexture = content.Load<Texture2D>("Graphics/xbblp");
            this.HexagonBorderDownLeftLargeTexture = content.Load<Texture2D>("Graphics/xbbllp");
            this.HexagonBorderTextureRange = new[] { HexagonBorderUpLeftTexture, HexagonBorderLeftTexture, HexagonBorderDownLeftTexture, HexagonBorderDownLeftTexture, HexagonBorderLeftTexture, HexagonBorderUpLeftTexture };
            this.HexagonBorderLargeTextureRange = new[] { HexagonBorderUpLeftTexture, HexagonBorderLeftTexture, HexagonBorderDownLeftLargeTexture, HexagonBorderDownLeftLargeTexture, HexagonBorderLeftTexture, HexagonBorderUpLeftTexture };

            this.TileSize = this.HexagonOuterTexture.ToVector();
            this.HexagonSizeAdjusted = (this.TileSize.X / SHORT_OVERLAP_DIVISOR, this.TileSize.Y / LONG_OVERLAP_DIVISOR);

            this.Map = new Dictionary<Cube, Hexagon>();
        }

        #endregion

        #region Properties

        /// <summary> Raised when tilemap rotation changes. </summary>
        public event Action OnRotate;

        /// <summary> The mapping of all tiles by cube-coordinates. </summary>
        public IDictionary<Cube, Hexagon> Map { get; protected set; }

        /// <summary> The tile over which the cursor is hovering. </summary>
        public Hexagon FocusTile { get; protected set; }

        /// <summary> The selected tile. </summary>
        public Hexagon SourceTile { get; protected set; }

        /// <summary> The combined size of all tiles. </summary>
        public Vector2 TilemapSize { get; protected set; }

        /// <summary> The distance between origin and tilemap that would set the tilemap centered in the bounding box. </summary>
        public Vector2 TilemapOffset { get; protected set; }

        /// <summary> A transform matrix that rotates and moves to relative render position. </summary>
        public Matrix RenderRotationMatrix =>
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / -2f - this.RenderPosition, 1)) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / 2f + this.RenderPosition + this.TilemapOffset, 1));

        protected Matrix TileRotationMatrix { get; set; }
        protected Matrix WraparoundRotationMatrix { get; set; }

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected SpriteFont Font { get; }

        /// <summary> The amount of rotation in radians to apply to tile sprites. </summary>
        protected float Rotation { get; set; }

        /// <summary> The amount of rotation in radians to apply to sprites that should maintain a relatively 'upward' orientation. </summary>
        /// <remarks> This is the result of performing a wraparound on <see cref="Rotation"/>. The amount wraps in range intervals of 60 degrees.
        /// <br/> E.g. with a degree range of [-29,31] : any rotation within this range of degrees is unaffected, but at 32 degrees the rotation becomes -29, and at -30 the rotation becomes 31. </remarks>
        public float WraparoundRotation { get; protected set; }

        /// <summary> An index in the range [0,5] which when multiplied by 60 corresponds to a degrees range in which <see cref="Rotation"/> lies.  </summary>
        public int WraparoundRotationInterval { get; protected set; }

        protected Vector2 RenderPosition { get; set; }

        protected Hexagon OriginTile { get; set; }
        protected Hexagon CenterTile { get; set; }
        public Hexagon LastFocusTile { get; set; }
        protected Hexagon LastSourceTile { get; set; }

        protected IDictionary<Hexagon, bool> FogOfWarMap { get; set; }
        protected IDictionary<Hexagon, bool> MovementOverlayMap { get; set; }

        // TODO is this needed
        protected bool CalculatedVisibility { get; set; }
        // TODO is this needed
        protected IDictionary<Hexagon, bool> VisibilityByHexagonMap { get; } = new Dictionary<Hexagon, bool>();

        protected IDictionary<Hexagon, (Direction Direction, BorderType Type)[]> BorderMap { get; set; }

        protected Texture2D HexagonOuterTexture { get; set; }
        protected Texture2D HexagonInnerTexture { get; set; }
        protected Texture2D HexagonBorderPointyTexture { get; set; }
        protected Texture2D HexagonBorderFlattyTexture { get; set; }

        protected Texture2D HexagonBorderDownLeftTexture { get; set; }
        protected Texture2D HexagonBorderDownLeftLargeTexture { get; set; }
        protected Texture2D HexagonBorderLeftTexture { get; set; }
        protected Texture2D HexagonBorderUpLeftTexture { get; set; }
        protected Texture2D[] HexagonBorderTextureRange { get; set; }
        protected Texture2D[] HexagonBorderLargeTextureRange { get; set; }

        protected Vector2 TileSize { get; set; }
        protected (double X, double Y) HexagonSizeAdjusted { get; set; }

        protected bool PrintCoords { get; set; }

        #endregion

        #region Methods

        public void Arrange(string path)
        {
            var axials = path.IsNullOrWhiteSpace() ?
                this.Spawn(8, 12, Shape.Hexagon) :
                this.Load(path);

            this.Map = axials
                .Select(axial =>
                {
                    var (q, r, elevation, slope, type) = axial;
                    var cube = Cube.FromAxial(axial.Q, axial.R);
                    var positionX = Math.Round(this.HexagonSizeAdjusted.X * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                    var positionY = Math.Round(this.HexagonSizeAdjusted.Y * (3.0 / 2.0 * r));
                    var position = new Vector2((float) positionX, (float) positionY);
                    return new Hexagon(cube, position, this.TileSize, elevation, slope, type);
                })
                .ToDictionary(x => x.Cube);

            if (this.Map.Any())
                this.RenderPosition = this.Map.Values
                    .Select(x => x.Position)
                    .Aggregate((aggregate, position) => Vector2.Min(aggregate, position));

            this.TilemapSize = this.CalculateTilesCombinedSize();
            this.VisibilityByHexagonMap.Clear();

            this.FocusTile = default;
            this.SourceTile = default;

            this.RecalculateRotations();
            this.RecalculateTileBorders();
            this.ResetVisibility();
            this.ResetMovementOverlay();
        }

        /// <summary> Generate a new tilemap using specified integers to determine shape and size. </summary>
        public (int Q, int R, int E, Direction S, TileType T)[] Spawn(int n, int m, Shape shape)
        {
            var axials = new List<(int Q, int R)>();
            switch (shape)
            {
                case Shape.Triangle:
                    for (int q = 0; q <= n; q++)
                        for (int r = 0; r <= n - q; r++)
                            axials.Add((q, r));
                    break;
                case Shape.Parallelogram:
                    for (int q = 0; q <= n; q++)
                        for (int r = 0; r <= m; r++)
                            axials.Add((q, r));
                    break;
                case Shape.Hexagon:
                    for (var q = -n; q <= n; q++)
                    {
                        var r1 = Math.Max(-n, -q - n);
                        var r2 = Math.Min(n, -q + n);
                        for (var r = r1; r <= r2; r++)
                            axials.Add((q, r));
                    }
                    if (n < 3) break;
                    // DONUT
                    axials.Remove(default);
                    axials.Remove((-1, 0));
                    axials.Remove((-1, 1));
                    axials.Remove((0, -1));
                    axials.Remove((0, 1));
                    axials.Remove((1, -1));
                    axials.Remove((1, 0));
                    axials.Remove((0, -2));
                    axials.Remove((1, -2));
                    axials.Remove((2, -2));
                    axials.Remove((-1, -1));
                    axials.Remove((2, -1));
                    axials.Remove((-2, 0));
                    axials.Remove((2, 0));
                    axials.Remove((-2, 1));
                    axials.Remove((1, 1));
                    axials.Remove((-2, 2));
                    axials.Remove((-1, 2));
                    axials.Remove((0, 2));
                    break;
                case Shape.Rectangle:
                    for (var r = 0; r < m - 1; r++)
                    {
                        var r_offset = (int) Math.Floor(r / 2f);
                        var r_end = (r % 2 == 1) ? r_offset : r_offset - 1;
                        for (var q = -r_offset; q < n - r_end; q++)
                            axials.Add((q, r));
                    }
                    break;
                case Shape.Line:
                    for (var q = -n; q <= n; q++)
                        axials.Add((q, 0));
                    break;
                default:
                    throw shape.Invalid();
            }
            // var ran = new Random();
            // Enumerable.Range(0, ran.Next(axials.Count))
            //     .Each(i => axials.RemoveAt(ran.Next(axials.Count)));
            return axials
                .Select(tuple => (tuple.Q, tuple.R, 1, Direction.None, ToTileType(tuple.Q, tuple.R)))
                .ToArray();

            static TileType ToTileType(int q, int r)
            {
                var cube = Cube.FromAxial(q, r);
                return
                    (cube.X % 7 == cube.Z) ? TileType.Mountain :
                    (cube.Z % 3 == cube.Y + 5) ? TileType.Sea :
                    TileType.Grass;
            }
        }

        public (int Q, int R, int E, Direction S, TileType T)[] Load(string path)
        {
            var random = new Random();
            return File.ReadAllLines(path)
                .Skip(1)
                .Where(line => !line.IsNullOrWhiteSpace())
                .Select(line =>
                {
                    var identifierSplit = line.Split(":");
                    var axialSplit = identifierSplit[0].Split(", ");
                    var q = int.Parse(axialSplit[0]);
                    var r = int.Parse(axialSplit[1]);

                    var bodySplit = identifierSplit[1].Substring(1).Split(" ");
                    var e = int.Parse(bodySplit[0]);
                    var s = Enum.Parse<Direction>(bodySplit[1]);
                    var t = Enum.Parse<TileType>(bodySplit[2]);

                    return (q, r, e, s, t);
                })
                .ToArray();
        }

        /// <summary> Determines the distance from origin necessary to render the tilemap so that it is centered on a specified position.
        /// <br/> The result is stored in <see cref="TilemapOffset"/>. </summary>
        public void CalculateOffset(Vector2 center)
        {
            // Get distance from top left (renderposition) to tilemap middle
            var relativeMiddle = this.TilemapSize / 2 + this.RenderPosition;
            // Subtract this distance from center to get offset for centered tilemap rendering
            this.TilemapOffset = Vector2.Round(center - relativeMiddle);
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.P))
                this.PrintCoords = !this.PrintCoords;

            if (!this.CalculatedVisibility && (this.SourceTile != default) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
            {
                this.CalculatedVisibility = true;
                this.VisibilityByHexagonMap.Clear();
                this.Map.Values
                    .Select(tile => (tile, this.DefineLineOfSight(this.SourceTile, tile, 10).Last().Visible))
                    .Each(this.VisibilityByHexagonMap.Add);
            };

            if (this.Input.MousePressed(MouseButton.Left))
            {
                this.LastSourceTile = this.SourceTile;
                this.SourceTile = (this.SourceTile != this.FocusTile) ? this.FocusTile : default;
                this.CalculatedVisibility = false;
            }

            if (this.VisibilityByHexagonMap.Any() && (this.Input.KeysUp(Keys.LeftAlt, Keys.RightAlt, Keys.LeftShift, Keys.RightShift) || (this.SourceTile == default)))
            {
                this.VisibilityByHexagonMap.Clear();
                this.CalculatedVisibility = false;
            }

            if (this.Input.KeyPressed(Keys.Z) && !this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: -30);
                else
                    this.Rotate(degrees: -60);
            else if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeyDown(Keys.Z) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: -3);
                else if (this.Input.KeyDown(Keys.Z) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: -1);

            if (this.Input.KeyPressed(Keys.X) && !this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: 30);
                else
                    this.Rotate(degrees: 60);
            else if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeyDown(Keys.X) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: 3);
                else if (this.Input.KeyDown(Keys.X) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: 1);

            if (this.SourceTile != null)
            {
                if (this.Input.KeyPressed(Keys.Right))
                    this.SourceTile = this.GetNeighbor(this.SourceTile, Direction.Right) ?? this.SourceTile;
                if (this.Input.KeyPressed(Keys.Left))
                    this.SourceTile = this.GetNeighbor(this.SourceTile, Direction.Left) ?? this.SourceTile;
                if (this.Input.KeyPressed(Keys.Up))
                    this.SourceTile = this.GetNeighbor(this.SourceTile, Direction.UpRight) ?? this.SourceTile;
                if (this.Input.KeyPressed(Keys.Down))
                    this.SourceTile = this.GetNeighbor(this.SourceTile, Direction.DownLeft) ?? this.SourceTile;
            }

            if (this.Input.KeyPressed(Keys.V))
            {
                this.Rotation = 0;
                this.RecalculateRotations();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var tile in this.Map.Values)
            {
                var cube = tile.Cube;
                var basePosition = tile.Position;
                var baseMiddle = tile.Middle;
                var position = basePosition.Transform(this.RenderRotationMatrix);

                var innerColor =
                (tile == this.SourceTile) ? Color.Coral
                    : (tile == this.OriginTile) ? Color.Gold
                    : (tile == this.CenterTile) ? Color.Aquamarine
                    : tile.Color != default ? tile.Color
                    : tile.TileType switch
                    {
                        // TileType.Mountain => Color.Tan,
                        TileType.Sea => new Color(100, 200, 220).Blend(80),
                        _ => new Color(190, 230, 160)
                    };
                spriteBatch.DrawAt(this.HexagonInnerTexture, position, innerColor, this.Rotation, depth: .15f);

                if (tile == this.FocusTile)
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, Color.White.Blend(150), this.Rotation, depth: .16f);

                if (this.VisibilityByHexagonMap.TryGetValue(tile, out var visibility))
                {
                    var visiblityOverlayColor = visibility ?
                        new Color(255, 255, 255).Blend(80) :
                        new Color(0, 0, 0).Blend(50);
                    // new Color(205, 235, 185, 100) :
                    // new Color(175, 195, 160, 100);
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, visiblityOverlayColor, this.Rotation, depth: .175f);
                }

                // offset center of hexagon to preserve rotational origin for overlay sprites
                var borderBasePosition = baseMiddle.Transform(this.RenderRotationMatrix);
                var borderTextureOffset = (this.TileSize / 2).Transform(this.WraparoundRotationMatrix);
                var borderPosition = borderBasePosition - borderTextureOffset;

                foreach (var border in this.BorderMap[tile])
                {
                    var (direction, borderType) = border;
                    if (borderType == BorderType.Slope)
                        continue;
                    if (borderType == BorderType.Edge)
                    {
                        spriteBatch.DrawAt(this.HexagonBorderPointyTexture, borderPosition, Color.Sienna, this.WraparoundRotation, depth: .075f);
                        continue;
                    }
                    if ((borderType != BorderType.Small) && (borderType != BorderType.Large))
                        throw new NotImplementedException($"Missing logic for drawing border type '{borderType}'.");

                    var (texture, flip) = this.GetDirectionalBorderTexture(direction, borderType);
                    var effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    spriteBatch.DrawAt(texture, borderPosition, Color.Sienna, this.WraparoundRotation, depth: .3f, effects: effects);
                }

                innerColor = tile.TileType switch
                {
                    // TileType.Mountain => new Color(130, 100, 60),
                    _ => new Color(100, 140, 70)
                };
                var outerDepth = (TileType.Mountain == tile.TileType) ? .26f : .25f;
                spriteBatch.DrawAt(this.HexagonOuterTexture, position, innerColor, this.Rotation, depth: outerDepth);

                if (this.PrintCoords)
                {
                    var (q, r) = cube.ToAxial();
                    var axialPrint = $"{q},{r}";
                    spriteBatch.DrawText(this.Font, axialPrint, position + new Vector2(5), Color.MistyRose, scale: 0.5f, .9f);
                }

                if ((this.FogOfWarMap != null) && (!this.FogOfWarMap[tile]))
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, new Color(100, 100, 100).Blend(128), this.Rotation, depth: .33f);

                if ((this.MovementOverlayMap != null) && this.MovementOverlayMap.GetOrDefault(tile))
                {
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, new Color(100, 150, 200).Blend(192), this.Rotation, depth: .32f);
                }
            }
        }

        public void Focus(Vector2 position)
        {
            var coordinates = this.ToTileCoordinates(position);
            this.LastFocusTile = this.FocusTile;
            this.FocusTile = this.Map.GetOrDefault(coordinates);

            // remove sourcetile?

            // if ((this.FocusTile != this.LastFocusTile) && (this.SourceTile != default))
            // {
            //     if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
            //     {
            //         this.VisibilityByHexagonMap.Clear();
            //         if (this.FocusTile != default)
            //             this.DefineLineOfSight(this.SourceTile, this.FocusTile, 10)
            //                 .Each(this.VisibilityByHexagonMap.Add);
            //     }
            // }
        }

        public void Unfocus()
        {
            this.FocusTile = default;
            this.ResetMovementOverlay();
        }

        public IDictionary<Hexagon, bool> DetermineFogOfWar(Hexagon sourceTile, int viewDistance)
        {
            return this.Map.Values.ToDictionary(tile => tile, tile =>
                this.DefineLineOfSight(sourceTile, tile, viewDistance).Last().Visible);
        }

        public void ResetVisibility()
        {
            this.FogOfWarMap = null;
        }

        public void ApplyVisibility(IDictionary<Hexagon, bool> fogOfWar)
        {
            this.FogOfWarMap = fogOfWar;
        }

        public void ResetMovementOverlay()
        {
            this.MovementOverlayMap = null;
        }

        public void ApplyMovementOverlay(Hexagon source, Hexagon target, int distance)
        {
            var movementOverlay = this.DefineLineOfMovement(source, target, distance);
            this.MovementOverlayMap = movementOverlay.ToDictionary(x => x.Tile, x => x.Accessible);
        }

        #endregion

        #region Helper Methods

        protected Cube ToTileCoordinates(Vector2 position)
        {
            var invertedPosition = position.Transform(this.RenderRotationMatrix.Invert());
            var (mx, my) = invertedPosition - this.TileSize / 2;

            // no idea what this is or why this works but without it the coordinates are off
            mx += my / SCREEN_TO_HEX_MAGIC_OFFSET_NUMBER;

            var q = (Math.Sqrt(3) / 3.0 * mx - 1.0 / 3.0 * my) / this.HexagonSizeAdjusted.X;
            var r = (2.0 / 3.0 * my) / this.HexagonSizeAdjusted.Y;
            return Cube.Round(q, (-q - r), r);
        }

        protected Hexagon GetNeighbor(Hexagon hexagon, Direction direction)
        {
            return this.Map.GetOrDefault(hexagon.Cube.Neighbor(direction));
        }

        protected Direction? GetDirection(Hexagon source, Hexagon target)
        {
            foreach (var direction in DIRECTIONS)
                if (source.Cube.Neighbor(direction) == target.Cube)
                    return direction;
            return default;
        }

        protected void Rotate(int degrees)
        {
            var radians = (float) (degrees * Math.PI / 180);
            this.Rotate(radians);
        }

        protected void Rotate(float radians)
        {
            this.Rotation += radians;
            this.Rotation %= (float) (360 * Math.PI / 180);

            this.RecalculateRotations();
            this.OnRotate?.Invoke();
        }

        protected void RecalculateRotations()
        {
            this.TileRotationMatrix = Matrix.CreateRotationZ(this.Rotation);

            // separate rotations in intervals of 60 degrees, with the intervals shifted by (30n+1) degrees
            var baseDegrees = (int) (this.Rotation * 180 / Math.PI);
            var degrees = baseDegrees.Modulo(360);
            this.WraparoundRotationInterval = degrees switch
            {
                < 32 or >= 332 => 0,
                < 92 => 1,
                < 152 => 2,
                < 212 => 3,
                < 272 => 4,
                < 332 => 5
            };
            var rotationOffset = (float) (this.WraparoundRotationInterval * 60 * Math.PI / 180);

            this.WraparoundRotation = (this.Rotation % (float) (360 * Math.PI / 180)) - rotationOffset;
            this.WraparoundRotationMatrix = Matrix.CreateRotationZ(this.WraparoundRotation);
        }

        protected void RecalculateTileBorders()
        {
            this.BorderMap = this.Map.Values
                .ToDictionary(tile => tile, tile => DIRECTIONS
                    .Select(direction => (direction, Type: DetermineBorderType(tile, direction)))
                    .Where(border => (border.Type != BorderType.None))
                    .ToArray());

            BorderType DetermineBorderType(Hexagon tile, Direction direction)
            {
                var neighbor = this.GetNeighbor(tile, direction);
                if (neighbor == null)
                    return BorderType.Edge;
                if (neighbor.Elevation >= tile.Elevation)
                    return BorderType.None;
                if ((tile.SlopeMask & direction) == direction)
                    return BorderType.Slope;
                if (tile.Elevation - neighbor.Elevation == 1)
                    return BorderType.Small;
                return BorderType.Large;
            }
        }

        protected Vector3 Lerp(Cube a, Cube b, float t) =>
            this.Lerp(a.ToVector3(), b.ToVector3(), t);

        protected Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(this.Lerp(a.X, b.X, t), this.Lerp(a.Y, b.Y, t), this.Lerp(a.Z, b.Z, t));

        protected float Lerp(float a, float b, float t) =>
            a + (b - a) * t;

        protected IEnumerable<(Hexagon Tile, bool Visible)> DefineLineOfSight(Hexagon source, Hexagon target, int viewDistance)
        {
            if (source == target)
                return (source, true).Yield();

            // A point can be exactly between two cubes, so both sides should be checked
            //  point + EPSILON  will be called Add
            //  point - EPSILON  will be called Sub
            var highestAddTileElevation = int.MinValue;
            var highestSubTileElevation = int.MinValue;
            var addTileBeforeElevationLowered = source;
            var subTileBeforeElevationLowered = source;

            var distance = (int) Cube.Distance(source.Cube, target.Cube);
            return Generate.Range(1, distance + 1).Select(distanceStep =>
            {
                // Use linear interpolation to determine which tiles are on the line
                var lerp = this.Lerp(source.Cube, target.Cube, 1f / distance * distanceStep);
                var addTile = this.Map[(lerp + EPSILON).ToRoundCube()];
                var subTile = this.Map[(lerp - EPSILON).ToRoundCube()];

                // If the tile is too far away, it is not visible
                if (distanceStep >= viewDistance)
                    return (addTile, false);

                // Keep track of tiles before lowered elevation
                if (addTile.Elevation >= addTileBeforeElevationLowered.Elevation)
                    addTileBeforeElevationLowered = addTile;
                if (subTile.Elevation >= subTileBeforeElevationLowered.Elevation)
                    subTileBeforeElevationLowered = subTile;
                // TODO support the following scenario: >1 difference in elevation that lies far away hides 1 tile
                // i.e.     o]]] x [[x]] - x

                // If a previous tile is higher than current, it is obstructed
                if (highestAddTileElevation > addTile.Elevation)
                    return (addTile, false);
                if (highestSubTileElevation > subTile.Elevation)
                    return (subTile, false);

                // If not obstructed, tiles are visible if they have the same elevation
                var addTileElevationDifference = addTile.Elevation - source.Elevation;
                if (addTileElevationDifference == 0)
                    return (addTile, true);
                var subTileElevationDifference = subTile.Elevation - source.Elevation;
                if (subTileElevationDifference == 0)
                    return (subTile, true);

                // When looking from below to above:
                // Difference in elevation determines amount of tiles away from elevation for edge tile visibility.
                // Anything beyond edge tile is not visible unless also higher, in which case same rule applies.
                //  i.e.    o[x - -     o[[- -      o x[[x -    o x x[[[x -     o[x[x[x -   o[x-[x-
                if (addTileElevationDifference > 0)
                {
                    if (addTileElevationDifference <= distanceStep)
                    {
                        if (highestAddTileElevation == addTile.Elevation)
                            return (addTile, false);
                        highestAddTileElevation = addTile.Elevation;
                        return (addTile, true);
                    }
                    highestAddTileElevation = addTile.Elevation;
                }
                if (subTileElevationDifference > 0)
                {
                    if (subTileElevationDifference <= distanceStep)
                    {
                        if (highestSubTileElevation == subTile.Elevation)
                            return (subTile, false);
                        highestSubTileElevation = subTile.Elevation;
                        return (subTile, true);
                    }
                    highestSubTileElevation = subTile.Elevation;
                }

                // When looking from above to below:
                // Distance from tile to edge tile equals distance from edge until lower tiles become visible.
                //  i.e.    o ] x x x x x x     o x ] - x x x x     o x x ] - - x x     o x x x ] - - -
                if (addTileElevationDifference < 0)
                {
                    var distanceSourceToEdge = (int) Cube.Distance(source.Cube, addTileBeforeElevationLowered.Cube);
                    var distanceEdgeToTarget = (int) Cube.Distance(addTile.Cube, addTileBeforeElevationLowered.Cube);
                    return (addTile, (distanceSourceToEdge < distanceEdgeToTarget));
                }
                if (subTileElevationDifference < 0)
                {
                    var distanceSourceToEdge = (int) Cube.Distance(source.Cube, subTileBeforeElevationLowered.Cube);
                    var distanceEdgeToTarget = (int) Cube.Distance(subTile.Cube, subTileBeforeElevationLowered.Cube);
                    return (subTile, (distanceSourceToEdge < distanceEdgeToTarget));
                }

                // If no other condition is met, it means the target tile is higher than source,
                // and the difference in elevation is greater than distance from source to target.
                // Therefore the target is not visible.
                return (addTile, false);
            });
        }

        protected IEnumerable<(Hexagon Tile, bool Accessible)> DefinePath(Hexagon source, Hexagon target, int maxDistance)
        {
            if (source == target)
                return (source, true).Yield();
                
            return null;
        }

        protected IEnumerable<(Hexagon Tile, bool Accessible)> DefineLineOfMovement(Hexagon source, Hexagon target, int moveDistance)
        {
            if (source == target)
                return (source, true).Yield();

            // A point can be exactly between two cubes, so both sides should be checked
            //  point + EPSILON  will be called Add
            //  point - EPSILON  will be called Sub
            var previousAddTile = source;
            var previousSubTile = source;

            var blocked = false;

            var distance = (int) Cube.Distance(source.Cube, target.Cube);
            return Generate.Range(0, distance + 1).Select(distanceStep =>
            {
                // Use linear interpolation to determine which tiles are on the line
                var lerp = this.Lerp(source.Cube, target.Cube, 1f / distance * distanceStep);
                var addTile = this.Map[(lerp + EPSILON).ToRoundCube()];
                var subTile = this.Map[(lerp - EPSILON).ToRoundCube()];

                // If the tile is too far away, it is not accessible
                if ((distanceStep >= moveDistance) || blocked)
                    return (addTile, false);

                var addTileDifference = addTile.Elevation - previousAddTile.Elevation;
                var subTileDifference = subTile.Elevation - previousSubTile.Elevation;
                previousAddTile = addTile;
                previousSubTile = subTile;

                // Tile is accessible on same elevation
                if (addTileDifference == 0)
                    return (addTile, true);
                if (subTileDifference == 0)
                    return (subTile, true);
                
                // Tile is accessible if going down one level of elevation
                if ((addTile.Elevation < previousAddTile.Elevation) && (addTileDifference == 1))
                    return (addTile, true);
                if ((subTile.Elevation < previousSubTile.Elevation) && (subTileDifference == 1))
                    return (subTile, true);

                // Tile is accessible if there is a slope
                var directionAdd = this.GetDirection(previousAddTile, addTile);
                if (directionAdd.HasValue && ((previousAddTile.SlopeMask & directionAdd) == directionAdd))
                    return (addTile, true);
                var directionSub = this.GetDirection(previousSubTile, subTile);
                if (directionSub.HasValue && ((previousSubTile.SlopeMask & directionSub) == directionSub))
                    return (subTile, true);
                
                // If no other conditions are met, tile is not accessible
                blocked = true;
                return (addTile, false);
            });
        }

        // not really sure what center is useful for
        protected Cube FindCenterCube()
        {
            var (minX, minY, minZ, maxX, maxY, maxZ) = this.Map.Values
                .Select(tile => tile.Cube)
                .Aggregate((MinX: int.MaxValue, MinY: int.MaxValue, MinZ: int.MaxValue,
                            MaxX: int.MinValue, MaxY: int.MinValue, MaxZ: int.MaxValue),
                    (t, cube) => (Math.Min(t.MinX, cube.X), Math.Min(t.MinY, cube.Y), Math.Min(t.MinZ, cube.Z),
                        Math.Max(t.MaxX, cube.X), Math.Max(t.MaxY, cube.Y), Math.Max(t.MaxZ, cube.Z)));
            return Cube.Round((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        }

        /// <summary> Calculates min and max positions of all tiles, then returns (max - min). </summary>
        protected Vector2 CalculateTilesCombinedSize()
        {
            var (width, height) = this.TileSize.ToPoint();
            var (minX, maxX, minY, maxY) = this.Map.Values
                .Select(tile => tile.Position)
                .Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                    (aggregate, vector) => (
                        Math.Min(aggregate.MinX, (int) vector.X),
                        Math.Max(aggregate.MaxX, (int) vector.X + width),
                        Math.Min(aggregate.MinY, (int) vector.Y),
                        Math.Max(aggregate.MaxY, (int) vector.Y + height)));
            return new Vector2(maxX - minX, maxY - minY);
        }

        protected (Texture2D Texture, bool Flip) GetDirectionalBorderTexture(Direction direction, BorderType borderType)
        {
            if (this.HexagonBorderTextureRange.Length != this.HexagonBorderLargeTextureRange.Length)
                throw new InvalidOperationException("If this happened, it's time to refactor.");

            var directionIndex = direction switch
            {
                Direction.UpRight => 0,
                Direction.Right => 1,
                Direction.DownRight => 2,
                Direction.DownLeft => 3,
                Direction.Left => 4,
                Direction.UpLeft => 5,
                _ => throw direction.Invalid()
            };
            var index = (directionIndex + this.WraparoundRotationInterval) % this.HexagonBorderTextureRange.Length;
            var flip = index < this.HexagonBorderTextureRange.Length / 2;
            return borderType switch
            {
                BorderType.Small => (this.HexagonBorderTextureRange[index], flip),
                BorderType.Large => (this.HexagonBorderLargeTextureRange[index], flip),
                _ => throw borderType.Invalid()
            };
        }

        #endregion
    }
}