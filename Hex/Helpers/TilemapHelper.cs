using Extended.Collections;
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
using System.Linq;

namespace Hex.Helpers
{
    public class TilemapHelper : IUpdate<NormalUpdate>, IDraw<BackgroundDraw>
    {
        #region Constants

        // no idea what these divisions are (possibly to account for hexagon borders sharing pixels?)
        // without them there are small gaps or overlap between hexagons, especially as coordinates increase
        // note that these are specific to 25/29 (pointy) and 29/25 (flatty) sizes!
        private const double SHORT_OVERLAP_DIVISOR = 1.80425; // this seems to be the offset for odd rows(pointy)/cols(flat)
        private const double LONG_OVERLAP_DIVISOR = 2.07137; // but this one no idea, doesn't seem to match any offset

        // no idea why this works, but without it mouse to hexagon conversion is off and gets worse as it moves further from origin
        private const int SCREEN_TO_HEX_OFFSET = 169;

        /// <summary> An error-margin that can be used to always push Lerp operations in the same direction when a point is exactly between two points. </summary>
        private static readonly Vector3 EPSILON = new Vector3(0.000001f, 0.000002f, -0.000003f);

        #endregion

        #region Constructors

        public TilemapHelper(Vector2 size, int padding, InputHelper input, CameraHelper camera)
        {
            this.TilemapSize = size;
            this.TilemapPadding = new Vector2(padding);
            this.Input = input;
            this.Camera = camera;
            // this.HexagonMap = new HexagonMap(default, default, default);
        }

        #endregion

        #region Properties

        public Vector2 TilemapSize { get; }
        public Vector2 TilemapPadding { get; }

        public HexagonMap HexagonMap { get; protected set; }

        public Hexagon CursorHexagon { get; protected set; }
        public Hexagon SourceHexagon { get; protected set; }

        // TODO should this be public?
        /// <summary>
        /// Hexagon rotation is supported in intervals of 30 degrees from 0° to 330°. Rotating to 360° will reset the rotation to 0°, which means there are 12 possible orientations.
        /// <br/> Even-numbered rotations use pointy-top hexagonal shapes, odd-numbered rotations use flatty-top shapes.
        /// </summary>
        public Cycle<int> Orientation { get; } = new Cycle<int>(Generate.Range(12).ToArray());

        // TODO find better way to share this (camera needs it, maybe have camera be child of tilemaphelper)
        public Vector2 MapSize =>
            Vector2.Max(this.TilemapSize, (this.GridSizes[this.Orientation] + this.TilemapPadding))
                .IfOddAddOne();

        protected InputHelper Input { get; }
        protected CameraHelper Camera { get; }

        protected Vector2 TilemapOrigin { get; set; }

        protected Hexagon OriginHexagon { get; set; }
        protected Hexagon CenterHexagon { get; set; }
        protected Hexagon LastCursorHexagon { get; set; }
        protected Hexagon LastSourceHexagon { get; set; }

        protected bool CalculatedVisibility { get; set; }
        protected IDictionary<Hexagon, bool> VisibilityByHexagonMap { get; } = new Dictionary<Hexagon, bool>();

        protected Vector2[] GridSizes { get; set; }

        protected Texture2D HexOuterPointyTop { get; set; }
        protected Texture2D HexInnerPointyTop { get; set; }
        protected Texture2D HexOuterFlattyTop { get; set; }
        protected Texture2D HexInnerFlattyTop { get; set; }
        protected Texture2D HexBorderPointyTop { get; set; }
        protected Texture2D HexBorderFlattyTop { get; set; }

        protected Vector2 HexagonPointySize { get; set; }
        protected Vector2 HexagonFlattySize { get; set; }
        protected (double X, double Y) HexagonPointySizeAdjusted { get; set; }
        protected (double X, double Y) HexagonFlattySizeAdjusted { get; set; }

        protected bool HexagonsArePointy => this.Orientation.Value.IsEven();

        protected Vector2 HexSize => this.HexagonsArePointy ? this.HexagonPointySize : this.HexagonFlattySize;
        protected (double X, double Y) HexSizeAdjusted => this.HexagonsArePointy ? this.HexagonPointySizeAdjusted : this.HexagonFlattySizeAdjusted;
        protected Texture2D HexOuterTexture => this.HexagonsArePointy ? this.HexOuterPointyTop : this.HexOuterFlattyTop;
        protected Texture2D HexInnerTexture => this.HexagonsArePointy ? this.HexInnerPointyTop : this.HexInnerFlattyTop;
        protected Texture2D HexBorderTexture => this.HexagonsArePointy ? this.HexBorderPointyTop : this.HexBorderFlattyTop;

        protected bool PrintCoords { get; set; }

        // TODO these should come from somewhere else
        protected Texture2D BlankTexture { get; set; }
        protected SpriteFont Font { get; set; }

        #endregion

        #region Methods

        // public void Load(DependencyMap dependencyMap)
        public void Load(ContentManager content, Texture2D blankTexture, SpriteFont font)
        {
            this.BlankTexture = blankTexture;
            this.Font = font;

            this.HexOuterPointyTop = content.Load<Texture2D>("xop");
            this.HexInnerPointyTop = content.Load<Texture2D>("xip");
            this.HexOuterFlattyTop = content.Load<Texture2D>("xof");
            this.HexInnerFlattyTop = content.Load<Texture2D>("xif");
            this.HexBorderPointyTop = content.Load<Texture2D>("xbp");
            this.HexBorderFlattyTop = content.Load<Texture2D>("xbf");

            this.HexagonPointySize = new Vector2(this.HexOuterPointyTop.Width, this.HexOuterPointyTop.Height);
            this.HexagonFlattySize = new Vector2(this.HexOuterFlattyTop.Width, this.HexOuterFlattyTop.Height);
            this.HexagonPointySizeAdjusted = (this.HexOuterPointyTop.Width / SHORT_OVERLAP_DIVISOR, this.HexOuterPointyTop.Height / LONG_OVERLAP_DIVISOR);
            this.HexagonFlattySizeAdjusted = (this.HexOuterFlattyTop.Width / LONG_OVERLAP_DIVISOR, this.HexOuterFlattyTop.Height / SHORT_OVERLAP_DIVISOR);

            var random = new Random();
            var n = 14;
            var m = 30;
            var axials = new List<(int Q, int R)>();
            if (false)
            {
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

            var adjustedWidthPointyTop = this.HexOuterPointyTop.Width / SHORT_OVERLAP_DIVISOR;
            var adjustedWidthFlattyTop = this.HexOuterFlattyTop.Width / LONG_OVERLAP_DIVISOR;
            var adjustedHeightPointyTop = this.HexOuterPointyTop.Height / LONG_OVERLAP_DIVISOR;
            var adjustedHeightFlattyTop = this.HexOuterFlattyTop.Height / SHORT_OVERLAP_DIVISOR;
            this.HexagonMap = axials
                .Select(axial =>
                {
                    var cube = Cube.FromAxial(axial.Q, axial.R);
                    return Enumerable
                        .Range(0, 6)
                        .Select(_ =>
                        {
                            var (q, r) = cube.ToAxial();
                            var pointyTopX = Math.Round(adjustedWidthPointyTop * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                            var pointyTopY = Math.Round(adjustedHeightPointyTop * (3.0 / 2.0 * r));
                            var pointyPosition = new Vector2((float) pointyTopX, (float) pointyTopY);

                            var flattyTopX = Math.Round(adjustedWidthFlattyTop * (3.0 / 2.0 * q));
                            var flattyTopY = Math.Round(adjustedHeightFlattyTop * (Math.Sqrt(3) / 2 * q + Math.Sqrt(3) * r));
                            var flattyPosition = new Vector2((float) flattyTopX, (float) flattyTopY);

                            var currentCube = cube;
                            cube = cube.Rotate();
                            return (Cube: currentCube, Pointy: pointyPosition, Flatty: flattyPosition);
                        })
                        .SelectMulti(x => (x.Cube, Position: x.Pointy), x => (x.Cube, Position: x.Flatty))
                        .Into(sequence => new Hexagon(sequence.ToArray()));
                })
                .Into(sequence => new HexagonMap(sequence, 12, () => this.Orientation));

            this.OriginHexagon = this.HexagonMap[default];
            this.OriginHexagon.Color = Color.Gold;
            this.HexagonMap[new Cube(1, -2, 1)]?.Into(hex => hex.Color = Color.Silver);

            var (minX, minY, minZ, maxX, maxY, maxZ) = this.HexagonMap.Values
                .Select(this.GetCube)
                .Aggregate((MinX: int.MaxValue, MinY: int.MaxValue, MinZ: int.MaxValue,
                            MaxX: int.MinValue, MaxY: int.MinValue, MaxZ: int.MaxValue),
                    (t, cube) => (Math.Min(t.MinX, cube.X), Math.Min(t.MinY, cube.Y), Math.Min(t.MinZ, cube.Z),
                        Math.Max(t.MaxX, cube.X), Math.Max(t.MaxY, cube.Y), Math.Max(t.MaxZ, cube.Z)));
            var round = Cube.Round((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
            this.CenterHexagon = this.HexagonMap[round];
            this.CenterHexagon.Color = Color.Aquamarine;

            this.GridSizes = Enumerable
                .Range(0, 12)
                .Select(orientation =>
                {
                    var width = orientation.IsEven() ? this.HexOuterPointyTop.Width : this.HexOuterFlattyTop.Width;
                    var height = orientation.IsEven() ? this.HexOuterPointyTop.Height : this.HexOuterFlattyTop.Height;
                    var (minX, maxX, minY, maxY) = this.HexagonMap.Values
                        .Select(this.GetPosition)
                        .Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                            (aggregate, vector) => (
                                Math.Min(aggregate.MinX, (int) vector.X),
                                Math.Max(aggregate.MaxX, (int) vector.X + width),
                                Math.Min(aggregate.MinY, (int) vector.Y),
                                Math.Max(aggregate.MaxY, (int) vector.Y + height)));
                    return new Vector2(maxX - minX, maxY - minY);
                })
                .ToArray();
        }

        public void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.O))
                this.Center();

            if (this.Input.KeyPressed(Keys.P))
                this.PrintCoords = !this.PrintCoords;

            if (this.Input.MouseMoved())
            {
                var mouseVector = this.Input.CurrentVirtualMouseVector;
                var cameraTranslatedMouseVector = this.Camera.FromScreen(mouseVector);

                if (this.TilemapSize.ToRectangle().Contains(mouseVector))
                {
                    var cubeAtMouse = this.ToCubeCoordinates(cameraTranslatedMouseVector);
                    this.LastCursorHexagon = this.CursorHexagon;
                    this.CursorHexagon = this.HexagonMap[cubeAtMouse];

                    if ((this.CursorHexagon != this.LastCursorHexagon) && (this.SourceHexagon != default))
                    {
                        if (this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
                        {
                            Predicate<Cube> determineIsVisible = x => (this.HexagonMap[x]?.TileType == TileType.Grass);
                            this.VisibilityByHexagonMap.Clear();
                            this.DefineLineVisibility(this.GetCube(this.SourceHexagon), cubeAtMouse, determineIsVisible)
                                .Select(tuple => (Hexagon: this.HexagonMap[tuple.Cube], tuple.Visible))
                                .Where(tuple => (tuple.Hexagon != default))
                                .Each(this.VisibilityByHexagonMap.Add);
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
                Predicate<Cube> determineIsVisible = x => (this.HexagonMap[x]?.TileType == TileType.Grass);
                this.VisibilityByHexagonMap.Clear();
                var sourceCoordinates = this.GetCube(this.SourceHexagon);
                this.HexagonMap.Values
                    .Select(hexagon => (Hexagon: hexagon, IsVisible: this.DeterminePointIsVisibleFrom(this.GetCube(hexagon), sourceCoordinates, determineIsVisible)))
                    .Where(tuple => !this.VisibilityByHexagonMap.ContainsKey(tuple.Hexagon))
                    .Each(this.VisibilityByHexagonMap.Add);
            };

            if (this.Input.MousePressed(MouseButton.Left))
            {
                this.LastSourceHexagon = this.SourceHexagon;
                this.SourceHexagon = (this.SourceHexagon != this.CursorHexagon) ? this.CursorHexagon : default;
                this.CalculatedVisibility = false;
            }

            if (this.VisibilityByHexagonMap.Any() && (this.Input.KeysUp(Keys.LeftAlt, Keys.RightAlt, Keys.LeftShift, Keys.RightShift) || (this.SourceHexagon == default)))
            {
                this.VisibilityByHexagonMap.Clear();
                this.CalculatedVisibility = false;
            }

            if (this.Input.KeyPressed(Keys.Z))
            {
                this.Rotate(advance: true);
                this.Camera.Center();
            }
            if (this.Input.KeyPressed(Keys.X))
            {
                this.Rotate(advance: false);
                this.Camera.Center();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawTo(this.BlankTexture, this.MapSize.ToRectangle(), Color.DarkSlateGray);

            // should flip back to using depth so only 1 loop needed
            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = this.GetCube(hex);
                var position = this.TilemapOrigin + this.GetPosition(hex);
                spriteBatch.DrawAt(this.HexBorderTexture, position, 1f, Color.Sienna);
            }

            // should flip back to using depth so only 1 loop needed
            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = this.GetCube(hex);
                var position = this.TilemapOrigin + this.GetPosition(hex);
                var color = (hex == this.SourceHexagon) ? Color.Coral
                    : (hex == this.CursorHexagon) ? Color.LightYellow
                    : this.VisibilityByHexagonMap.TryGetValue(hex, out var visible) ? (visible ? new Color(205, 235, 185) : new Color(175, 195, 160))
                    : hex.TileType switch
                    {
                        TileType.Mountain => Color.Tan,
                        _ => new Color(190, 230, 160)
                    };

                // TODO calculate border hexagons and only draw for them, note it changes by orientation!
                spriteBatch.DrawAt(this.HexInnerTexture, position, 1f, color);
            }

            // should flip back to using depth so only 1 loop needed
            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = this.GetCube(hex);
                var position = this.TilemapOrigin + this.GetPosition(hex);

                // TODO if mountain tiles are on top of each other it looks bad, calculate
                if (hex.TileType == TileType.Mountain)
                    spriteBatch.DrawAt(this.HexBorderTexture, position - new Vector2(0, 5), 1f, Color.Sienna);
            }

            // should flip back to using depth so only 1 loop needed
            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = this.GetCube(hex);
                var position = this.TilemapOrigin + this.GetPosition(hex);
                spriteBatch.DrawAt(this.HexOuterTexture, position, 1f, hex.TileType switch
                {
                    TileType.Mountain => new Color(130, 100, 60),
                    _ => new Color(100, 140, 70)
                });
            }

            // should flip back to using depth so only 1 loop needed
            if (this.PrintCoords)
            {
                // spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, transformMatrix: this.Camera.TranslationMatrix);
                foreach (var hex in this.HexagonMap.Values)
                {
                    var cube = this.GetCube(hex);
                    var position = this.TilemapOrigin + this.GetPosition(hex);
                    var (q, r) = cube.ToAxial();
                    var hexLog = $"{q},{r}";
                    spriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.MistyRose, scale: 0.5f);
                }
                // spriteBatch.End();
            }
        }

        // shouldnt be publically called like this
        public void Center()
        {
            this.RecenterGrid();
        }

        public (Cube Coordinates, Vector2 Position) Info(Hexagon hexagon) =>
            (this.GetCube(hexagon), this.GetPosition(hexagon));

        #endregion

        #region Helper Methods

        protected Cube GetCube(Hexagon hexagon) =>
            hexagon.Cubes[this.Orientation];

        protected Vector2 GetPosition(Hexagon hexagon) =>
            hexagon.Positions[this.Orientation];

        protected void RecenterGrid()
        {
            // TODO figure out why there is offset
            // // 154 | 143     44 | 24     27 | 4      86 |66      21 |10     23 | 24        
            // //  30 | 52       9 | 22     15 | 16     119|108     25 | 5     27 | 4
            // // 149 | 149     34 | 34     15 | 16     76 |76      16 | 16    23 | 24
            // //  41 | 41      15 | 15     15 | 16     114|114     15 | 15    16 | 16
            // // -5.5| 5.5     -10|10    -11.5|11.5    -10|10     -5.5|5.5    0.5|-0.5
            // // +11 |-11      6.5|-6.5    0.5|-0.5   -5.5|5.5     -10|10  -11.5.|11.5

            // // 142 | 155      23|45       3 |28       65|87        9|22     23 | 24    
            // //  51 | 31       21|10       15|16      107|120       4|26      3 | 20    
            // // 148 | 148      34|34       15|15       76|76       15|15     23 | 23    
            // //  41 | 41       16|16       15|16      113|113      15|15     11 | 11    
            // // 6.5 | -6.5     11|-11    12.5|-12.5    11|-11     6.5|-6.5   0.5|-0.5   
            // // -10 | 10     -5.5|5.5     0.5|-0.5    6.5|-6.5     11|-11    8.5|-8.5   
            // // these offsets are for 20x30 grid only. need to find the math           
            // var offsets = new[]
            // {
            //     new Vector2(-5.5f, 11f),    // A
            //     new Vector2(-10f, 6.5f),    // B
            //     new Vector2(-11.5f, 0.5f),  // C
            //     new Vector2(-10f, -5.5f),   // D
            //     new Vector2(-5.5f, -10f),   // D
            //     new Vector2(0.5f, -11.5f),  // C
            //     new Vector2(6.5f, -10f),    // B
            //     new Vector2(11f, -5.5f),    // A
            //     new Vector2(12.5f, 0.5f),   // E
            //     new Vector2(11f, 6.5f),     // F
            //     new Vector2(6.5f, 11f),     // F
            //     new Vector2(0.5f, 8.5f)     // G
            // };
            var centerHexagonPositionRelativeToOrigin = this.GetPosition(this.CenterHexagon);
            var positionForCenterHexagon = (this.MapSize - this.HexSize) / 2;
            this.TilemapOrigin = Vector2.Floor(positionForCenterHexagon - centerHexagonPositionRelativeToOrigin);
        }

        protected void Rotate(bool advance)
        {
            if (advance)
                this.Orientation.Advance();
            else
                this.Orientation.Reverse();
            this.RecenterGrid();

            // todo fix the CameraHelper.CenterOn method
            // then calculate what is hexagon in center of screen before rotate
            // then after rotate center on that hexagon
            this.Camera.Center();
        }

        protected Cube ToCubeCoordinates(Vector2 position)
        {
            var (mx, my) = position - this.TilemapOrigin - this.HexSize / 2;

            // no idea why this works but without it the coordinates are off
            if (this.HexagonsArePointy)
                mx += my / SCREEN_TO_HEX_OFFSET;
            else
                my += mx / SCREEN_TO_HEX_OFFSET;

            double q, r;
            if (this.HexagonsArePointy)
            {
                q = (Math.Sqrt(3) / 3.0 * mx - 1.0 / 3.0 * my) / this.HexSizeAdjusted.X;
                r = (2.0 / 3.0 * my) / this.HexSizeAdjusted.Y;
            }
            else
            {
                q = (2.0 / 3.0 * mx) / this.HexSizeAdjusted.X;
                r = (-1.0 / 3.0 * mx + Math.Sqrt(3) / 3.0 * my) / this.HexSizeAdjusted.Y;
            }
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

        #endregion
    }
}