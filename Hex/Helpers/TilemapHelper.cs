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

        /// <summary> An error-margin that can be used to always push Lerp operations in the same direction when a point is exactly between two points. </summary>
        private static readonly Vector3 EPSILON = new Vector3(0.000001f, 0.000002f, -0.000003f);

        #endregion

        #region Constructors

        public TilemapHelper(ClientWindow client, InputHelper input, ContentManager content, Texture2D blankTexture, SpriteFont font)
        {
            this.Camera = new CameraHelper(() => this.TrueSize, () => this.ContainerSize, () => this.Rotation, input, client);

            this.Input = input;
            this.BlankTexture = blankTexture;
            this.Font = font;

            this.HexagonOuterTexture = content.Load<Texture2D>("xop");
            this.HexagonInnerTexture = content.Load<Texture2D>("xip");
            this.HexagonBorderPointyTexture = content.Load<Texture2D>("xbp");
            this.HexagonBorderFlattyTexture = content.Load<Texture2D>("xbf");

            this.HexagonSize = new Vector2(this.HexagonOuterTexture.Width, this.HexagonOuterTexture.Height);
            this.HexagonSizeAdjusted = (this.HexagonOuterTexture.Width / SHORT_OVERLAP_DIVISOR, this.HexagonOuterTexture.Height / LONG_OVERLAP_DIVISOR);

            this.HexagonMap = new Dictionary<Cube, Hexagon>();
        }

        #endregion

        #region Properties

        public IDictionary<Cube, Hexagon> HexagonMap { get; protected set; }
        public Hexagon CursorHexagon { get; protected set; }
        public Hexagon SourceHexagon { get; protected set; }

        // TODO find better way to share this (camera needs it, maybe have camera be child of tilemaphelper)
        public Vector2 TrueSize => this.BaseBoundingBoxSize;
        // Vector2.Max(this.ContainerSize, this.TransformedPaddedTilemapSize).IfOddAddOne();

        public Vector2 BaseTilemapSize { get; protected set; }

        // needed for hexagon-shaped map, where (0,0,0) is not at top left but in middle -> probably more scenarios!!
        // alternatively make a hexagon shape where (0,0,0) is not middle?
        protected Vector2 CenterOffset =>
            (this.BaseTilemapSize / 2 - this.OriginHexagon.Position + this.HexagonSize / 2);
        // (this.OriginHexagon == this.CenterHexagon) ? (this.BaseTilemapSize - this.HexagonSize) / 2 : Vector2.Zero;

        // not accurate!
        // Y is too big in parallelograms
        public Vector2 TransformedPaddedTilemapSize => new Vector2(
            (float) (Math.Abs((this.BaseTilemapSize.X + this.ContainerPadding.X) * Math.Cos(this.Rotation)) +
                    Math.Abs((this.BaseTilemapSize.Y + this.ContainerPadding.Y) * Math.Sin(this.Rotation))),
            (float) (Math.Abs((this.BaseTilemapSize.X + this.ContainerPadding.X) * Math.Sin(this.Rotation)) +
                    Math.Abs((this.BaseTilemapSize.Y + this.ContainerPadding.Y) * Math.Cos(this.Rotation))));

        protected InputHelper Input { get; }
        protected CameraHelper Camera { get; }

        protected Vector2 ContainerSize { get; set; }
        protected Vector2 ContainerPadding { get; set; }

        protected Vector2 TilemapOrigin { get; set; }

        protected Hexagon OriginHexagon { get; set; }
        protected Hexagon CenterHexagon { get; set; }
        protected Hexagon RotationHexagon { get; set; }
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

        // TODO these should come from somewhere else
        protected Texture2D BlankTexture { get; set; }
        protected SpriteFont Font { get; set; }

        public float Rotation { get; set; }

        #endregion

        #region Methods

        public void Arrange(Vector2 containerSize, int containerPadding)
        {
            this.ContainerSize = containerSize;
            this.ContainerPadding = new Vector2(containerPadding);

            // TODO:
            // Load preset tilemaps
            var axials = this.Spawn(13, 40);

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

            this.BaseTilemapSize = this.CalculateHexagonsCombinedSize();

            var (w, h) = this.BaseTilemapSize.ToPoint();
            var diagonal = (float) Math.Sqrt(Math.Pow(w, 2) + Math.Pow(h, 2));
            this.BaseBoundingBoxSize = new Vector2(diagonal);


            this.FogOfWarMap = this.HexagonMap.Values.ToDictionary(x => x, x => false);
            this.Center();
        }
        public Vector2 BaseBoundingBoxSize { get; protected set; }
        public Vector2 PaddedBoundingBoxSize => this.BaseBoundingBoxSize + this.ContainerPadding;

        /// <summary> Generate a new tilemap using specified integers to determine shape and size. </summary>
        // TODO tiletype should also come from here, meaning not in the hexagon ctor
        public (int Q, int R)[] Spawn(int n, int m)
        {
            var axials = new List<(int Q, int R)>();
            if (false)
            {
                // triangle
                for (int q = 0; q <= n; q++)
                {
                    for (int r = 0; r <= n - q; r++)
                    {
                        axials.Add((q, r));
                    }
                }
            }
            else if (false)
            {
                // parallelogram
                for (int q = 0; q <= n; q++)
                {
                    for (int r = 0; r <= m; r++)
                    {
                        axials.Add((q, r));
                    }
                }
            }
            else if (false)
            {
                // hexagon
                for (var q = -n; q <= n; q++)
                {
                    // var color = new Color(random.Next(256), random.Next(256), random.Next(256));
                    var color = Color.White;
                    var r1 = Math.Max(-n, -q - n);
                    var r2 = Math.Min(n, -q + n);
                    for (var r = r1; r <= r2; r++)
                    {
                        // not sure why q and r are flipped here
                        axials.Add((r, q));
                    }
                }
            }
            else
            {
                // rectangle
                for (var r = 0; r < m; r++)
                {
                    var color = Color.White;
                    var r_offset = (int) Math.Floor(r / 2f);
                    for (var q = -r_offset; q < n - r_offset; q++)
                    {
                        // TODO square board with odd rows having 1 less
                        axials.Add((q, r));
                    }
                }
            }
            return axials.ToArray();
        }

        public void Load(string path)
        {
        }

        public void Update(GameTime gameTime)
        {
            this.Camera.Update(gameTime);

            if (this.Input.KeyPressed(Keys.O))
                this.Center();

            if (this.Input.KeyPressed(Keys.P))
                this.PrintCoords = !this.PrintCoords;

            if (this.Input.MouseMoved())
            {
                var mouseVector = this.Input.CurrentVirtualMouseVector;
                // mouseVector = new Vector2(mouseVector.X, 500);
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
                                .Select(tuple => (Hexagon: this.HexagonMap[tuple.Cube], tuple.Visible))
                                .Where(tuple => (tuple.Hexagon != default))
                                .Each(this.VisibilityByHexagonMap.Add);
                            bool IsVisible(Cube cube) => (this.HexagonMap.GetOrDefault(cube)?.TileType == TileType.Grass);
                        }
                    }
                }
                else if (this.CursorHexagon != default)
                {
                    this.CursorHexagon = default;
                }
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

            if (this.Input.KeyPressed(Keys.Z) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                this.Rotate(advance: true, fixedStep: true);
            else if (this.Input.KeyDown(Keys.Z) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                this.Rotate(advance: true, fixedStep: false);

            if (this.Input.KeyPressed(Keys.X) && !this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                this.Rotate(advance: false, fixedStep: true);
            else if (this.Input.KeyDown(Keys.X) && this.Input.KeysDownAny(Keys.LeftShift, Keys.RightShift))
                this.Rotate(advance: false, fixedStep: false);

            if (this.Input.KeyPressed(Keys.V))
            {
                this.Rotation = 0f;
                this.Center();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawTo(this.BlankTexture, this.TrueSize.ToRectangle(), Color.DarkSlateGray, .05f);

            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = hex.Cube;
                var basePosition = hex.Position;
                var position = this.TilemapOrigin + this.Transform(basePosition);

                spriteBatch.DrawAt(this.HexagonBorderPointyTexture, position, Color.Sienna, this.Rotation, depth: .075f);

                var color = ((hex == this.CursorHexagon) && (hex == this.SourceHexagon)) ? new Color(255, 170, 130)
                    : (hex == this.CursorHexagon) ? Color.LightYellow
                    : (hex == this.SourceHexagon) ? Color.Coral
                    : (hex == this.RotationHexagon) ? Color.OliveDrab
                    : this.VisibilityByHexagonMap.TryGetValue(hex, out var visible) ? (visible ? new Color(205, 235, 185) : new Color(175, 195, 160))
                    : hex.Color != default ? hex.Color
                    : hex.TileType switch
                    {
                        TileType.Mountain => Color.Tan,
                        _ => new Color(190, 230, 160)
                    };

                spriteBatch.DrawAt(this.HexagonInnerTexture, position, color, this.Rotation, depth: .15f);

                // TODO if mountain tiles are on top of each other it looks bad, calculate
                // TODO calculate border hexagons and only draw for them, note it changes by orientation!
                if (hex.TileType == TileType.Mountain)
                {
                    // var degrees = Math.Round(this.Rotation * 180 / Math.PI);
                    // var modulo = degrees % 60;
                    // var borderTexture = modulo == 0 ? this.HexagonBorderPointyTexture : this.HexagonBorderFlattyTexture;

                    var borderTexture = this.HexagonBorderPointyTexture;
                    var borderRotation = this.Rotation;

                    var innerBorderPosition1 = this.TilemapOrigin + this.Transform(basePosition - new Vector2(0, 5));
                    // var innerBorderPosition2 = this.TilemapOrigin + this.Transform(basePosition - new Vector2(0, 9));
                    // var innerBorderPosition3 = this.TilemapOrigin + this.Transform(basePosition - new Vector2(0, 13));

                    spriteBatch.DrawAt(borderTexture, innerBorderPosition1, Color.Sienna, borderRotation, depth: .2f);
                    // spriteBatch.DrawAt(this.HexBorderTexture, innerBorderPosition2, Color.Sienna, this.Rotation, depth: .21f);
                    // spriteBatch.DrawAt(this.HexBorderTexture, innerBorderPosition3, Color.Sienna, this.Rotation, depth: .22f);
                }

                color = hex.TileType switch
                {
                    TileType.Mountain => new Color(130, 100, 60),
                    _ => new Color(100, 140, 70)
                };
                spriteBatch.DrawAt(this.HexagonOuterTexture, position, color, this.Rotation, depth: .25f);

                if (this.PrintCoords)
                {
                    var (q, r) = cube.ToAxial();
                    var hexLog = $"{q},{r}";
                    spriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.MistyRose, scale: 0.5f, .9f);
                }
            }

            if (this.SourceHexagon != null)
            {
                foreach (var pair in this.FogOfWarMap)
                {
                    var (hex, visible) = pair;
                    if (visible)
                        continue;
                    var position = this.TilemapOrigin + this.Transform(hex.Position);
                    spriteBatch.DrawAt(this.HexagonInnerTexture, position, new Color(100, 100, 100, 128), this.Rotation, depth: .3f);
                }
            }

            // if (this.CenterHexagon != null)
            // {
            //     var position = this.CenterHexagon.Position;
            //     var scaledViewportCenter = this.ContainerSize / this.Camera.ZoomScaleFactor / 2f;
            //     spriteBatch.DrawText(this.Font, position.ToString(), position);
            //     spriteBatch.DrawText(this.Font, scaledViewportCenter.ToString(), scaledViewportCenter);
            //     spriteBatch.DrawAt(this.BlankTexture, this.TilemapOrigin + position, 3f, Color.Maroon);
            //     spriteBatch.DrawAt(this.BlankTexture, this.TilemapOrigin + position + this.HexSize/2, 3f, Color.Navy);
            // }

            var middle = this.BaseTilemapSize / 2 - this.CenterOffset + this.TilemapOrigin;

            spriteBatch.DrawAt(this.BlankTexture, middle - new Vector2(3), Color.DarkOrange, scale: 7f, depth: .9f);
        }

        // shouldnt be publically called like this
        public void Center()
        {
            this.RecenterGrid();
            this.Camera.Center();
        }

        public (Cube Coordinates, Vector2 Position) Info(Hexagon hexagon) =>
            (hexagon.Cube, hexagon.Position);

        public Vector2 Translate(Vector2 vector) =>
            this.Camera.FromScreen(vector);

        public Matrix TranslationMatrix =>
            this.Camera.TranslationMatrix;

        #endregion

        #region Helper Methods

        protected void RecenterGrid()
        {
            // given arbitrary tilemap shape
            // given arbitrary origin, calculate difference between origin and middle
            // given max bounding box,


            // bounding box to be calculated in Arrange
            // BaseBoundingBox
            // PaddedBoundingBox

            this.TilemapOrigin = Vector2.Round(this.BaseBoundingBoxSize / 2 - this.CenterOffset);


            // this.TilemapOrigin = Vector2.Round((this.TrueSize - this.BaseTilemapSize) / 2) + this.CenterOffset;
        }

        protected void Rotate(bool advance, bool fixedStep)
        {
            var rotationOriginVector = this.Translate(this.ContainerSize / 2);
            var cubeAtRotationOrigin = this.ToCubeCoordinates(rotationOriginVector);
            // this.RotationHexagon = this.HexagonMap[cubeAtRotationOrigin];

            var degreeIncrement = fixedStep ? 30 : 3;

            if (advance)
                this.Rotation += (float) (degreeIncrement * Math.PI / 180);
            else
                this.Rotation -= (float) (degreeIncrement * Math.PI / 180);

            this.Rotation %= (float) (360 * Math.PI / 180);

            // for camera:
            // calculate difference between camera position and middle of tilemap
            // now do transform by translating -that distance, rotate, +that distance

            // this.RecenterGrid();
            // this.Camera.CenterOn(this.Transform(this.Camera.Position));
            // this.Camera.Center();
            return;

            // this does not need to be a property
            if (this.RotationHexagon != null)
                this.Camera.CenterOn(this.RotationHexagon.Position, this.CenterHexagon.Position);
            else
                // center on is not ideal, it requires relative hexagon positioning to work
                // this means that when no rotation origin hexagon is found, camera position is off
                // calling Clamp here is a bandaid: it ensures camera does not go outside tilemap
                // true solution requires:
                // 1. center on position, not relative hexagon
                // 2. increasing the 'viewable' tilemap size to be larger -> each tilemap edge + half container size
                this.Camera.Clamp();
        }

        protected Cube ToCubeCoordinates(Vector2 position)
        {
            var (mx, my) = this.TransformInvert(position - this.TilemapOrigin) - this.HexagonSize / 2;

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
            var viewDistance = 10;
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

        protected Matrix TilemapTransform =>
                Matrix.CreateTranslation(new Vector3(this.BaseTilemapSize / -2f, 1)) *
                Matrix.CreateRotationZ(this.Rotation) *
                Matrix.CreateTranslation(new Vector3(this.BaseTilemapSize / 2f, 1));

        protected Vector2 Transform(Vector2 vector) =>
            Vector2.Transform(vector, this.TilemapTransform);

        protected Vector2 TransformInvert(Vector2 vector) =>
            Vector2.Transform(vector, Matrix.Invert(this.TilemapTransform));

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