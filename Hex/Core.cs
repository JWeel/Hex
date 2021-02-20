using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Helpers;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex
{
    public class Core : Game
    {
        #region Constants

        private const string CONTENT_ROOT_DIRECTORY = "Content";
        private const float BASE_GLOBAL_SCALE = 1.5f;
        private const float MAX_GLOBAL_SCALE = 5f;
        private const float MIN_GLOBAL_SCALE = 0.25f;
        public const int BASE_WINDOW_WIDTH = 1280;
        public const int BASE_WINDOW_WIDTH_INCREMENT = BASE_WINDOW_WIDTH / 8; // used for keyboard-based scaling
        public const int BASE_WINDOW_WIDTH_MIN = BASE_WINDOW_WIDTH / 4; // minimum for keyboard-based scaling (not for mouse)
        public const int BASE_WINDOW_WIDTH_MAX = BASE_WINDOW_WIDTH * 2; // maximum for keyboard-based scaling (not for mouse)
        public const int BASE_WINDOW_HEIGHT = 720;
        public const int BASE_MAP_PANEL_WIDTH = 790; // 1280 / 1.618 = 791.10 : use 790 for even number
        public const int BASE_MAP_PANEL_HEIGHT = BASE_WINDOW_HEIGHT;
        public const int BASE_SIDE_PANEL_WIDTH = BASE_WINDOW_WIDTH - BASE_MAP_PANEL_WIDTH;
        public const int BASE_SIDE_PANEL_HEIGHT = 445; // 720 / 1.618 = 444.99
        private const float BASE_ASPECT_RATIO = BASE_WINDOW_WIDTH / (float) BASE_WINDOW_HEIGHT;
        private const float GOLDEN_RATIO = 1.618f;

        private static readonly Vector2 BASE_MAP_PANEL_SIZE = new Vector2(BASE_MAP_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT);
        private const int BASE_MAP_PADDING = 30;

        // no idea what these divisions are (possibly to account for hexagon borders sharing pixels?)
        // without them there are small gaps or overlap between hexagons, especially as coordinates increase
        private const double SHORT_OVERLAP_DIVISOR = 1.8045; // this seems to be the offset for odd rows(pointy)/cols(flat)
        private const double LONG_OVERLAP_DIVISOR = 2.072; // but this one no idea, doesn't seem to match any offset

        #endregion

        #region Constructors

        public Core()
        {
            this.Graphics = new GraphicsDeviceManager(this);
        }

        #endregion

        #region Data Members

        protected event Action<ContentManager> OnLoad;
        protected event Action<GameTime> OnUpdate;
        protected event Action<SpriteBatch> OnDrawMap;
        protected event Action<SpriteBatch> OnDrawPanel;

        protected GraphicsDeviceManager Graphics { get; set; }
        protected SpriteBatch SpriteBatch { get; set; }

        protected FramerateHelper Framerate { get; set; }
        protected InputHelper Input { get; set; }
        protected CameraHelper Camera { get; set; }

        protected SpriteFont Font { get; set; }
        protected List<Hexagon> Hexagons { get; set; } = new List<Hexagon>();
        protected Vector2 GridCenter { get; set; }
        protected Texture2D HexOuterPointyTop { get; set; }
        protected Texture2D HexInnerPointyTop { get; set; }
        protected Texture2D HexOuterFlattyTop { get; set; }
        protected Texture2D HexInnerFlattyTop { get; set; }
        protected Texture2D BlankTexture { get; set; }
        protected Troolean PointyTop { get; set; }

        protected RenderTarget2D WindowScalingRenderTarget { get; set; }
        protected Vector2 ClientSizeTranslation { get; set; }
        protected Vector2 AspectRatio { get; set; }
        protected Vector2 ScaledWindowSize { get; set; }
        protected Vector2 ScaledMapPanelSize { get; set; }
        protected Rectangle ScaledMapPanelRectangle { get; set; }

        /// <summary> Mouse position relative to window. </summary>
        protected Vector2 BaseMouseVector { get; set; }
        /// <summary> Client size translation is needed for fullscreen mode (in windowed mode ClientBounds == BackBuffer size). </summary>
        protected Vector2 ClientSizeTranslatedMouseVector { get; set; }
        /// <summary> camera translation is needed when camera is zoomed in. </summary>
        protected Vector2 CameraTranslatedMouseVector { get; set; }

        protected Hexagon CursorHexagon { get; set; }

        protected Vector2 MapSizePointyTop { get; set; }
        protected Vector2 MapSizeFlattyTop { get; set; }
        protected Vector2 MapOriginPointyTop { get; set; }
        protected Vector2 MapOriginFlattyTop { get; set; }

        protected Vector2 MapSize =>
            Vector2.Max(BASE_MAP_PANEL_SIZE,
                (this.PointyTop ? this.MapSizePointyTop : this.MapSizeFlattyTop) + new Vector2(BASE_MAP_PADDING))
                    .IfOddAddOne();

        protected string CalculatedDebug;

        #endregion

        #region Overridden Methods

        protected override void Initialize()
        {
            var random = new Random();
            var n = 10;
            var m = 20;
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
                        this.Hexagons.Add(new Hexagon(r, q,
                            (q == 0 && r == 0) ? Color.Gold
                            : (q == 1 && r == 1) ? Color.Silver
                            : color));
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
                        this.Hexagons.Add(new Hexagon(q, r,
                            (q == 0 && r == 0) ? Color.Gold
                            : (q == 1 && r == 1) ? Color.Silver
                            : color));
                    }
                }
            }

            // maybe make DI:
            // here we register the classes (typeof(FramerateHelper))
            // then the factory looks at the CTOR, calls
            this.Framerate = new FramerateHelper(new Vector2(10, 10), this.SubscribeToLoad, this.SubscribeToUpdate, this.SubscribeToDrawPanel);
            this.Input = new InputHelper(this.SubscribeToUpdate);
            this.Camera = new CameraHelper(() => this.MapSize, () => BASE_MAP_PANEL_SIZE);

            // GraphicsDeviceManager and GameWindow properties require a call to GraphicsDeviceManager.ApplyChanges
            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += this.OnWindowResize;

            // HardwareModeSwitch: if this is set to true, fullscreen automatically scales the regular screen.
            // However, toggling is a lot slower. Also, resizing a non-fullscreen window does not rescale.
            // When set to false, fullscreen is not auto-scaled. By adding a render target it will still auto-scale.
            // This render target can then also be used for non-fullscreen scaling using ClientSizeChanged event.
            // Note that if the backbuffer size is changed in that event handler (like to preserve aspect ratio)
            // then it is important NOT to do that when switching to fullscreen (which also calls the event),
            // because doing so makes it impossible to go back to windowed mode. [See also OnWindowResize]
            this.Graphics.HardwareModeSwitch = false;
            this.Graphics.IsFullScreen = false;
            this.Graphics.PreferredBackBufferWidth = BASE_WINDOW_WIDTH;
            this.Graphics.PreferredBackBufferHeight = BASE_WINDOW_HEIGHT;
            this.Graphics.ApplyChanges();

            // base.Initialize finalizes the GraphicsDevice (and then calls LoadContent)
            base.Initialize();
        }

        protected override void LoadContent()
        {
            this.Content.RootDirectory = CONTENT_ROOT_DIRECTORY;
            this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.Font = this.Content.Load<SpriteFont>("Alphabet/alphabet");
            this.HexOuterPointyTop = this.Content.Load<Texture2D>("xop");
            this.HexInnerPointyTop = this.Content.Load<Texture2D>("xip");
            this.HexOuterFlattyTop = this.Content.Load<Texture2D>("xof");
            this.HexInnerFlattyTop = this.Content.Load<Texture2D>("xif");

            this.BlankTexture = new Texture2D(this.GraphicsDevice, width: 1, height: 1);
            this.BlankTexture.SetData(new[] { Color.White });

            this.WindowScalingRenderTarget = new RenderTarget2D(this.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

            foreach (var hex in this.Hexagons)
            {
                var q = hex.Q;
                var r = hex.R;

                var adjustedWidthPointyTop = this.HexOuterPointyTop.Width / SHORT_OVERLAP_DIVISOR;
                var adjustedHeightPointyTop = this.HexOuterPointyTop.Height / LONG_OVERLAP_DIVISOR;
                var pointyTopX = Math.Round(adjustedWidthPointyTop * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                var pointyTopY = Math.Round(adjustedHeightPointyTop * (3.0 / 2.0 * r));
                hex.PositionPointyTop = new Vector2((float) pointyTopX, (float) pointyTopY);

                var adjustedWidthFlattyTop = this.HexOuterFlattyTop.Width / LONG_OVERLAP_DIVISOR;
                var adjustedHeightFlattyTop = this.HexOuterFlattyTop.Height / SHORT_OVERLAP_DIVISOR;
                var flattyTopX = Math.Round(adjustedWidthFlattyTop * (3.0 / 2.0 * q));
                var flattyTopY = Math.Round(adjustedHeightFlattyTop * (Math.Sqrt(3) / 2 * q + Math.Sqrt(3) * r));
                hex.PositionFlattyTop = new Vector2((float) flattyTopX, (float) flattyTopY);
            }

            (int MinX, int MaxX, int MinY, int MaxY) CalculateMinMax(IEnumerable<(int X, int Y)> points, int width, int height) =>
                points.Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                    (aggregate, point) => (
                        Math.Min(aggregate.MinX, point.X),
                        Math.Max(aggregate.MaxX, point.X + width),
                        Math.Min(aggregate.MinY, point.Y),
                        Math.Max(aggregate.MaxY, point.Y + height)));

            var pPoints = this.Hexagons.Select(hex => ((int) hex.PositionPointyTop.X, (int) hex.PositionPointyTop.Y));
            var (pMinX, pMaxX, pMinY, pMaxY) = CalculateMinMax(pPoints, this.HexOuterPointyTop.Width, this.HexOuterPointyTop.Height);
            this.MapSizePointyTop = new Vector2(pMaxX - pMinX, pMaxY - pMinY);
            this.MapOriginPointyTop = new Vector2(pMinX, pMinY);

            var fPoints = this.Hexagons.Select(hex => ((int) hex.PositionFlattyTop.X, (int) hex.PositionFlattyTop.Y));
            var (fMinX, fMaxX, fMinY, fMaxY) = CalculateMinMax(fPoints, this.HexOuterFlattyTop.Width, this.HexOuterFlattyTop.Height);
            this.MapSizeFlattyTop = new Vector2(fMaxX - fMinX, fMaxY - fMinY);
            this.MapOriginFlattyTop = new Vector2(fMinX, fMinY);

            this.RecalculateClientSize();
            this.RecenterGrid();
            // reposition camera
            this.Camera.Move(Vector2.Zero, clamp: true);

            this.OnLoad?.Invoke(this.Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (this.Input.KeyPressed(Keys.Escape))
                Exit();

            this.IsMouseVisible = true;

            if (this.Input.KeyPressed(Keys.F11) || (this.Input.KeyPressed(Keys.Enter) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt)))
                this.Graphics.ToggleFullScreen();

            if (this.Input.KeyPressed(Keys.C))
            {
                this.PointyTop = !this.PointyTop;

                // probably what we want instead is to have two GridCenters, one for pointy one for flatty
                // same with map size?
                // then simply swapping the PointyTop flag is enough
                this.RecalculateMapSize();
                this.RecenterGrid();
                this.Camera.Move(Vector2.Zero, clamp: true);
            }

            if (!this.Graphics.IsFullScreen)
            {
                if (this.Input.KeyPressed(Keys.R))
                    this.ResizeWindowFromKeyboard(newBackBufferWidth: BASE_WINDOW_WIDTH);
                if (this.Input.KeyPressed(Keys.OemPlus))
                {
                    var newBackBufferWidth = this.Graphics.PreferredBackBufferWidth.AddWithUpperLimit(BASE_WINDOW_WIDTH_INCREMENT, upperLimit: BASE_WINDOW_WIDTH_MAX);
                    this.ResizeWindowFromKeyboard(newBackBufferWidth);
                }
                if (this.Input.KeyPressed(Keys.OemMinus))
                {
                    var newBackBufferWidth = this.Graphics.PreferredBackBufferWidth.AddWithLowerLimit(-BASE_WINDOW_WIDTH_INCREMENT, lowerLimit: BASE_WINDOW_WIDTH_MIN);
                    this.ResizeWindowFromKeyboard(newBackBufferWidth);
                }
            }

            if (this.Input.KeyPressed(Keys.Left))
                this.GridCenter -= new Vector2(100, 0);
            if (this.Input.KeyPressed(Keys.Right))
                this.GridCenter += new Vector2(100, 0);
            if (this.Input.KeyPressed(Keys.Up))
                this.GridCenter -= new Vector2(0, 100);
            if (this.Input.KeyPressed(Keys.Down))
                this.GridCenter += new Vector2(0, 100);

            if (this.Input.KeyPressed(Keys.I))
                this.Camera.CenterOn(this.Hexagons.First(hex => (hex.Q == 0 && hex.R == 0))
                    .Into(x => this.PointyTop ? x.PositionPointyTop : x.PositionFlattyTop));

            this.Camera.HandleInput(this.Input);

            if (this.Camera.IsMoving)
            {
                this.Camera.MouseMove(this.Input.CurrentMouseState.ToVector2());
                if (this.Input.MouseReleased(MouseButton.Right))
                    this.Camera.StopMouseMove();
            }
            if (this.ScaledMapPanelRectangle.Contains(this.Input.CurrentMouseState))
            {
                if (this.Input.MouseScrolled())
                    this.Camera.Zoom(this.Input.MouseScrolledUp() ? .25f : -.25f
                        );//, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

                if (!this.Camera.IsMoving && this.Input.MousePressed(MouseButton.Right))
                    this.Camera.StartMouseMove(this.Input.CurrentMouseState.ToVector2());
            }

            if (this.Input.KeyPressed(Keys.P))
                this.RecalculateDebug();

            if (this.Input.KeyPressed(Keys.O))
                this.RecenterGrid();

            this.BaseMouseVector = this.Input.CurrentMouseState.ToVector2();
            this.ClientSizeTranslatedMouseVector = this.BaseMouseVector * this.ClientSizeTranslation / this.AspectRatio;
            this.CameraTranslatedMouseVector = this.Camera.ScreenToCamera(this.ClientSizeTranslatedMouseVector);

            var width = this.PointyTop ? this.HexOuterPointyTop.Width : this.HexOuterFlattyTop.Width;
            var height = this.PointyTop ? this.HexOuterPointyTop.Height : this.HexOuterFlattyTop.Height;
            var adjustedWidth = this.PointyTop ? width / SHORT_OVERLAP_DIVISOR : width / LONG_OVERLAP_DIVISOR;
            var adjustedHeight = this.PointyTop ? height / LONG_OVERLAP_DIVISOR : height / SHORT_OVERLAP_DIVISOR;

            var mx = this.CameraTranslatedMouseVector.X - this.GridCenter.X - width / 2;
            var my = this.CameraTranslatedMouseVector.Y - this.GridCenter.Y - height / 2;
            double q, r;
            if (this.PointyTop)
            {
                q = (Math.Sqrt(3) / 3.0 * mx - 1.0 / 3.0 * my) / adjustedWidth;
                r = (2.0 / 3.0 * my) / adjustedHeight;
            }
            else
            {
                q = (2.0 / 3.0 * mx) / adjustedWidth;
                r = (-1.0 / 3.0 * mx + Math.Sqrt(3) / 3.0 * my) / adjustedHeight;
            }
            var cube = Cube.Round(q, (-q - r), r);
            this.CursorHexagon = this.Hexagons.FirstOrDefault(x => (x.Cube == cube));

            this.OnUpdate?.Invoke(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.LightSlateGray);

            // this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);
            // this.SpriteBatch.DrawTo(this.BlankTexture, this.ScaledMapPanelRectangle, Color.DarkOliveGreen, depth: 0.1f);
            // this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, transformMatrix: this.Camera.TranslationMatrix);
            this.OnDrawMap?.Invoke(this.SpriteBatch);

            var hexTextureOuter = this.PointyTop ? this.HexOuterPointyTop : this.HexOuterFlattyTop;
            var hexTextureInner = this.PointyTop ? this.HexInnerPointyTop : this.HexInnerFlattyTop;
            foreach (var hex in this.Hexagons)
            {
                var position = this.GridCenter + (this.PointyTop ? hex.PositionPointyTop : hex.PositionFlattyTop);
                this.SpriteBatch.DrawAt(hexTextureOuter, position, 1f, Color.Black, depth: 0.6f);
                var color = (hex == this.CursorHexagon) ? Color.YellowGreen : hex.Color;
                this.SpriteBatch.DrawAt(hexTextureInner, position, 1f, color, depth: 0.5f);
                var hexLog = $"{hex.Q},{hex.R}";
                this.SpriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.IndianRed, scale: 0.5f);
            }

            this.SpriteBatch.DrawTo(this.BlankTexture, this.ScaledMapPanelRectangle, Color.DarkOliveGreen, depth: 0.1f);
            this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);
            this.OnDrawPanel?.Invoke(this.SpriteBatch);

            var mapToPanelSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, 1, BASE_WINDOW_HEIGHT);
            var panelToLogSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT, BASE_SIDE_PANEL_WIDTH, 1);
            var panelOverlay = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, BASE_SIDE_PANEL_WIDTH, BASE_WINDOW_HEIGHT);
            this.SpriteBatch.DrawTo(this.BlankTexture, mapToPanelSeparator, Color.BurlyWood, depth: 0.9f);
            this.SpriteBatch.DrawTo(this.BlankTexture, panelToLogSeparator, Color.BurlyWood, depth: 0.9f);
            this.SpriteBatch.DrawTo(this.BlankTexture, panelOverlay, Color.SlateGray, depth: 0.85f);

            // var topRectangle = new Rectangle(0, 0, BASE_WINDOW_WIDTH - 1, 1);
            // var bottomRectangle = new Rectangle(0, BASE_WINDOW_HEIGHT - 1, BASE_WINDOW_WIDTH - 1, 1);
            // var leftRectangle = new Rectangle(0, 0, 1, BASE_WINDOW_HEIGHT - 1);
            // var rightRectangle = new Rectangle(BASE_WINDOW_WIDTH - 1, 0, 1, BASE_WINDOW_HEIGHT - 1);
            // var middleRectangle = new Rectangle(BASE_WINDOW_WIDTH / 2 - 1, 0, 2, BASE_WINDOW_HEIGHT - 1);
            // this.SpriteBatch.DrawTo(this.BlankTexture, topRectangle, Color.Maroon);
            // this.SpriteBatch.DrawTo(this.BlankTexture, bottomRectangle, Color.Maroon);
            // this.SpriteBatch.DrawTo(this.BlankTexture, leftRectangle, Color.Maroon);
            // this.SpriteBatch.DrawTo(this.BlankTexture, rightRectangle, Color.Maroon);
            // this.SpriteBatch.DrawTo(this.BlankTexture, middleRectangle, Color.Maroon);

            var log = $"M1: {this.BaseMouseVector.Print()}"
                + Environment.NewLine + $"M2: {this.ClientSizeTranslatedMouseVector.PrintRounded()}"
                + Environment.NewLine + $"M3: {this.CameraTranslatedMouseVector.PrintRounded()}"
                + Environment.NewLine + "SW:" + this.ScaledWindowSize.Print()
                + Environment.NewLine + "SM:" + this.ScaledMapPanelSize.Print()
                + Environment.NewLine + "ZF:" + this.Camera.ZoomScaleFactor
                + Environment.NewLine + "GC:" + this.GridCenter.Print()
                + Environment.NewLine + "CP:" + this.Camera.Position.Print()
                + Environment.NewLine + "MS:" + this.MapSize.Print()
                + Environment.NewLine + "CZ:" + this.ClientSizeTranslation.Print()
                + Environment.NewLine + "AR:" + this.AspectRatio.Print()
                + Environment.NewLine + this.CalculatedDebug;

            this.SpriteBatch.DrawText(this.Font, log, new Vector2(10 + BASE_MAP_PANEL_WIDTH, 10));
            this.SpriteBatch.End();
        }

        protected override bool BeginDraw()
        {
            this.GraphicsDevice.SetRenderTarget(this.WindowScalingRenderTarget);
            return base.BeginDraw();
        }

        protected override void EndDraw()
        {
            this.GraphicsDevice.SetRenderTarget(null);
            this.SpriteBatch.Begin();
            this.SpriteBatch.Draw(this.WindowScalingRenderTarget, this.GraphicsDevice.Viewport.Bounds, Color.White);
            this.SpriteBatch.End();
            base.EndDraw();
        }

        #endregion

        #region Helper Methods

        protected void RecalculateDebug()
        {
        }

        protected void RecenterGrid()
        {
            var centerHex = this.Hexagons.First(hex => ((hex.Q == 0) && (hex.R == 0)));
            var centerPosition = (this.PointyTop ? centerHex.PositionPointyTop : centerHex.PositionFlattyTop);
            var mapPosition = (this.PointyTop ? this.MapOriginPointyTop : this.MapOriginFlattyTop);
            var centerOffset = (centerPosition - mapPosition);

            var totalHexagonSpace = (this.PointyTop ? this.MapSizePointyTop : this.MapSizeFlattyTop);
            var halfAvailableSpace = this.ScaledMapPanelSize / 2;
            var hexagonSpaceCentered = halfAvailableSpace - (totalHexagonSpace / 2);

            this.GridCenter = Vector2.Floor(hexagonSpaceCentered + centerOffset);
        }

        protected void ResizeWindowFromKeyboard(int newBackBufferWidth)
        {
            this.Graphics.PreferredBackBufferWidth = newBackBufferWidth;
            this.Graphics.ApplyChanges();
            this.OnWindowResize(this, default);
            this.Window.Position = new Point(
                (this.GraphicsDevice.Adapter.CurrentDisplayMode.Width / 2) - (this.Graphics.PreferredBackBufferWidth / 2),
                (this.GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2) - (this.Graphics.PreferredBackBufferHeight / 2));
        }

        protected void OnWindowResize(object sender, EventArgs e)
        {
            // When going to fullscreen, it is important not to change the backbuffer size,
            // because doing that makes it impossible to go back to windowed mode (might be a bug in Monogame?).
            if (!this.Graphics.IsFullScreen)
            {
                // This event would be triggered again by GraphicsDeviceManager.ApplyChanges
                this.Window.ClientSizeChanged -= this.OnWindowResize;

                if (this.Window.ClientBounds.Width != this.ScaledWindowSize.X)
                {
                    this.Graphics.PreferredBackBufferWidth = this.Window.ClientBounds.Width;
                    this.Graphics.PreferredBackBufferHeight = (int) (this.Window.ClientBounds.Width / BASE_ASPECT_RATIO);
                }
                else if (this.Window.ClientBounds.Height != this.ScaledWindowSize.Y)
                {
                    this.Graphics.PreferredBackBufferWidth = (int) (this.Window.ClientBounds.Height * BASE_ASPECT_RATIO);
                    this.Graphics.PreferredBackBufferHeight = this.Window.ClientBounds.Height;
                }

                this.Graphics.ApplyChanges();
                this.Window.ClientSizeChanged += this.OnWindowResize;
            }
            this.RecalculateClientSize();
        }

        protected void RecalculateClientSize()
        {
            this.AspectRatio = new Vector2(
                this.Graphics.PreferredBackBufferWidth / (float) BASE_WINDOW_WIDTH,
                this.Graphics.PreferredBackBufferHeight / (float) BASE_WINDOW_HEIGHT
            );
            this.ClientSizeTranslation = new Vector2(
                this.Graphics.PreferredBackBufferWidth / (float) this.Window.ClientBounds.Width,
                this.Graphics.PreferredBackBufferHeight / (float) this.Window.ClientBounds.Height);
            this.ScaledWindowSize = new Vector2(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
            this.RecalculateMapSize();
        }

        protected void RecalculateMapSize()
        {
            this.ScaledMapPanelSize = this.MapSize / this.ClientSizeTranslation;
            this.ScaledMapPanelRectangle = new Rectangle(Vector2.Zero.ToPoint(), this.ScaledMapPanelSize.ToPoint());
        }

        protected void SubscribeToLoad(Action<ContentManager> handler) => this.OnLoad += handler;

        protected void SubscribeToUpdate(Action<GameTime> handler) => this.OnUpdate += handler;

        protected void SubscribeToDrawMap(Action<SpriteBatch> handler) => this.OnDrawMap += handler;

        protected void SubscribeToDrawPanel(Action<SpriteBatch> handler) => this.OnDrawPanel += handler;

        #endregion
    }
}