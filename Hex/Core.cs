using Extended.Collections;
using Extended.Extensions;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Helpers;
using Hex.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi;
using Mogi.Controls;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Helpers;
using Mogi.Inversion;
using Mogi.State;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex
{
    public class Core : Game, IRoot
    {
        #region Constants

        private const string CONTENT_ROOT_DIRECTORY = "Content";
        private const float BASE_GLOBAL_SCALE = 1.5f;
        private const float MAX_GLOBAL_SCALE = 5f;
        private const float MIN_GLOBAL_SCALE = 0.25f;
        private const int BASE_WINDOW_WIDTH = 1280;
        private const int BASE_WINDOW_WIDTH_INCREMENT = BASE_WINDOW_WIDTH / 8; // used for keyboard-based scaling
        private const int BASE_WINDOW_WIDTH_MIN = BASE_WINDOW_WIDTH / 4; // minimum for keyboard-based scaling (not for mouse)
        private const int BASE_WINDOW_HEIGHT = 720;
        private const int BASE_MAP_PANEL_WIDTH = 790; // 1280 / 1.618 = 791.10 : using 790 for even number
        private const int BASE_MAP_PANEL_HEIGHT = BASE_WINDOW_HEIGHT;
        private const int BASE_SIDE_PANEL_WIDTH = BASE_WINDOW_WIDTH - BASE_MAP_PANEL_WIDTH;
        private const int BASE_SIDE_PANEL_HEIGHT = 445; // 720 / 1.618 = 444.99
        private const float BASE_ASPECT_RATIO = BASE_WINDOW_WIDTH / (float) BASE_WINDOW_HEIGHT;
        private const float GOLDEN_RATIO = 1.618f;

        private static readonly Vector2 BASE_WINDOW_SIZE = new Vector2(BASE_WINDOW_WIDTH, BASE_WINDOW_HEIGHT);
        private static readonly Rectangle BASE_WINDOW_RECTANGLE = BASE_WINDOW_SIZE.ToRectangle();
        private static readonly Vector2 BASE_MAP_PANEL_SIZE = new Vector2(BASE_MAP_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT);
        private const int BASE_MAP_PADDING = 30;

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

        public Core()
        {
            this.Graphics = new GraphicsDeviceManager(this);
        }

        #endregion

        #region Data Members

        public event Action<GameTime> OnUpdate;
        public event Action<SpriteBatch> OnDraw;
        public event Action<WindowState> OnResize;

        protected event Action<ContentManager> OnLoad;
        protected event Action<GameTime> OnUpdateCritical;
        protected event Action<GameTime> OnUpdateRegular;
        protected event Action<SpriteBatch> OnDrawMap;
        protected event Action<SpriteBatch> OnDrawPanel;

        protected GraphicsDeviceManager Graphics { get; set; }
        protected WindowState WindowState { get; set; }
        protected SpriteBatch SpriteBatch { get; set; }

        protected FramerateHelper Framerate { get; set; }
        protected InputHelper Input { get; set; }
        protected CameraHelper Camera { get; set; }

        protected Architect Architect { get; set; }

        protected SpriteFont Font { get; set; }
        protected Texture2D HexOuterPointyTop { get; set; }
        protected Texture2D HexInnerPointyTop { get; set; }
        protected Texture2D HexOuterFlattyTop { get; set; }
        protected Texture2D HexInnerFlattyTop { get; set; }
        protected Texture2D HexBorderPointyTop { get; set; }
        protected Texture2D HexBorderFlattyTop { get; set; }
        protected Texture2D BlankTexture { get; set; }

        protected Vector2 HexagonPointySize { get; set; }
        protected Vector2 HexagonFlattySize { get; set; }
        protected (double X, double Y) HexagonPointySizeAdjusted { get; set; }
        protected (double X, double Y) HexagonFlattySizeAdjusted { get; set; }

        protected RenderTarget2D WindowScalingRenderTarget { get; set; }
        protected Vector2 ClientSizeTranslation { get; set; }
        protected Vector2 VirtualSizeTranslation { get; set; }
        protected Vector2 PreviousClientBounds { get; set; }

        /// <summary> Mouse position relative to window. </summary>
        protected Vector2 BaseMouseVector { get; set; }

        /// <summary> Resolution translation is needed only when in windowed mode client resolution does not match virtual resolution (in fullscreen mode they always match). </summary>
        protected Vector2 ResolutionTranslatedMouseVector { get; set; }

        /// <summary> Camera translation is needed when camera is zoomed. </summary>
        protected Vector2 CameraTranslatedMouseVector { get; set; }

        /// <summary>
        /// Hexagon rotation is supported in intervals of 30 degrees from 0° to 330°. Rotating to 360° will reset the rotation to 0°, which means there are 12 possible orientations.
        /// <br/> Even-numbered rotations use pointy-top hexagonal shapes, odd-numbered rotations use flatty-top shapes.
        /// </summary>
        protected Cycle<int> Orientation { get; } = new Cycle<int>(Generate.Range(12).ToArray());

        protected bool HexagonsArePointy => this.Orientation.Value.IsEven();
        protected HexagonMap HexagonMap { get; set; }
        protected Vector2 GridOrigin { get; set; }
        protected Hexagon OriginHexagon { get; set; }
        protected Hexagon CenterHexagon { get; set; }
        protected Hexagon CursorHexagon { get; set; }
        protected Hexagon LastCursorHexagon { get; set; }
        protected Hexagon SourceHexagon { get; set; }
        protected Hexagon LastSourceHexagon { get; set; }
        protected bool CalculatedVisibility { get; set; }
        protected IDictionary<Hexagon, bool> VisibilityByHexagonMap { get; } = new Dictionary<Hexagon, bool>();

        protected Vector2[] GridSizes { get; set; }

        protected Vector2 MapSize =>
            Vector2.Max(BASE_MAP_PANEL_SIZE, (this.GridSizes[this.Orientation] + new Vector2(BASE_MAP_PADDING)))
                .IfOddAddOne();

        protected Vector2 HexSize => this.HexagonsArePointy ? this.HexagonPointySize : this.HexagonFlattySize;
        protected (double X, double Y) HexSizeAdjusted => this.HexagonsArePointy ? this.HexagonPointySizeAdjusted : this.HexagonFlattySizeAdjusted;
        protected Texture2D HexOuterTexture => this.HexagonsArePointy ? this.HexOuterPointyTop : this.HexOuterFlattyTop;
        protected Texture2D HexInnerTexture => this.HexagonsArePointy ? this.HexInnerPointyTop : this.HexInnerFlattyTop;
        protected Texture2D HexBorderTexture => this.HexagonsArePointy ? this.HexBorderPointyTop : this.HexBorderFlattyTop;

        protected bool PrintCoords { get; set; }
        protected string CalculatedDebug;

        #endregion

        #region Overridden Methods

        protected override void Initialize()
        {
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

            // This is the render target that is respectively set and unset before and after drawing. [See BeginDraw|EndDraw]
            // Setting a render target changes the GraphicsDevice.Viewport size to match render target size.
            // After unsetting it, the viewport returns to client size. The target can then be drawn as a texture,
            // and everything that was drawn on it will be drawn to the client and be automatically scale to client size.
            this.WindowScalingRenderTarget = new RenderTarget2D(this.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

            // A container of properties related to the client window: it should be updated whenever the client size changes.
            // It can provide, among other things, the necessary data to calculate resolution-indepedent mouse position.
            // It serves as an alternative to passing around the entire Game instance, to support separation of concerns.
            this.WindowState = new WindowState(this.Window, this.Graphics, BASE_WINDOW_SIZE);

            // base.Initialize finalizes the GraphicsDevice (and then calls LoadContent)
            base.Initialize();
        }

        protected override void LoadContent()
        {
            this.Content.RootDirectory = CONTENT_ROOT_DIRECTORY;
            this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.BlankTexture = new Texture2D(this.GraphicsDevice, width: 1, height: 1);
            this.BlankTexture.SetData(new[] { Color.White });

            this.Font = this.Content.Load<SpriteFont>("Alphabet/alphabet");

            var dependency = DependencyHelper.Create(this);
            dependency.Register(this.WindowState);
            dependency.Register(this.SpriteBatch);
            // dependency.Register(this.Content);
            // dependency.Register(this.Graphics);
            dependency.Register(this.BlankTexture);
            dependency.Register(this.Font);
            this.Input = dependency.Register<InputHelper>();
            this.Architect = dependency.Register<Architect>();

            this.Camera = new CameraHelper(() => this.MapSize, () => BASE_MAP_PANEL_SIZE, this.Input, this.WindowState);

            this.HexOuterPointyTop = this.Content.Load<Texture2D>("xop");
            this.HexInnerPointyTop = this.Content.Load<Texture2D>("xip");
            this.HexOuterFlattyTop = this.Content.Load<Texture2D>("xof");
            this.HexInnerFlattyTop = this.Content.Load<Texture2D>("xif");
            this.HexBorderPointyTop = this.Content.Load<Texture2D>("xbp");
            this.HexBorderFlattyTop = this.Content.Load<Texture2D>("xbf");

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

            this.PanelTexture = this.Content.Load<Texture2D>("panel");
            this.YesTexture = this.Content.Load<Texture2D>("buttonYes");
            this.NoTexture = this.Content.Load<Texture2D>("buttonNo");
            this.ExitTexture = this.Content.Load<Texture2D>("exit");
            this.SourceHexagon = this.HexagonMap[new Cube(0, -12, 12)];
            this.Orientation.Advance();
            this.Orientation.Advance();
            this.ResizeWindowFromKeyboard(this.Graphics.PreferredBackBufferWidth);

            this.RecenterGrid();
            this.Camera.Center();

            // var exitConfirmationPanelSize = new Vector2(256, 144);
            var exitConfirmationPanelSize = new Vector2(400, 100);
            var exitConfirmationPanelLocation = (BASE_WINDOW_SIZE / 2) - (exitConfirmationPanelSize / 2);
            var exitConfirmationPanelRectangle = new Rectangle(exitConfirmationPanelLocation.ToPoint(), exitConfirmationPanelSize.ToPoint());
            this.ExitConfirmation = new Panel(exitConfirmationPanelRectangle);
            this.ExitConfirmation.Append(new Patch(exitConfirmationPanelRectangle, this.PanelTexture, 13));

            // this.ExitConfirmation.Append(new Basic(new Rectangle(BASE_WINDOW_WIDTH / 2 - 40, BASE_WINDOW_HEIGHT / 2 - 40, 80, 40), this.ExitTexture));
            var exitConfirmationText = "Are you sure you want to quit?";
            var exitConformationTextScale = 2f;
            var exitCOnformationTextSize = this.Font.MeasureString(exitConfirmationText) * exitConformationTextScale;
            var exitConformationTextLocation = (BASE_WINDOW_SIZE / 2) - (exitCOnformationTextSize / 2) - new Vector2(0, 30);
            this.ExitConfirmation.Append(new Label(new Rectangle(exitConformationTextLocation.ToPoint(), exitCOnformationTextSize.ToPoint()), this.Font, exitConfirmationText, exitConformationTextScale));

            var noYesButtonSize = new Vector2(40);
            var noButtonLocation = (BASE_WINDOW_SIZE / 2) - new Vector2(noYesButtonSize.X, 0) * 1.5f;
            var noButton = new Button(new Rectangle(noButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.NoTexture, new Color(200, 0, 0));
            noButton.OnClick += button => this.ExitConfirmation.Toggle();
            this.ExitConfirmation.Append(noButton);

            var yesButtonLocation = (BASE_WINDOW_SIZE / 2) + new Vector2(noYesButtonSize.X, 0) / 1.5f;
            var yesButton = new Button(new Rectangle(yesButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.YesTexture, new Color(0, 200, 0));
            yesButton.OnClick += button => this.Exit();
            this.ExitConfirmation.Append(yesButton);
        }
        Texture2D PanelTexture;
        Texture2D YesTexture;
        Texture2D NoTexture;
        Texture2D ExitTexture;
        Panel ExitConfirmation;

        protected override void Update(GameTime gameTime)
        {
            this.OnUpdate?.Invoke(gameTime);

            this.ExitConfirmation.Update(gameTime);
            if (this.Input.KeyPressed(Keys.Escape))
                this.ExitConfirmation.Toggle();
            if (this.ExitConfirmation.IsActive)
                return;

            this.IsMouseVisible = true;

            if (this.Input.KeyPressed(Keys.F11) || (this.Input.KeyPressed(Keys.Enter) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt)))
                this.Graphics.ToggleFullScreen();

            if (this.Input.KeyPressed(Keys.C))
            {
                this.RecenterGrid();
                this.Camera.Center();
            }

            if (!this.Graphics.IsFullScreen)
            {
                if (this.Input.KeyPressed(Keys.R))
                    this.ResizeWindowFromKeyboard(newBackBufferWidth: BASE_WINDOW_WIDTH);
                if (this.Input.KeyPressed(Keys.OemPlus))
                {
                    var newBackBufferWidth = Math.Clamp(this.Graphics.PreferredBackBufferWidth + BASE_WINDOW_WIDTH_INCREMENT, BASE_WINDOW_WIDTH_MIN, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
                    this.ResizeWindowFromKeyboard(newBackBufferWidth);
                }
                if (this.Input.KeyPressed(Keys.OemMinus))
                {
                    var newBackBufferWidth = Math.Clamp(this.Graphics.PreferredBackBufferWidth - BASE_WINDOW_WIDTH_INCREMENT, BASE_WINDOW_WIDTH_MIN, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
                    this.ResizeWindowFromKeyboard(newBackBufferWidth);
                }
            }

            // if (this.Input.KeyPressed(Keys.Left))
            //     this.GridOrigin -= new Vector2(100, 0);
            // if (this.Input.KeyPressed(Keys.Right))
            //     this.GridOrigin += new Vector2(100, 0);
            // if (this.Input.KeyPressed(Keys.Up))
            //     this.GridOrigin -= new Vector2(0, 100);
            // if (this.Input.KeyPressed(Keys.Down))
            //     this.GridOrigin += new Vector2(0, 100);

            if (this.Input.KeyPressed(Keys.I))
                this.Camera.CenterOn(this.GetPosition(this.CenterHexagon));

            this.Camera.HandleInput(this.Input);

            if (this.Input.KeyPressed(Keys.P))
                this.PrintCoords = !this.PrintCoords;

            if (this.Input.KeyPressed(Keys.B))
                this.RecalculateDebug();

            if (this.Input.KeyPressed(Keys.O))
                this.RecenterGrid();

            if (this.Input.MouseMoved())
            {
                this.BaseMouseVector = this.Input.CurrentMouseVector;
                this.ResolutionTranslatedMouseVector = this.WindowState.Translate(this.BaseMouseVector);// * this.ClientSizeTranslation / this.VirtualSizeTranslation;
                this.CameraTranslatedMouseVector = this.Camera.FromScreen(this.ResolutionTranslatedMouseVector);

                if (BASE_MAP_PANEL_SIZE.ToRectangle().Contains(this.ResolutionTranslatedMouseVector))
                {
                    var cubeAtMouse = this.ToCubeCoordinates(this.CameraTranslatedMouseVector);
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
                this.Rotate(advance: true);
            if (this.Input.KeyPressed(Keys.X))
                this.Rotate(advance: false);
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.LightSlateGray);

            // this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);
            // this.SpriteBatch.DrawTo(this.BlankTexture, this.ScaledMapPanelRectangle, Color.DarkOliveGreen, depth: 0.1f);
            // this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap, transformMatrix: this.Camera.TranslationMatrix);
            this.OnDrawMap?.Invoke(this.SpriteBatch);

            foreach (var hex in this.HexagonMap.Values)
            {
                var cube = this.GetCube(hex);
                var position = this.GridOrigin + this.GetPosition(hex);
                this.SpriteBatch.DrawAt(this.HexOuterTexture, position, 1f, Color.Black, depth: 0.6f);
                var color = (hex == this.SourceHexagon) ? Color.Coral
                    : (hex == this.CursorHexagon) ? Color.LightYellow
                    : this.VisibilityByHexagonMap.TryGetValue(hex, out var visible) ? (visible ? new Color(205, 235, 185) : new Color(175, 195, 160))
                    : hex.TileType switch
                    {
                        TileType.Mountain => Color.Tan,
                        _ => new Color(190, 230, 160)
                    };
                this.SpriteBatch.DrawAt(this.HexInnerTexture, position, 1f, color, depth: 0.5f);

                // TODO calculate border hexagons and only draw for them, note it changes by orientation!
                this.SpriteBatch.DrawAt(this.HexBorderTexture, position, 1f, Color.Sienna, depth: 0.4f);

                // TODO if mountain tiles are on top of each other it looks bad, calculate
                if (hex.TileType == TileType.Mountain)
                    this.SpriteBatch.DrawAt(this.HexBorderTexture, position - new Vector2(0, 5), 1f, Color.Sienna, depth: 0.55f);
            }

            this.SpriteBatch.DrawTo(this.BlankTexture, this.MapSize.ToRectangle(), Color.DarkSlateGray, depth: 0.15f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, this.WindowState.Translate(this.MapSize).ToRectangle(), Color.DarkSlateGray, depth: 0.15f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, this.ScaledMapPanelRectangle, Color.Orange, depth: 0.15f);
            this.SpriteBatch.End();

            if (this.PrintCoords)
            {
                this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, transformMatrix: this.Camera.TranslationMatrix);
                foreach (var hex in this.HexagonMap.Values)
                {
                    var cube = this.GetCube(hex);
                    var position = this.GridOrigin + this.GetPosition(hex);
                    var (q, r) = cube.ToAxial();
                    var hexLog = $"{q},{r}";
                    this.SpriteBatch.DrawText(this.Font, hexLog, position + new Vector2(5), Color.MistyRose, scale: 0.5f);
                }
                this.SpriteBatch.End();
            }

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);

            var mapToPanelSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, 1, BASE_WINDOW_HEIGHT);
            var panelToLogSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT, BASE_SIDE_PANEL_WIDTH, 1);
            var panelOverlay = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, BASE_SIDE_PANEL_WIDTH, BASE_WINDOW_HEIGHT);
            this.SpriteBatch.DrawTo(this.BlankTexture, mapToPanelSeparator, Color.BurlyWood, depth: 0.9f);
            this.SpriteBatch.DrawTo(this.BlankTexture, panelToLogSeparator, Color.BurlyWood, depth: 0.9f);
            this.SpriteBatch.DrawTo(this.BlankTexture, panelOverlay, Color.SlateGray, depth: 0.85f);

            this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp);
            this.OnDrawPanel?.Invoke(this.SpriteBatch);

            // var topRectangle = new Rectangle(0, 0, BASE_WINDOW_WIDTH - 1, 1);
            // var bottomRectangle = new Rectangle(0, BASE_WINDOW_HEIGHT - 1, BASE_WINDOW_WIDTH - 1, 1);
            // var leftRectangle = new Rectangle(0, 0, 1, BASE_WINDOW_HEIGHT - 1);
            // var rightRectangle = new Rectangle(BASE_WINDOW_WIDTH - 1, 0, 1, BASE_WINDOW_HEIGHT - 1);
            // var middleRectangle = new Rectangle(BASE_WINDOW_WIDTH / 2 - 1, 0, 2, BASE_WINDOW_HEIGHT - 1);
            // this.SpriteBatch.DrawTo(this.BlankTexture, topRectangle, Color.Maroon, depth: 1f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, bottomRectangle, Color.Maroon, depth: 1f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, leftRectangle, Color.Maroon, depth: 1f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, rightRectangle, Color.Maroon, depth: 1f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, middleRectangle, Color.Maroon, depth: 1f);

            // var portraitRectangle = new Rectangle(BASE_MAP_PANEL_WIDTH + 30, 30, 64, 64);
            // this.SpriteBatch.DrawTo(this.BlankTexture, portraitRectangle, Color.WhiteSmoke, depth: 1f);

            var log = /*             */ "M1:" + this.BaseMouseVector.Print()
            // var log = "M2:" + this.ClientSizeTranslatedMouseVector.PrintRounded()
                + Environment.NewLine + "M2:" + this.ResolutionTranslatedMouseVector.PrintRounded()
                // + Environment.NewLine + "M2b:" + this.CameraTranslatedMouseVector.PrintRounded()
                + Environment.NewLine + "M3:" + this.CameraTranslatedMouseVector.PrintRounded()
                //     + Environment.NewLine + "SW:" + this.ScaledWindowSize.Print()
                //     + Environment.NewLine + "GC:" + this.GridOrigin.Print()
                //     + Environment.NewLine + "CP:" + this.Camera.Position.Print()
                //     + Environment.NewLine + "CZ:" + this.Camera.ZoomScaleFactor
                //     + Environment.NewLine + "MS:" + this.MapSize.Print()
                //     + Environment.NewLine + "GS:" + this.GridSizes[this.Orientation].Print()
                    // + Environment.NewLine + "RECT1:" + BASE_MAP_PANEL_SIZE.ToRectangle().Contains(this.BaseMouseVector)
                    // + Environment.NewLine + "RECT2:" + this.WindowState.Translate(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.BaseMouseVector)
                    // + Environment.NewLine + "RECT3:" + this.WindowState.Translate2(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.BaseMouseVector)
                    + Environment.NewLine + "RECT4:" + BASE_MAP_PANEL_SIZE.ToRectangle().Contains(this.ResolutionTranslatedMouseVector)
                    // + Environment.NewLine + "RECT5:" + this.WindowState.Translate(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.ResolutionTranslatedMouseVector)
                    // + Environment.NewLine + "RECT6:" + this.WindowState.Translate2(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.ResolutionTranslatedMouseVector)
                    // + Environment.NewLine + "RECT7:" + BASE_MAP_PANEL_SIZE.ToRectangle().Contains(this.WindowState.Translate2(this.BaseMouseVector))
                    // + Environment.NewLine + "RECT8:" + this.WindowState.Translate(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.WindowState.Translate2(this.BaseMouseVector))
                    // + Environment.NewLine + "RECT9:" + this.WindowState.Translate2(BASE_MAP_PANEL_SIZE).ToRectangle().Contains(this.WindowState.Translate2(this.BaseMouseVector))
                // + Environment.NewLine + "CT:" + this.ClientSizeTranslation.Print()
                // + Environment.NewLine + "AR1:" + this.VirtualSizeTranslation.Print()
                // + Environment.NewLine + "AR2:" + this.GraphicsDevice.Viewport.AspectRatio
                + Environment.NewLine + "Buffer:" + (this.Graphics.PreferredBackBufferWidth, this.Graphics.PreferredBackBufferHeight)
                + Environment.NewLine + "Viewport:" + (this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height)
                + Environment.NewLine + "Window:" + (this.Window.ClientBounds.Width, this.Window.ClientBounds.Height)
                // + Environment.NewLine + "Orientation: " + this.Orientation.Value
                // + Environment.NewLine + "Hexagons: " + this.HexagonMap.Count
                //     + Environment.NewLine + "MP:" + this.ScaledMapPanelRectangle
                // + Environment.NewLine + "Button:" + this.Button.Contains(this.ClientSizeTranslatedMouseVector)
                + Environment.NewLine + this.CalculatedDebug;
            this.SpriteBatch.DrawText(this.Font, log, new Vector2(10 + BASE_MAP_PANEL_WIDTH, 10 + BASE_SIDE_PANEL_HEIGHT * 1.25f).Floored(), scale: 1.5f);

            var cursorInfo = "Cursor:" + Environment.NewLine +
                ((this.CursorHexagon == null) ? "-none-" :
                    "Hex:" + this.GetCube(this.CursorHexagon) + Environment.NewLine +
                    "Position:" + this.GetPosition(this.CursorHexagon).Print()
                );

            var sourceInfo = "Selected:" + Environment.NewLine +
                ((this.SourceHexagon == null) ? "-none-" :
                    "Hex:" + this.GetCube(this.SourceHexagon) + Environment.NewLine +
                    "Position:" + this.GetPosition(this.SourceHexagon).Print()
                );

            this.SpriteBatch.DrawText(this.Font, sourceInfo, new Vector2(10 + BASE_MAP_PANEL_WIDTH * 1.25f, 10 + BASE_SIDE_PANEL_HEIGHT).Floored(), scale: 1.5f);
            this.SpriteBatch.DrawText(this.Font, cursorInfo, new Vector2(10 + BASE_MAP_PANEL_WIDTH, 10 + BASE_SIDE_PANEL_HEIGHT).Floored(), scale: 1.5f);

            // var rect1 = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, BASE_SIDE_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT);
            // var rect2 = new Rectangle(BASE_MAP_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT, BASE_SIDE_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT - BASE_SIDE_PANEL_HEIGHT);
            // this.SpriteBatch.DrawNinePatchRectangle(this.PanelTexture, rect1, 13, new Color(150, 200, 170, 255));
            // this.SpriteBatch.DrawNinePatchRectangle(this.PanelTexture, rect2, 13, new Color(150, 200, 170, 255));

            // var marginsize = 4;
            // var rect3 = new Rectangle(0, 0, BASE_MAP_PANEL_WIDTH + marginsize, marginsize);
            // var rect4 = new Rectangle(0, BASE_MAP_PANEL_HEIGHT - marginsize, BASE_MAP_PANEL_WIDTH + marginsize, marginsize);
            // var rect5 = new Rectangle(0, 0, marginsize, BASE_MAP_PANEL_HEIGHT);
            // var rect6 = new Rectangle(BASE_MAP_PANEL_WIDTH - marginsize, 0, marginsize * 2, BASE_MAP_PANEL_HEIGHT);
            // this.SpriteBatch.DrawNinePatchRectangle(this.PanelTexture, rect3, 4, new Color(150, 200, 170, 255), depth: 0.8f);
            // this.SpriteBatch.DrawNinePatchRectangle(this.PanelTexture, rect4, 4, new Color(150, 200, 170, 255), depth: 0.8f);
            // this.SpriteBatch.DrawNinePatchRectangle(this.PanelTexture, rect5, 2, new Color(150, 200, 170, 255), depth: 0.85f);
            // this.SpriteBatch.DrawRoundedRectangle(this.PanelTexture, rect6, 4, new Color(150, 200, 170, 255), depth: 0.95f);

            this.SpriteBatch.End();

            this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap);

            this.OnDraw?.Invoke(this.SpriteBatch);

            if (this.ExitConfirmation.IsActive)
            {
                this.SpriteBatch.DrawTo(this.BlankTexture, BASE_WINDOW_RECTANGLE, new Color(100, 100, 100, 100));
            }
            this.ExitConfirmation.Draw(this.SpriteBatch);

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

        // move to somewhere else? if only extension methods could be added statefully somehow
        protected Cube GetCube(Hexagon hexagon) =>
            hexagon.Cubes[this.Orientation];

        // move to somewhere else? if only extension methods could be added statefully somehow
        protected Vector2 GetPosition(Hexagon hexagon) =>
            hexagon.Positions[this.Orientation];

        // move to somewhere else?
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
            this.GridOrigin = Vector2.Floor(positionForCenterHexagon - centerHexagonPositionRelativeToOrigin);
        }

        // move to somewhere else?
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

        // move to somewhere else?
        protected Cube ToCubeCoordinates(Vector2 position)
        {
            var (mx, my) = position - this.GridOrigin - this.HexSize / 2;

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

        // move to somewhere else?
        protected Vector3 Lerp(Cube a, Cube b, float t) =>
            this.Lerp(a.ToVector3(), b.ToVector3(), t);

        // move to somewhere else?
        protected Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(this.Lerp(a.X, b.X, t), this.Lerp(a.Y, b.Y, t), this.Lerp(a.Z, b.Z, t));

        // move to somewhere else?
        protected float Lerp(float a, float b, float t) =>
            a + (b - a) * t;

        // move to somewhere else?
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

        // move to somewhere else?
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

        protected void RecalculateDebug()
        {
        }

        protected void ResizeWindowFromKeyboard(int newBackBufferWidth)
        {
            this.Graphics.PreferredBackBufferWidth = newBackBufferWidth;
            this.Graphics.ApplyChanges();
            this.OnWindowResize(this, default);
            // this.WindowState.Resize();
            this.Window.Position = new Point(
                (this.GraphicsDevice.Adapter.CurrentDisplayMode.Width / 2) - (this.Graphics.PreferredBackBufferWidth / 2),
                (this.GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2) - (this.Graphics.PreferredBackBufferHeight / 2));
        }

        protected void OnWindowResize(object sender, EventArgs e)
        {
            // In earlier version: when going to fullscreen, it is important not to change the backbuffer size,
            // because doing that makes it impossible to go back to windowed mode.
            // In current version: there is no bug with going back to windowed mode.
            // Still, with this logic there is no need to store windowed backbuffer size, 
            // so going back to windowed keeps old state without having to preserve it manually.
            if (!this.Graphics.IsFullScreen)
            {
                // Need to unsubscribe because this event would be triggered again by GraphicsDeviceManager.ApplyChanges
                this.Window.ClientSizeChanged -= this.OnWindowResize;

                // Set backbuffer to match client size
                // Note: right now width is prioritized over height, should check largest diff and apply that one
                // can use: var primary = x ? this.Window.ClientBounds.Width:Height < problem is need setter/getter
                if (this.Window.ClientBounds.Width != this.PreviousClientBounds.X)
                {
                    this.Graphics.PreferredBackBufferWidth = this.Window.ClientBounds.Width;
                    // Preserve aspect ratio
                    this.Graphics.PreferredBackBufferHeight = (int) (this.Window.ClientBounds.Width / BASE_ASPECT_RATIO);
                }
                else if (this.Window.ClientBounds.Height != this.PreviousClientBounds.Y)
                {
                    this.Graphics.PreferredBackBufferHeight = this.Window.ClientBounds.Height;
                    // Preserve aspect ratio
                    this.Graphics.PreferredBackBufferWidth = (int) (this.Window.ClientBounds.Height * BASE_ASPECT_RATIO);
                }

                this.Graphics.ApplyChanges();
                this.Window.ClientSizeChanged += this.OnWindowResize;
                this.PreviousClientBounds = this.Window.ClientBounds.Size.ToVector2();
            }
            // TODO: figure out way to make this.WindowState.Resize automatic
            // cannot subscribe it to Window.OnWindowResize because order of subscriber invocations is unreliable,
            // especially given in this method this class unsubscribes and resubscribes, making its invocation last.
            // Likely want a custom event that windowstate subscribes to (see Mogi inversion)
            // but it is not really elegant because ideally the OnResize event gets the already resized windowstate
            // so that other dependencies can use it, and it would be ugly to have two events?
            // It is also not nice to pass the GraphicsDeviceManager and GameWindow in the custom event
            // Potentially the OnResize could be used also for the WindowState, meaning it gets a reference to itself
            // It is a bit hackish - it also relies again on event invocation order: it should be first to get called
            this.WindowState.Resize();
            // Note: ClientSizeChanged gets raised twice when going to fullscreen, but only once when going back
        }

        #endregion

        // some ideas:
        // add mutable settings for stuff like SamplerState, maybe BlendState, panel color. 
        //      Can affect different SpriteBatch scopes (font, map, panel)
        //      Also in settings would be font size? May be tricky to fit it
        //      And whether to start in fullscreen -> meaning global settings should be stored in config file
        // add <> and {} to font
        // all form controls need keyboard support, like the blinking selector from pan engine
    }
}