using System.Runtime.InteropServices.ComTypes;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Helpers;
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
        public const int BASE_MAP_WIDTH = 791; // 1280 / 1.618 = 791.10
        public const int BASE_MAP_HEIGHT = BASE_WINDOW_HEIGHT;
        public const int BASE_PANEL_WIDTH = BASE_WINDOW_WIDTH - BASE_MAP_WIDTH;
        public const int BASE_PANEL_HEIGHT = 445; // 720 / 1.618 = 444.99
        private const float BASE_ASPECT_RATIO = BASE_WINDOW_WIDTH / (float) BASE_WINDOW_HEIGHT;
        private const float GOLDEN_RATIO = 1.618f;

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
        // protected Matrix ScalingMatrix { get; set; }
        protected Vector2 ClientSizeTranslation { get; set; }
        protected Vector2 ScaledWindowSize { get; set; }
        protected Vector2 ScaledMapSize { get; set; }
        protected Rectangle ScaledMapRectangle { get; set; }

        protected float MapWidth => this.Graphics.PreferredBackBufferWidth / GOLDEN_RATIO;
        protected float MapHeight => this.Graphics.PreferredBackBufferHeight;

        protected string CalculatedDebug;

        #endregion

        #region Overridden Methods

        protected override void Initialize()
        {
            var random = new Random();
            var n = 20;
            var m = 30;
            // for (var q = -n; q <= n; q++)
            // {
            //     // var color = new Color(random.Next(256), random.Next(256), random.Next(256));
            //     var color = Color.White;
            //     var r1 = Math.Max(-n, -q - n);
            //     var r2 = Math.Min(n, -q + n);
            //     for (var r = r1; r <= r2; r++)
            //     {
            //         this.Hexagons.Add(new Hexagon(r, q,
            //             (q == 0 && r == 0) ? Color.Gold
            //             : (q == 1 && r == 1) ? Color.Silver
            //             : color));
            //     }
            // }
            for (var r = 0; r < m; r++)
            {
                var color = Color.White;
                var r_offset = (int) Math.Floor(r / 2f);
                for (var q = -r_offset; q < n - r_offset; q++)
                {
                    this.Hexagons.Add(new Hexagon(q, r,
                        (q == 0 && r == 0) ? Color.Gold
                        : (q == 1 && r == 1) ? Color.Silver
                        : color));
                }
            }

            // maybe make DI:
            // here we register the classes (typeof(FramerateHelper))
            // then the factory looks at the CTOR, calls
            this.Framerate = new FramerateHelper(new Vector2(10, 10), this.SubscribeToLoad, this.SubscribeToUpdate, this.SubscribeToDrawPanel);
            this.Input = new InputHelper(this.SubscribeToUpdate);
            this.Camera = new CameraHelper(() => BASE_MAP_WIDTH, () => BASE_MAP_HEIGHT);

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

            // var HexTexOuter2 = this.HexTexOuter.

            this.BlankTexture = new Texture2D(this.GraphicsDevice, width: 1, height: 1);
            this.BlankTexture.SetData(new[] { Color.White });

            this.RecalculateSizes();
            this.WindowScalingRenderTarget = new RenderTarget2D(this.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

            this.GridCenter = new Vector2(BASE_MAP_WIDTH / 2 - this.HexOuterPointyTop.Width / 2, BASE_MAP_HEIGHT / 2 - this.HexOuterPointyTop.Height / 2);

            foreach (var hex in this.Hexagons)
            {
                var q = hex.X;
                var r = hex.Y;

                // no idea what these divisions are (possibly to account for hexagon borders sharing pixels?)
                // but without them there are small gaps or overlap between hexagons, especially as coordinates increase
                var shortDivisor = 1.805; // this seems to be the offset for odd rows(pointy)/cols(flat)
                var longDivisor = 2.072; // but this one no idea, doesn't seem to match any offset

                var adjustedWidthPointyTop = this.HexOuterPointyTop.Width / shortDivisor;
                var adjustedHeightPointyTop = this.HexOuterPointyTop.Height / longDivisor;
                var pointyTopX = Math.Round(adjustedWidthPointyTop * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2 * r));
                var pointyTopY = Math.Round(adjustedHeightPointyTop * (3.0 / 2.0 * r));
                hex.PositionPointyTop = new Vector2((float) pointyTopX, (float) pointyTopY);

                var adjustedWidthFlattyTop = this.HexOuterFlattyTop.Width / longDivisor;
                var adjustedHeightFlattyTop = this.HexOuterFlattyTop.Height / shortDivisor;
                var flattyTopX = Math.Round(adjustedWidthFlattyTop * (3.0 / 2.0 * q));
                var flattyTopY = Math.Round(adjustedHeightFlattyTop * (Math.Sqrt(3) / 2 * q + Math.Sqrt(3) * r));
                hex.PositionFlattyTop = new Vector2((float) flattyTopX, (float) flattyTopY);
            }

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
                this.PointyTop = !this.PointyTop;

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

            if (this.Input.KeyPressed(Keys.P))
                this.Camera.CenterOn(this.Hexagons.First(hex => (hex.X == 0 && hex.Y == 0)));

            this.Camera.HandleInput(this.Input);

            var rect = new Rectangle(Vector2.Zero.ToPoint(), this.ScaledMapSize.ToPoint());

            if (this.Camera.IsMoving)
            {
                this.Camera.MouseMove(this.Input.CurrentMouseState.ToVector2());
                if (this.Input.MouseReleased(MouseButton.Middle))
                    this.Camera.StopMouseMove();
            }
            if (this.ScaledMapRectangle.Contains(this.Input.CurrentMouseState))
            {
                if (this.Input.MouseScrolled())
                    this.Camera.Zoom(this.Input.MouseScrolledUp() ? .25f : -.25f
                        );//, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

                if (!this.Camera.IsMoving && this.Input.MousePressed(MouseButton.Middle))
                    this.Camera.StartMouseMove(this.Input.CurrentMouseState.ToVector2());
            }

            if (this.Input.KeyPressed(Keys.P))
                this.RecalculateDebug();

            this.OnUpdate?.Invoke(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.LightSlateGray);
            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, transformMatrix: this.Camera.TranslationMatrix);
            this.OnDrawMap?.Invoke(this.SpriteBatch);

            var hexTextureOuter = this.PointyTop ? this.HexOuterPointyTop : this.HexOuterFlattyTop;
            var hexTextureInner = this.PointyTop ? this.HexInnerPointyTop : this.HexInnerFlattyTop;
            foreach (var hex in this.Hexagons)
            {
                var position = this.GridCenter + (this.PointyTop ? hex.PositionPointyTop : hex.PositionFlattyTop);
                this.SpriteBatch.DrawAt(hexTextureOuter, position, 1f, Color.Black, depth: 0.6f);
                this.SpriteBatch.DrawAt(hexTextureInner, position, 1f, hex.Color, depth: 0.5f);
                var hexLog = $"{hex.X},{hex.Y}";
                this.SpriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.IndianRed, scale: 0.5f);
            }

            this.SpriteBatch.DrawTo(this.BlankTexture, this.ScaledMapRectangle, Color.DarkOliveGreen, depth: 0.1f);
            this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);
            this.OnDrawPanel?.Invoke(this.SpriteBatch);

            var mapToPanelSeparator = new Rectangle(BASE_MAP_WIDTH, 0, 1, BASE_WINDOW_HEIGHT);
            var panelToLogSeparator = new Rectangle(BASE_MAP_WIDTH, BASE_PANEL_HEIGHT, BASE_PANEL_WIDTH, 1);
            var panelOverlay = new Rectangle(BASE_MAP_WIDTH, 0, BASE_PANEL_WIDTH, BASE_WINDOW_HEIGHT);
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

            var absoluteMouseVector = this.Input.CurrentMouseState.ToVector2();
            // client size translation is needed for fullscreen mode (in windowed mode ClientBounds == BackBuffer size)
            var clientSizeTranslatedMouseVector = absoluteMouseVector * this.ClientSizeTranslation;
            // camera translation is needed when camera is zoomed in
            var cameraTranslatedMouseVector = this.Camera.ScreenToCamera(clientSizeTranslatedMouseVector);

            var log = $"M: {absoluteMouseVector.X:0}, {absoluteMouseVector.Y:0}"
                + Environment.NewLine + $"R: {clientSizeTranslatedMouseVector.X:0}, {clientSizeTranslatedMouseVector.Y:0}"
                + Environment.NewLine + $"C: {cameraTranslatedMouseVector.X:0}, {cameraTranslatedMouseVector.Y:0}"
                + Environment.NewLine + this.ScaledWindowSize.ToString()
                + Environment.NewLine + this.ScaledMapSize.ToString()
                + Environment.NewLine + this.CalculatedDebug;

            this.SpriteBatch.DrawText(this.Font, log, new Vector2(10 + BASE_MAP_WIDTH, 10));
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
            var hw = this.PointyTop ? 25 : 29;
            var hh = this.PointyTop ? 29 : 25;
            var (minX, maxX, minY, maxY) = this.Hexagons
                .Aggregate((MinX: int.MaxValue, MaxX: int.MinValue, MinY: int.MaxValue, MaxY: int.MinValue),
                    (t, x) => (Math.Min(t.MinX, Math.Abs(x.X)), Math.Max(t.MaxX, Math.Abs(x.X)),
                        Math.Min(t.MinY, Math.Abs(x.Y)), Math.Max(t.MaxY, Math.Abs(x.Y))));
            
            // need to calculate furthest left and furthest right
            // and furthers up and furthest down
            // minX is not enough because going down in straight line lowers X but does not go further left

            // so calculate for each row what is width and for each column
            // then take max
            // add half hex offset (if > 1 in row / col)

            var width = (maxX - minX) * hw;
            var height = (maxY - minY) * hh;
            this.CalculatedDebug = $"x:{minX}/{maxX} y:{minY}/{maxY} w:{width} h:{height}";
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
            this.RecalculateSizes();
        }

        protected void RecalculateSizes()
        {
            this.ClientSizeTranslation = new Vector2(
                this.Graphics.PreferredBackBufferWidth / (float) this.Window.ClientBounds.Width,
                this.Graphics.PreferredBackBufferHeight / (float) this.Window.ClientBounds.Height);
            this.ScaledWindowSize = new Vector2(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
            this.ScaledMapSize = new Vector2(this.MapWidth, this.MapHeight) / this.ClientSizeTranslation;
            this.ScaledMapRectangle = new Rectangle(Vector2.Zero.ToPoint(), this.ScaledMapSize.ToPoint());
        }

        protected void SubscribeToLoad(Action<ContentManager> handler) => this.OnLoad += handler;

        protected void SubscribeToUpdate(Action<GameTime> handler) => this.OnUpdate += handler;

        protected void SubscribeToDrawMap(Action<SpriteBatch> handler) => this.OnDrawMap += handler;

        protected void SubscribeToDrawPanel(Action<SpriteBatch> handler) => this.OnDrawPanel += handler;

        #endregion
    }

    public class Hexagon
    {
        public Hexagon(int x, int y, Color color)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
        }

        public int X { get; }
        public int Y { get; }
        public Color Color { get; }
        public Vector2 PositionPointyTop { get; set; }
        public Vector2 PositionFlattyTop { get; set; }
    }
}