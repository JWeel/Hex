using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Models;
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

        private static Direction[] DIRECTIONS { get; } = Enum.GetValues<Direction>();

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
        public Hexagon CursorTile { get; protected set; }

        /// <summary> The selected tile. </summary>
        public Hexagon SourceTile { get; protected set; }

        /// <summary> The combined size of all tiles. </summary>
        public Vector2 TilemapSize { get; protected set; }

        /// <summary> The distance between origin and tilemap that would set the tilemap centered in the bounding box. </summary>
        public Vector2 TilemapOffset { get; protected set; }

        /// <summary> Gets the position at the center of the source tile. </summary>
        public Vector2 SourceTileMiddle
        {
            get
            {
                if (this.SourceTile == null)
                    return Vector2.Zero;
                var position = this.SourceTile.Position + this.TileSize / 2;
                var rotated = position.Transform(this.RotationMatrix);
                return Vector2.Round(rotated);
            }
        }

        /// <summary> A transform matrix that rotates and moves to relative render position. </summary>
        public Matrix RotationMatrix =>
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / -2f - this.RenderPosition, 1)) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / 2f + this.RenderPosition + this.TilemapOffset, 1));

        protected InputHelper Input { get; }
        protected Texture2D BlankTexture { get; }
        protected SpriteFont Font { get; }

        protected float Rotation { get; set; }
        protected Vector2 RenderPosition { get; set; }

        protected Hexagon OriginTile { get; set; }
        protected Hexagon CenterTile { get; set; }
        protected Hexagon LastCursorTile { get; set; }
        protected Hexagon LastSourceTile { get; set; }

        protected IDictionary<Hexagon, bool> FogOfWarMap { get; set; }

        // TODO is this needed
        protected bool CalculatedVisibility { get; set; }
        // TODO is this needed
        protected IDictionary<Hexagon, bool> VisibilityByHexagonMap { get; } = new Dictionary<Hexagon, bool>();

        protected Texture2D HexagonOuterTexture { get; set; }
        protected Texture2D HexagonInnerTexture { get; set; }
        protected Texture2D HexagonBorderPointyTexture { get; set; }
        protected Texture2D HexagonBorderFlattyTexture { get; set; }

        protected Texture2D HexagonBorderDownLeftTexture { get; set; }
        protected Texture2D HexagonBorderLeftTexture { get; set; }
        protected Texture2D HexagonBorderUpLeftTexture { get; set; }

        protected Vector2 TileSize { get; set; }
        protected (double X, double Y) HexagonSizeAdjusted { get; set; }

        protected bool PrintCoords { get; set; }

        #endregion

        #region Methods

        public void Arrange(string path)
        {
            (int Q, int R, int E, TileType T)[] axials;
            if (path.IsNullOrWhiteSpace())
                axials = this.Spawn(8, 12, Shape.Hexagon);
            else
                axials = this.Load(path);

            this.Map = axials
                .Select(axial =>
                {
                    var (q, r, elevation, type) = axial;
                    var cube = Cube.FromAxial(axial.Q, axial.R);
                    var positionX = Math.Round(this.HexagonSizeAdjusted.X * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                    var positionY = Math.Round(this.HexagonSizeAdjusted.Y * (3.0 / 2.0 * r));
                    var position = new Vector2((float) positionX, (float) positionY);
                    return new Hexagon(cube, position, this.TileSize, elevation, type);
                })
                .ToDictionary(x => x.Cube);

            this.Map.GetOrDefault((0, 0))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((-1, 0))?.Into(h => h.Elevation = 2);
            this.Map.GetOrDefault((-2, 0))?.Into(h => h.Elevation = 2);
            this.Map.GetOrDefault((1, 0))?.Into(h => h.Elevation = 2);
            this.Map.GetOrDefault((2, 0))?.Into(h => h.Elevation = 2);
            this.Map.GetOrDefault((-2, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((-1, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((0, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((1, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((2, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((3, -1))?.Into(h => h.Elevation = 3);
            this.Map.GetOrDefault((-1, 2))?.Into(h => h.Elevation = 0);

            // this.OriginTile = this.Map.GetOrDefault(default);
            // this.Map.GetOrDefault((1, 1))?.Into(x => x.Color = Color.Silver);

            // var centerCube = this.FindCenterCube();
            // this.CenterTile = this.Map.GetOrDefault(centerCube);
            // this.SourceTile = this.Map.GetOrDefault((-5, -3));

            if (this.Map.Any())
                this.RenderPosition = this.Map.Values
                    .Select(x => x.Position)
                    .Aggregate((aggregate, position) => Vector2.Min(aggregate, position));

            this.TilemapSize = this.CalculateTilesCombinedSize();
            this.FogOfWarMap = this.Map.Values.ToDictionary(x => x, x => false);
        }

        /// <summary> Generate a new tilemap using specified integers to determine shape and size. </summary>
        // TODO tiletype should also come from here, meaning not in the hexagon ctor
        public (int Q, int R, int E, TileType T)[] Spawn(int n, int m, Shape shape)
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
                    throw new InvalidOperationException($"Invalid shape '{shape}'.");
            }
            // var ran = new Random();
            // Enumerable.Range(0, ran.Next(axials.Count))
            //     .Each(i => axials.RemoveAt(ran.Next(axials.Count)));
            return axials
                .Select(tuple => (tuple.Q, tuple.R, 1, ToTileType(tuple.Q, tuple.R)))
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

        public (int Q, int R, int E, TileType T)[] Load(string path)
        {
            var random = new Random();
            return File.ReadAllLines(path)
                .Where(line => !line.IsNullOrWhiteSpace())
                .Select(line =>
                {
                    var identifierSplit = line.Split(":");
                    var axialSplit = identifierSplit[0].Split(", ");
                    var q = int.Parse(axialSplit[0]);
                    var r = int.Parse(axialSplit[1]);
                    var t = Enum.Parse<TileType>(identifierSplit[1].Trim());
                    // if (random.Next(100) < 40) t = TileType.Mountain;
                    return (q, r, 1, t);
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
                var sourceCube = this.SourceTile.Cube;
                this.Map.Values
                    // .Where(hexagon => !this.VisibilityByHexagonMap.ContainsKey(hexagon))
                    .Select(hexagon => (Hexagon: hexagon, IsVisible: this.DeterminePointIsVisibleFrom(sourceCube, hexagon.Cube, this.IsVisible)))
                    .Each(this.VisibilityByHexagonMap.Add);
            };

            if (this.Input.MousePressed(MouseButton.Left))
            {
                this.LastSourceTile = this.SourceTile;
                this.SourceTile = (this.SourceTile != this.CursorTile) ? this.CursorTile : default;
                if (this.SourceTile != null)
                    this.DetermineFogOfWar();
                else
                    this.FogOfWarMap.Keys.Each(key => this.FogOfWarMap[key] = false);
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

            if (this.Input.KeyPressed(Keys.V))
                this.Rotation = 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var tile in this.Map.Values)
            {
                var cube = tile.Cube;
                var basePosition = tile.Position;
                var position = basePosition.Transform(this.RotationMatrix);

                var innerColor = ((tile == this.CursorTile) && (tile == this.SourceTile)) ? new Color(255, 170, 130)
                    : (tile == this.CursorTile) ? Color.LightYellow
                    : (tile == this.SourceTile) ? Color.Coral
                    : (tile == this.OriginTile) ? Color.Gold
                    : (tile == this.CenterTile) ? Color.Aquamarine
                    : tile.Color != default ? tile.Color
                    : tile.TileType switch
                    {
                        // TileType.Mountain => Color.Tan,
                        TileType.Sea => new Color(100, 200, 220, 80),
                        _ => new Color(190, 230, 160)
                    };
                spriteBatch.DrawAt(this.HexagonInnerTexture, position, innerColor, this.Rotation, depth: .15f);

                if (this.VisibilityByHexagonMap.TryGetValue(tile, out var visibility))
                {
                    var visiblityOverlayColor = visibility ?
                        new Color(255, 255, 255, 80) :
                        new Color(0, 0, 0, 50);
                    // new Color(205, 235, 185, 100) :
                    // new Color(175, 195, 160, 100);
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, visiblityOverlayColor, this.Rotation, depth: .175f);
                }

                // separate rotations in intervals of 60 degrees, with the intervals shifted by (30n+1) degrees
                var baseDegrees = (int) (this.Rotation * 180 / Math.PI);
                var degrees = baseDegrees.Modulo(360);
                var rotationInterval = degrees switch
                {
                    < 31 => 0,
                    < 91 => 1,
                    < 151 => 2,
                    < 211 => 3,
                    < 271 => 4,
                    < 331 => 5,
                    _ => 0
                };

                // convert this to radians to get rotation offset to subtract from tilemap rotation
                var rotationOffset = (float) (rotationInterval * 60 * Math.PI / 180);
                var borderRotation = (this.Rotation % (float) (360 * Math.PI / 180)) - rotationOffset;
                var borderRotationMatrix = Matrix.CreateRotationZ(borderRotation);

                // offset center of hexagon to preserve rotational origin for overlay sprites
                var borderBasePosition = (basePosition + this.TileSize / 2).Transform(this.RotationMatrix);
                var borderTextureOffset = (this.TileSize / 2).Transform(borderRotationMatrix);
                var borderPosition = borderBasePosition - borderTextureOffset;
                spriteBatch.DrawAt(this.HexagonBorderPointyTexture, borderPosition, Color.Sienna, borderRotation, depth: .075f);

                if (tile.TileType == TileType.Mountain)
                {
                    // var innerBorderPosition1 = borderPosition - new Vector2(0, 5).Transform(borderRotationMatrix);
                    // var innerBorderPosition2 = borderPosition - new Vector2(0, 9).Transform(borderRotationMatrix);
                    // var innerBorderPosition3 = borderPosition - new Vector2(0, 13).Transform(borderRotationMatrix);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition1, Color.Sienna, borderRotation, depth: .2f);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition2, Color.Sienna, borderRotation, depth: .21f);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition3, Color.Sienna, borderRotation, depth: .22f);

                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, position - new Vector2(0, 5).Transform(Matrix.CreateRotationZ(this.Rotation)), Color.Sienna, this.Rotation, depth: .2f);
                }


                // TODO:
                // upleft and upright should be just single line (width one)
                // length doesnt increase on higher elevation (but maybe color gets darker?)
                // left and right are vertical lines with length two, doubling for each elevation (up to 4x?)
                // downleft and downright are diagonal with length 3(?), doubling for each elevation (up to 4x?)

                // visibility should be based on elevation:
                // when looking from above to below:
                //  distance from tile to tile where elevation change starts
                //  equals distance until lower tile elevation tiles become visible
                //  i.e.    o ] x x x x x x     o x ] - x x x x     o x x ] - - x x     o x x x ] - - -
                // when looking from below to above:
                //  difference in elevation determines amount of tiles away from elevation for edge tile to be visible
                //  i.e.    o [ x - -     o [[- -      o x [[x -    o x x[[[x -
                foreach (var direction in DIRECTIONS)
                {
                    var neighbor = this.GetNeighbor(tile, direction);
                    if ((neighbor == null) || (tile.Elevation <= neighbor.Elevation))
                        continue;

                    // if ((direction == Direction.UpLeft) || (direction == Direction.UpRight))
                        // continue;

                    var texture = direction switch
                    {
                        Direction.Left => this.HexagonBorderLeftTexture,
                        Direction.Right => this.HexagonBorderLeftTexture,
                        Direction.DownLeft => this.HexagonBorderDownLeftTexture,
                        Direction.DownRight => this.HexagonBorderDownLeftTexture,
                        Direction.UpLeft => this.HexagonBorderUpLeftTexture,
                        Direction.UpRight => this.HexagonBorderUpLeftTexture,
                        _ => this.BlankTexture
                    };
                    var effects = direction switch
                    {
                        Direction.Right => SpriteEffects.FlipHorizontally,
                        Direction.DownRight => SpriteEffects.FlipHorizontally,
                        Direction.UpRight => SpriteEffects.FlipHorizontally,
                        _ => default
                    };
                    var offset = direction switch
                    {
                        Direction.UpLeft => new Vector2(0, 0),
                        Direction.UpRight => new Vector2(0, 0),
                        Direction.Left => new Vector2(0, 0),
                        Direction.Right => new Vector2(0, 0),
                        Direction.DownLeft => new Vector2(0, 3),
                        Direction.DownRight => new Vector2(0, 3),
                        _ => Vector2.Zero
                    };
                    var elevationSteps = (neighbor == null) ? tile.Elevation : tile.Elevation - neighbor.Elevation;
                    for (int i = 1; i < elevationSteps + 1; i++)
                    {
                        var directionPosition = position - (offset * i).Transform(Matrix.CreateRotationZ(this.Rotation));
                        spriteBatch.DrawAt(texture, directionPosition, Color.Sienna, this.Rotation, depth: .3f, effects: effects);
                    }
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
                    var hexLog = $"{q},{r}";
                    spriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.MistyRose, scale: 0.5f, .9f);
                }

                if (this.SourceTile != null)
                {
                    // if (!this.VisibilityByHexagonMap.Any())
                    if (!this.FogOfWarMap[tile])
                        spriteBatch.DrawAt(this.HexagonInnerTexture, position, new Color(100, 100, 100, 128), this.Rotation, depth: .3f);
                }
            }
        }

        public Cube ToTileCoordinates(Vector2 position)
        {
            var invertedPosition = position.Transform(this.RotationMatrix.Invert());
            var (mx, my) = invertedPosition - this.TileSize / 2;

            // no idea what this is or why this works but without it the coordinates are off
            mx += my / SCREEN_TO_HEX_MAGIC_OFFSET_NUMBER;

            var q = (Math.Sqrt(3) / 3.0 * mx - 1.0 / 3.0 * my) / this.HexagonSizeAdjusted.X;
            var r = (2.0 / 3.0 * my) / this.HexagonSizeAdjusted.Y;
            return Cube.Round(q, (-q - r), r);
        }

        public void TrackTiles(Vector2 position)
        {
            var coordinates = this.ToTileCoordinates(position);
            this.TrackTiles(coordinates);
        }

        public void TrackTiles(Cube coordinates)
        {
            this.LastCursorTile = this.CursorTile;
            this.CursorTile = this.Map.GetOrDefault(coordinates);

            if ((this.CursorTile != this.LastCursorTile) && (this.SourceTile != default))
            {
                if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                {
                    this.VisibilityByHexagonMap.Clear();
                    this.DefineLineVisibility(this.SourceTile.Cube, coordinates, this.IsVisible)
                        .Select(tuple => (Hexagon: this.Map.GetOrDefault(tuple.Cube), tuple.Visible))
                        .Where(tuple => (tuple.Hexagon != default))
                        .Each(this.VisibilityByHexagonMap.Add);
                }
            }
        }

        public void UntrackTiles()
        {
            this.CursorTile = default;
        }

        #endregion

        #region Helper Methods

        protected Hexagon GetNeighbor(Hexagon hexagon, Direction direction)
        {
            var cube = hexagon.Cube;
            switch (direction)
            {
                case Direction.UpRight:
                    return this.Map.GetOrDefault((cube.X + 1, cube.Y, cube.Z - 1));
                case Direction.Right:
                    return this.Map.GetOrDefault((cube.X + 1, cube.Y - 1, cube.Z));
                case Direction.DownRight:
                    return this.Map.GetOrDefault((cube.X, cube.Y - 1, cube.Z + 1));
                case Direction.DownLeft:
                    return this.Map.GetOrDefault((cube.X - 1, cube.Y, cube.Z + 1));
                case Direction.Left:
                    return this.Map.GetOrDefault((cube.X - 1, cube.Y + 1, cube.Z));
                case Direction.UpLeft:
                    return this.Map.GetOrDefault((cube.X, cube.Y + 1, cube.Z - 1));
                default:
                    throw new ArgumentException("Invalid enum value.", nameof(direction));
            }
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
            this.OnRotate?.Invoke();
        }

        protected Vector3 Lerp(Cube a, Cube b, float t) =>
            this.Lerp(a.ToVector3(), b.ToVector3(), t);

        protected Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(this.Lerp(a.X, b.X, t), this.Lerp(a.Y, b.Y, t), this.Lerp(a.Z, b.Z, t));

        protected float Lerp(float a, float b, float t) =>
            a + (b - a) * t;

        protected IEnumerable<(Cube Cube, bool Visible)> DefineLineVisibility(Cube start, Cube end, Func<Cube, Cube, bool> determineIsVisible)
        {
            var restIsStillVisible = true;
            var totalDistance = (int) Cube.Distance(start, end);
            var lastCube = start;
            return Generate.Range(totalDistance + 1) // +1 to include end
                .Select(stepDistance =>
                {
                    var lerp = this.Lerp(start, end, 1f / totalDistance * stepDistance);
                    var cubePositive = (lerp + EPSILON).ToRoundCube();
                    if (!restIsStillVisible)
                        return (cubePositive, Visible: false);
                    if (determineIsVisible(lastCube, cubePositive))
                    {
                        lastCube = cubePositive;
                        return (cubePositive, Visible: restIsStillVisible);
                    }
                    var cubeNegative = (lerp - EPSILON).ToRoundCube();
                    if ((cubePositive != cubeNegative) && determineIsVisible(lastCube, cubeNegative))
                    {
                        lastCube = cubeNegative;
                        return (cubeNegative, Visible: restIsStillVisible);
                    }
                    else
                        restIsStillVisible = false;
                    return (cubePositive, Visible: restIsStillVisible);
                });
        }

        protected bool DeterminePointIsVisibleFrom(Cube from, Cube target, Func<Cube, Cube, bool> determineIsVisible)
        {
            var stillVisible = true;
            var totalDistance = (int) Cube.Distance(target, from);
            var lastCube = from;
            Generate.Range(totalDistance + 1)
                .TakeWhile(_ => stillVisible)
                .Each(stepDistance =>
                {
                    var lerp = this.Lerp(from, target, 1f / totalDistance * stepDistance);
                    var cubePositive = (lerp + EPSILON).ToRoundCube();
                    if (determineIsVisible(lastCube, cubePositive))
                    {
                        lastCube = cubePositive;
                        stillVisible = true;
                        return;
                    }
                    var cubeNegative = (lerp - EPSILON).ToRoundCube();
                    if ((cubePositive != cubeNegative) && determineIsVisible(lastCube, cubeNegative))
                    {
                        lastCube = cubeNegative;
                        stillVisible = true;
                        return;
                    }
                    stillVisible = false;
                });
            return stillVisible;
        }

        protected void DetermineFogOfWar()
        {
            var viewDistance = 9;
            this.Map.Values.Each(hex => this.FogOfWarMap[hex] = InView(hex));
            bool InView(Hexagon hexagon)
            {
                if (hexagon == this.SourceTile)
                    return true;
                var targetCube = hexagon.Cube;
                var sourceCube = this.SourceTile.Cube;
                var distance = Cube.Distance(targetCube, sourceCube);
                var withinView = (distance < viewDistance);
                if (!withinView)
                    return false;
                return this.DeterminePointIsVisibleFrom(sourceCube, targetCube, this.IsVisible);
            }
        }

        // not really sure what center is useful for
        protected Cube FindCenterCube()
        {
            var (minX, minY, minZ, maxX, maxY, maxZ) = this.Map.Values
                .Select(hex => hex.Cube)
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
                .Select(hex => hex.Position)
                .Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                    (aggregate, vector) => (
                        Math.Min(aggregate.MinX, (int) vector.X),
                        Math.Max(aggregate.MaxX, (int) vector.X + width),
                        Math.Min(aggregate.MinY, (int) vector.Y),
                        Math.Max(aggregate.MaxY, (int) vector.Y + height)));
            return new Vector2(maxX - minX, maxY - minY);
        }

        protected bool IsVisible(Cube source, Cube target)
        {
            var sourceTile = this.Map.GetOrDefault(source);
            if (sourceTile == default)
                return false;
            var targetTile = this.Map.GetOrDefault(target);
            if (targetTile == default)
                return false;
            if (targetTile.Elevation <= sourceTile.Elevation)
                return true;
            // Z axis is vertical, if target.Z > source.Z then target is further down than source
            return (target.Z > source.Z);
        }

        #endregion
    }
}