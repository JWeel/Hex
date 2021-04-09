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
using Mogi.Framework;
using Mogi.Helpers;
using Mogi.Inversion;
using System;
using System.Collections.Generic;
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

        #endregion

        #region Constructors

        public TilemapHelper(InputHelper input, CameraHelper camera, ContentManager content, Texture2D blankTexture, SpriteFont font)
        {
            this.Camera = camera;
            this.Input = input;
            this.BlankTexture = blankTexture;
            this.Font = font;

            this.HexagonOuterTexture = content.Load<Texture2D>("xop");
            this.HexagonInnerTexture = content.Load<Texture2D>("xip");
            this.HexagonBorderPointyTexture = content.Load<Texture2D>("xbp");
            this.HexagonBorderFlattyTexture = content.Load<Texture2D>("xbf");

            this.HexagonSize = this.HexagonOuterTexture.ToVector();
            this.HexagonSizeAdjusted = (this.HexagonSize.X / SHORT_OVERLAP_DIVISOR, this.HexagonSize.Y / LONG_OVERLAP_DIVISOR);

            this.HexagonMap = new Dictionary<Cube, Hexagon>();
        }

        #endregion

        #region Properties

        /// <summary> The mapping of all tiles by cube-coordinates. </summary>
        public IDictionary<Cube, Hexagon> HexagonMap { get; protected set; }

        /// <summary> The tile over which the cursor is hovering. </summary>
        public Hexagon CursorHexagon { get; protected set; }

        /// <summary> The selected tile. </summary>
        public Hexagon SourceHexagon { get; protected set; }

        /// <summary> The size of the widget, control, or component that contains this tilemap. </summary>
        public Vector2 ContainerSize { get; protected set; }

        /// <summary> The combined size of all tiles. </summary>
        public Vector2 TilemapSize { get; protected set; }

        /// <summary> The size of the bounding box that can fully contains the tilemap in any rotation. </summary>
        public Vector2 BoundingBoxSize { get; protected set; }

        /// <summary> The bounds of the camera. This is the max of <see cref="BoundingBoxSize"/> and <see cref="ContainerSize"/>. </summary>
        public Vector2 CameraBounds { get; protected set; }

        /// <summary> The distance between origin and tilemap that would set the tilemap centered in the bounding box. </summary>
        public Vector2 TilemapOffset { get; protected set; }

        protected InputHelper Input { get; }
        // move to other helper
        protected CameraHelper Camera { get; }

        protected Vector2 RenderPosition { get; set; }
        public float Rotation { get; set; }

        protected Hexagon OriginHexagon { get; set; }
        protected Hexagon CenterHexagon { get; set; }
        protected Hexagon LastCursorHexagon { get; set; }
        protected Hexagon LastSourceHexagon { get; set; }

        protected bool CalculatedVisibility { get; set; }
        protected IDictionary<Hexagon, bool> VisibilityByHexagonMap { get; } = new Dictionary<Hexagon, bool>();
        protected IDictionary<Hexagon, bool> FogOfWarMap { get; set; }

        protected Texture2D HexagonOuterTexture { get; set; }
        protected Texture2D HexagonInnerTexture { get; set; }
        protected Texture2D HexagonBorderPointyTexture { get; set; }
        protected Texture2D HexagonBorderFlattyTexture { get; set; }
        protected Vector2 HexagonSize { get; set; }
        protected (double X, double Y) HexagonSizeAdjusted { get; set; }

        protected bool PrintCoords { get; set; }

        protected Texture2D BlankTexture { get; set; }
        protected SpriteFont Font { get; set; }

        public Matrix TilemapRotationMatrix =>
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / -2f - this.RenderPosition, 1)) *
            Matrix.CreateRotationZ(this.Rotation) *
            Matrix.CreateTranslation(new Vector3(this.TilemapSize / 2f + this.RenderPosition + this.TilemapOffset, 1));

        #endregion

        #region Methods

        public void Arrange(Vector2 containerSize)
        {
            this.ContainerSize = containerSize;
            // this.ContainerSize = new Vector2(containerSize.X / 1.3f, containerSize.Y / 1.1f);

            // TODO:
            // Load preset tilemaps
            var axials = this.Spawn(4, 8);

            this.HexagonMap = axials
                .Select(axial =>
                {
                    var cube = Cube.FromAxial(axial.Q, axial.R);
                    var (q, r) = cube.ToAxial();
                    var positionX = Math.Round(this.HexagonSizeAdjusted.X * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                    var positionY = Math.Round(this.HexagonSizeAdjusted.Y * (3.0 / 2.0 * r));
                    var position = new Vector2((float) positionX, (float) positionY);
                    return new Hexagon(cube, position);
                })
                .ToDictionary(x => x.Cube);

            this.OriginHexagon = this.HexagonMap.GetOrDefault(default);
            if (this.OriginHexagon != null)
                this.OriginHexagon.Color = Color.Gold;
            this.HexagonMap.GetOrDefault(new Cube(1, -2, 1))?.Into(x => x.Color = Color.Silver);

            var centerCube = this.FindCenterCube();
            this.CenterHexagon = this.HexagonMap.GetOrDefault(centerCube);
            if (this.CenterHexagon != null)
                this.CenterHexagon.Color = Color.Aquamarine;

            if (this.HexagonMap.Any())
                this.RenderPosition = this.HexagonMap.Values
                    .Select(x => x.Position)
                    .Aggregate((a, v) => Vector2.Min(a, v));

            this.TilemapSize = this.CalculateHexagonsCombinedSize();

            // boundingbox should be all 4 corners of the bounding rectangle (usually diagonal of tilemapsize)
            // plus padding for when that corner is the center of rotation (half of containersize on each side)
            // below formula gives slightly more than necessary (might be hexagonsize?), but will do for now
            this.BoundingBoxSize = new Vector2(this.TilemapSize.Length()) + this.ContainerSize;

            this.CameraBounds = Vector2.Max(this.BoundingBoxSize, this.ContainerSize);
            this.Camera.Center();

            // requires RenderPosition, TilemapSize, and CameraBounds (maybe pass these as args to the method)
            this.TilemapOffset = this.CalculateOffset();

            this.FogOfWarMap = this.HexagonMap.Values.ToDictionary(x => x, x => false);
        }

        /// <summary> Generate a new tilemap using specified integers to determine shape and size. </summary>
        // TODO tiletype should also come from here, meaning not in the hexagon ctor
        public (int Q, int R)[] Spawn(int n, int m)
        {
            var shape = Shape.Hexagon;
            // var shape = Shape.Rectangle;
            // var shape = Shape.Triangle;
            // var shape = Shape.Parallelogram;
            // var shape = Shape.Line;
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
            return axials.ToArray();
        }

        public void Load(string path)
        {
        }

        public void Update(GameTime gameTime)
        {
            this.Camera.Update(gameTime);

            if (this.Input.KeyPressed(Keys.C))
                this.Camera.Center();

            if (this.Input.KeyPressed(Keys.H))
            {
                if (null != this.SourceHexagon)
                {
                    var pos = this.SourceHexagon.Position + this.HexagonSize / 2;
                    var transPos = pos.Transform(this.TilemapRotationMatrix);
                    var newPos = Vector2.Round(transPos);
                    this.Camera.CenterOn(newPos);
                }
            }

            if (this.Input.KeyPressed(Keys.P))
                this.PrintCoords = !this.PrintCoords;

            if (this.Input.MouseMoved())
            {
                var mouseVector = this.Input.CurrentVirtualMouseVector;
                var cameraTranslatedMouseVector = this.Camera.FromScreen(mouseVector);

                if (this.ContainerSize.ToRectangle().Contains(mouseVector))
                {
                    var cubeAtMouse = this.ToCubeCoordinates(cameraTranslatedMouseVector);
                    this.LastCursorHexagon = this.CursorHexagon;
                    this.CursorHexagon = this.HexagonMap.GetOrDefault(cubeAtMouse);

                    if ((this.CursorHexagon != this.LastCursorHexagon) && (this.SourceHexagon != default))
                    {
                        if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                        {
                            this.VisibilityByHexagonMap.Clear();
                            this.DefineLineVisibility(this.SourceHexagon.Cube, cubeAtMouse, IsVisible)
                                .Select(tuple => (Hexagon: this.HexagonMap.GetOrDefault(tuple.Cube), tuple.Visible))
                                .Where(tuple => (tuple.Hexagon != default))
                                .Each(this.VisibilityByHexagonMap.Add);
                            bool IsVisible(Cube cube) => (this.HexagonMap.GetOrDefault(cube)?.TileType == TileType.Grass);
                        }
                    }
                }
                // clear after leaving container
                else if (this.CursorHexagon != default)
                    this.CursorHexagon = default;
            }

            if (!this.CalculatedVisibility && (this.SourceHexagon != default) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
            {
                this.CalculatedVisibility = true;
                this.VisibilityByHexagonMap.Clear();
                var sourceCoordinates = this.SourceHexagon.Cube;
                this.HexagonMap.Values
                    .Select(hexagon => (Hexagon: hexagon, IsVisible: this.DeterminePointIsVisibleFrom(hexagon.Cube, sourceCoordinates, IsVisible)))
                    .Where(tuple => !this.VisibilityByHexagonMap.ContainsKey(tuple.Hexagon))
                    .Each(this.VisibilityByHexagonMap.Add);
                bool IsVisible(Cube cube) => (this.HexagonMap.GetOrDefault(cube)?.TileType == TileType.Grass);
            };

            if (this.Input.MousePressed(MouseButton.Left))
            {
                this.LastSourceHexagon = this.SourceHexagon;
                this.SourceHexagon = (this.SourceHexagon != this.CursorHexagon) ? this.CursorHexagon : default;
                if (this.SourceHexagon != null)
                    this.DetermineFogOfWar();
                else
                    this.FogOfWarMap.Keys.Each(key => this.FogOfWarMap[key] = false);
                this.CalculatedVisibility = false;
            }

            if (this.VisibilityByHexagonMap.Any() && (this.Input.KeysUp(Keys.LeftAlt, Keys.RightAlt, Keys.LeftShift, Keys.RightShift) || (this.SourceHexagon == default)))
            {
                this.VisibilityByHexagonMap.Clear();
                this.CalculatedVisibility = false;
            }

            if (this.Input.KeyPressed(Keys.Z) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                this.Rotate(degrees: -30);
            else if (!this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeyDown(Keys.Z) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: -3);
                else if (this.Input.KeyDown(Keys.Z) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: -1);

            if (this.Input.KeyPressed(Keys.X) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                this.Rotate(degrees: 30);
            else if (!this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                if (this.Input.KeyDown(Keys.X) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: 3);
                else if (this.Input.KeyDown(Keys.X) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                    this.Rotate(degrees: 1);

            if (this.Input.KeyPressed(Keys.V))
            {
                // subtract current rotation to reset it to 0
                this.Rotate(-this.Rotation);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // this draws a background color in exactly only the viewport. small rounding error so add tiny offset
            var roundingOffset = new Vector2(2) * this.Camera.ZoomScaleFactor;
            var cameraCorner = this.Camera.Position - this.ContainerSize / 2 / this.Camera.ZoomScaleFactor - roundingOffset;
            var cameraBoxSize = this.ContainerSize / this.Camera.ZoomScaleFactor + roundingOffset * 2;
            var cameraBox = new Rectangle(cameraCorner.ToPoint(), cameraBoxSize.ToPoint());
            spriteBatch.DrawTo(this.BlankTexture, cameraBox, new Color(20, 60, 80), .05f);

            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = hex.Cube;
                var basePosition = hex.Position;
                var position = basePosition.Transform(this.TilemapRotationMatrix);

                var innerColor = ((hex == this.CursorHexagon) && (hex == this.SourceHexagon)) ? new Color(255, 170, 130)
                    : (hex == this.CursorHexagon) ? Color.LightYellow
                    : (hex == this.SourceHexagon) ? Color.Coral
                    : this.VisibilityByHexagonMap.TryGetValue(hex, out var visible) ? (visible ? new Color(205, 235, 185) : new Color(175, 195, 160))
                    : hex.Color != default ? hex.Color
                    : hex.TileType switch
                    {
                        TileType.Mountain => Color.Tan,
                        TileType.Sea => new Color(100, 200, 220, 80),
                        _ => new Color(190, 230, 160)
                    };
                spriteBatch.DrawAt(this.HexagonInnerTexture, position, innerColor, this.Rotation, depth: .15f);


                // separate rotations in intervals of 60 degrees, with the intervals shifted by 30 degrees
                var baseDegrees = (int) (this.Rotation * 180 / Math.PI);
                var degrees = baseDegrees.Modulo(360);
                var rotationInterval = degrees switch
                {
                    var x when (x < 30) => 0,
                    var x when (x < 90) => 1,
                    var x when (x < 150) => 2,
                    var x when (x < 210) => 3,
                    var x when (x < 270) => 4,
                    var x when (x < 330) => 5,
                    _ => 0
                };

                // convert this to radians to get rotation offset to subtract from tilemap rotation
                var rotationOffset = (float) (rotationInterval * 60 * Math.PI / 180);
                var borderRotation = (this.Rotation % (float) (360 * Math.PI / 180)) - rotationOffset;
                var borderRotationMatrix = Matrix.CreateRotationZ(borderRotation);

                // offset center of hexagon to preserve rotational origin for overlay sprites
                var borderBasePosition = (basePosition + this.HexagonSize / 2).Transform(this.TilemapRotationMatrix);
                var borderTextureOffset = (this.HexagonSize / 2).Transform(borderRotationMatrix);
                var borderPosition = borderBasePosition - borderTextureOffset;
                spriteBatch.DrawAt(this.HexagonBorderPointyTexture, borderPosition, Color.Sienna, borderRotation, depth: .075f);

                if (hex.TileType == TileType.Mountain)
                {
                    var innerBorderPosition1 = borderPosition - new Vector2(0, 5).Transform(borderRotationMatrix);
                    var innerBorderPosition2 = borderPosition - new Vector2(0, 9).Transform(borderRotationMatrix);
                    var innerBorderPosition3 = borderPosition - new Vector2(0, 13).Transform(borderRotationMatrix);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition1, Color.Sienna, borderRotation, depth: .2f);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition2, Color.Sienna, borderRotation, depth: .21f);
                    // spriteBatch.DrawAt(this.HexagonBorderPointyTexture, innerBorderPosition3, Color.Sienna, borderRotation, depth: .22f);
                }

                innerColor = hex.TileType switch
                {
                    TileType.Mountain => new Color(130, 100, 60),
                    _ => new Color(100, 140, 70)
                };
                spriteBatch.DrawAt(this.HexagonOuterTexture, position, innerColor, this.Rotation, depth: .25f);

                if (this.PrintCoords)
                {
                    var (q, r) = cube.ToAxial();
                    var hexLog = $"{q},{r}";
                    spriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.MistyRose, scale: 0.5f, .9f);
                }

                if (this.SourceHexagon != null)
                {
                    if (!this.FogOfWarMap[hex])
                        spriteBatch.DrawAt(this.HexagonInnerTexture, position, new Color(100, 100, 100, 128), this.Rotation, depth: .3f);
                }
            }
        }

        public (Cube Coordinates, Vector2 Position) Info(Hexagon hexagon) =>
            (hexagon.Cube, hexagon.Position);

        public Vector2 Translate(Vector2 vector) =>
            this.Camera.FromScreen(vector);

        #endregion

        #region Helper Methods

        protected Vector2 CalculateOffset()
        {
            // Get distance from top left (renderposition) to tilemap middle
            var relativeMiddle = this.TilemapSize / 2 + this.RenderPosition;
            // Subtract this distance from true middle to get offset for centered tilemap rendering
            return Vector2.Round(this.CameraBounds / 2 - relativeMiddle);
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

            // TODO setting to toggle this behavior on/off
            if (this.SourceHexagon != null)
            {
                var pos = this.SourceHexagon.Position + this.HexagonSize / 2;
                var transPos = pos.Transform(this.TilemapRotationMatrix);
                var newPos = Vector2.Round(transPos);
                this.Camera.CenterOn(newPos);
            }
        }

        protected Cube ToCubeCoordinates(Vector2 position)
        {
            var invertedPosition = position.Transform(this.TilemapRotationMatrix.Invert());

            var (mx, my) = invertedPosition - this.HexagonSize / 2;

            // no idea what this is or why this works but without it the coordinates are off
            mx += my / SCREEN_TO_HEX_MAGIC_OFFSET_NUMBER;

            var q = (Math.Sqrt(3) / 3.0 * mx - 1.0 / 3.0 * my) / this.HexagonSizeAdjusted.X;
            var r = (2.0 / 3.0 * my) / this.HexagonSizeAdjusted.Y;

            return Cube.Round(q, (-q - r), r);
        }

        protected Vector3 Lerp(Cube a, Cube b, float t) =>
            this.Lerp(a.ToVector3(), b.ToVector3(), t);

        protected Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(this.Lerp(a.X, b.X, t), this.Lerp(a.Y, b.Y, t), this.Lerp(a.Z, b.Z, t));

        protected float Lerp(float a, float b, float t) =>
            a + (b - a) * t;

        protected IEnumerable<(Cube Cube, bool Visible)> DefineLineVisibility(Cube start, Cube end, Predicate<Cube> determineIsVisible)
        {
            var restIsStillVisible = true;
            var totalDistance = (int) Cube.Distance(start, end);
            return Generate.RangeDescending(totalDistance - 1) // -1 will exclude start tile from visibility check
                .Select(stepDistance =>
                {
                    var lerp = this.Lerp(end, start, 1f / totalDistance * stepDistance);
                    var cubePositive = (lerp + EPSILON).ToRoundCube();
                    if (!restIsStillVisible)
                        return (cubePositive, Visible: false);

                    var cubeNegative = (lerp - EPSILON).ToRoundCube();
                    if (cubePositive == cubeNegative)
                    {
                        if (!determineIsVisible(cubePositive))
                            restIsStillVisible = false;
                        return (cubePositive, Visible: restIsStillVisible);
                    }

                    var positiveIsVisible = determineIsVisible(cubePositive);
                    var negativeIsVisible = determineIsVisible(cubeNegative);
                    if (!positiveIsVisible && !negativeIsVisible)
                        restIsStillVisible = false;
                    else if (!positiveIsVisible)
                        return (cubeNegative, Visible: true);
                    else if (!negativeIsVisible)
                        return (cubePositive, Visible: true);
                    return (cubePositive, Visible: restIsStillVisible);
                });
        }

        protected bool DeterminePointIsVisibleFrom(Cube target, Cube from, Predicate<Cube> determineIsVisible)
        {
            var stillVisible = true;
            var totalDistance = (int) Cube.Distance(target, from);
            Generate.Range(totalDistance)
                .TakeWhile(_ => stillVisible)
                .Each(stepDistance =>
                {
                    var lerp = this.Lerp(target, from, 1f / totalDistance * stepDistance);
                    var cubePositive = (lerp + EPSILON).ToRoundCube();
                    var cubeNegative = (lerp - EPSILON).ToRoundCube();
                    stillVisible = (determineIsVisible(cubePositive) || determineIsVisible(cubeNegative));
                });
            return stillVisible;
        }

        protected void DetermineFogOfWar()
        {
            var viewDistance = 9;
            this.HexagonMap.Values.Each(hex => this.FogOfWarMap[hex] = InView(hex));
            bool InView(Hexagon hexagon)
            {
                if (hexagon == this.SourceHexagon)
                    return true;
                var targetCube = hexagon.Cube;
                var sourceCube = this.SourceHexagon.Cube;
                var distance = Cube.Distance(targetCube, sourceCube);
                var withinView = (distance < viewDistance);
                if (!withinView)
                    return false;
                return this.DeterminePointIsVisibleFrom(targetCube, sourceCube, IsVisible);
            }
            bool IsVisible(Cube cube) => (this.HexagonMap.GetOrDefault(cube)?.TileType == TileType.Grass);
        }

        // not really sure what center is useful for
        protected Cube FindCenterCube()
        {
            var (minX, minY, minZ, maxX, maxY, maxZ) = this.HexagonMap.Values
                .Select(hex => hex.Cube)
                .Aggregate((MinX: int.MaxValue, MinY: int.MaxValue, MinZ: int.MaxValue,
                            MaxX: int.MinValue, MaxY: int.MinValue, MaxZ: int.MaxValue),
                    (t, cube) => (Math.Min(t.MinX, cube.X), Math.Min(t.MinY, cube.Y), Math.Min(t.MinZ, cube.Z),
                        Math.Max(t.MaxX, cube.X), Math.Max(t.MaxY, cube.Y), Math.Max(t.MaxZ, cube.Z)));
            return Cube.Round((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        }

        protected Vector2 CalculateHexagonsCombinedSize()
        {
            var (width, height) = this.HexagonSize.ToPoint();
            var (minX, maxX, minY, maxY) = this.HexagonMap.Values
                .Select(hex => hex.Position)
                .Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                    (aggregate, vector) => (
                        Math.Min(aggregate.MinX, (int) vector.X),
                        Math.Max(aggregate.MaxX, (int) vector.X + width),
                        Math.Min(aggregate.MinY, (int) vector.Y),
                        Math.Max(aggregate.MaxY, (int) vector.Y + height)));
            return new Vector2(maxX - minX, maxY - minY);
        }

        #endregion
    }
}