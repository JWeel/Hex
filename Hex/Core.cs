using Extended.Extensions;
using Hex.Extensions;
using Hex.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Controls;
using Mogi.Extensions;
using Mogi.Framework;
using Mogi.Helpers;
using Mogi.Inversion;
using System.Text;

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
        private static readonly Vector2 BASE_WINDOW_INCREMENT = BASE_WINDOW_SIZE / 8; // used for keyboard-based scaling
        private static readonly Rectangle BASE_WINDOW_RECTANGLE = BASE_WINDOW_SIZE.ToRectangle();
        private static readonly Vector2 BASE_MAP_PANEL_SIZE = new Vector2(BASE_MAP_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT);
        private const int BASE_MAP_PADDING = 30;

        // no idea what these divisions are (possibly to account for hexagon borders sharing pixels?)
        // without them there are small gaps or overlap between hexagons, especially as coordinates increase
        // note that these are specific to 25/29 (pointy) and 29/25 (flatty) sizes!
        private const double SHORT_OVERLAP_DIVISOR = 1.80425; // this seems to be the offset for odd rows(pointy)/cols(flat)
        private const double LONG_OVERLAP_DIVISOR = 2.07137; // but this one no idea, doesn't seem to match any offset

        #endregion

        #region Constructors

        public Core()
        {
            this.Client = new ClientWindow(this.Window, new GraphicsDeviceManager(this), BASE_WINDOW_SIZE);
        }

        #endregion

        #region Data Members

        public PhasedEvent<GameTime> OnUpdate { get; set; }
        public PhasedEvent<SpriteBatch> OnDraw { get; set; }

        protected ClientWindow Client { get; }
        protected SpriteBatch SpriteBatch { get; set; }

        protected FramerateHelper Framerate { get; set; }
        protected InputHelper Input { get; set; }
        protected TilemapHelper Tilemap { get; set; }

        protected Architect Architect { get; set; }

        protected SpriteFont Font { get; set; }
        protected Texture2D BlankTexture { get; set; }

        /// <summary> Mouse position relative to window. </summary>
        protected Vector2 BaseMouseVector { get; set; }

        /// <summary> Resolution translation is needed only when in windowed mode client resolution does not match virtual resolution (in fullscreen mode they always match). </summary>
        protected Vector2 ResolutionTranslatedMouseVector { get; set; }

        /// <summary> Tilemap uses a camera, therefore translation is needed when the camera is zoomed. </summary>
        protected Vector2 TilemapTranslatedMouseVector { get; set; }

        protected string CalculatedDebug;

        #endregion

        #region Overridden Methods

        protected override void Initialize()
        {
            // Client is initalized first so that anything in LoadContent can safely depend on it
            this.Client.Initialize();

            // base.Initialize does the following: 
            // - call ApplyChanges on the GraphicsDevicemanager
            // - call Initialize on all attached GameComponents
            // - call LoadContent
            // This project does not use GameComponents, and ClientWindow.Initialize already called ApplyChanges.
            // Therefore LoadContent can be called here directly.
            this.LoadContent();
        }

        protected override void LoadContent()
        {
            this.Content.RootDirectory = CONTENT_ROOT_DIRECTORY;
            this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.BlankTexture = new Texture2D(this.GraphicsDevice, width: 1, height: 1);
            this.BlankTexture.SetData(new[] { Color.White });

            this.Font = this.Content.Load<SpriteFont>("Alphabet/saga");

            var dependency = DependencyHelper.Create(this);
            dependency.Register(this.Content);
            dependency.Register(this.Client);
            dependency.Register(this.SpriteBatch);
            dependency.Register(this.BlankTexture);
            dependency.Register(this.Font);
            dependency.Register<FramerateHelper>();
            this.Input = dependency.Register<InputHelper>();
            this.Architect = dependency.Register<Architect>();
            this.Tilemap = dependency.Register<TilemapHelper>();

            this.Tilemap.Arrange(BASE_WINDOW_SIZE, BASE_MAP_PADDING);
            // this.Tilemap.Orientation.Advance();
            // this.Tilemap.Orientation.Advance();

            this.PanelTexture = this.Content.Load<Texture2D>("panel");
            this.YesTexture = this.Content.Load<Texture2D>("buttonYes");
            this.NoTexture = this.Content.Load<Texture2D>("buttonNo");

            // var exitConfirmationPanelSize = new Vector2(256, 144);
            var exitConfirmationPanelSize = new Vector2(400, 100);
            var exitConfirmationPanelLocation = (BASE_WINDOW_SIZE / 2) - (exitConfirmationPanelSize / 2);
            var exitConfirmationPanelRectangle = new Rectangle(exitConfirmationPanelLocation.ToPoint(), exitConfirmationPanelSize.ToPoint());
            this.ExitConfirmation = new Panel(exitConfirmationPanelRectangle);
            this.ExitConfirmation.Append(new Basic(BASE_WINDOW_RECTANGLE, this.BlankTexture, new Color(100, 100, 100, 100)));
            this.ExitConfirmation.Append(new Patch(exitConfirmationPanelRectangle, this.PanelTexture, 13));

            var exitConfirmationText = "Are you sure you want to quit?";
            var exitConformationTextScale = 1.5f;
            var exitConformationTextSize = this.Font.MeasureString(exitConfirmationText) * exitConformationTextScale;
            var exitConformationTextLocation = (BASE_WINDOW_SIZE / 2) - (exitConformationTextSize / 2) - new Vector2(0, 30);
            this.ExitConfirmation.Append(new Label(new Rectangle(exitConformationTextLocation.ToPoint(), exitConformationTextSize.ToPoint()), this.Font, exitConfirmationText, exitConformationTextScale));

            var noYesButtonSize = new Vector2(40);
            var noButtonLocation = (BASE_WINDOW_SIZE / 2) - new Vector2(noYesButtonSize.X, 0) * 1.5f;
            var noButton = new Button(new Rectangle(noButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.NoTexture, new Color(200, 0, 0));
            noButton.WithInput(this.Input);
            noButton.OnClick += button =>
            {
                this.ExitConfirmation.Toggle();
                this.ExitConfirmation.SetPrevent(this.ExitConfirmation.IsActive);
            };
            this.ExitConfirmation.Append(noButton);

            var yesButtonLocation = (BASE_WINDOW_SIZE / 2) + new Vector2(noYesButtonSize.X, 0) / 1.5f;
            var yesButton = new Button(new Rectangle(yesButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.YesTexture, new Color(0, 200, 0));
            yesButton.WithInput(this.Input);
            yesButton.OnClick += button => this.Exit();
            this.ExitConfirmation.Append(yesButton);

            this.Log = new StringBuilder();
            this.Side = new Panel(new Rectangle());
            this.Side.Append(new Patch(new Rectangle(970, 10, 300, 700), this.PanelTexture, 13, Color.LightSlateGray));
            this.SideLabel = new Label(new Rectangle(980, 500, 280, 200), this.Font, () => this.Log.ToString());
            this.Side.Append(this.SideLabel);

            var toggleSize = new Vector2(40);
            var toggleLocation = new Vector2(1220, 20);
            this.Toggle = new Button(new Rectangle(toggleLocation.ToPoint(), toggleSize.ToPoint()), this.PanelTexture, Color.BurlyWood);
            this.Toggle.WithInput(this.Input);
            this.Toggle.OnClick += button => this.Side.Toggle();

            var exitWrapper = new PhasedWrapper<CriticalUpdate, MenuDraw>(this.ExitConfirmation.Update, this.ExitConfirmation.Draw);
            this.Attach(exitWrapper);
            this.Attach(this.Side);
            this.Attach(this.Toggle);
        }
        Texture2D PanelTexture;
        Texture2D YesTexture;
        Texture2D NoTexture;
        Panel ExitConfirmation;
        Button Toggle;
        Panel Side;
        Label SideLabel;
        StringBuilder Log;

        protected override void Update(GameTime gameTime)
        {
            this.OnUpdate?.Invoke<CriticalUpdate>(gameTime);

            // class Interrupter, simply contains a boolean that says stop processing
            // check it after critical update, if true return

            if (this.Input.KeyPressed(Keys.Escape))
            {
                this.ExitConfirmation.Toggle();
                this.ExitConfirmation.SetPrevent(this.ExitConfirmation.IsActive);
            }
            if (this.ExitConfirmation.IsActive)
                return;

            this.IsMouseVisible = true;

            if (this.Input.KeyPressed(Keys.F11) || (this.Input.KeyPressed(Keys.Enter) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt)))
                this.Client.ToggleFullscreen();

            // if (this.Input.KeyPressed(Keys.Enter))
            //     this.Tilemap.Arrange(BASE_WINDOW_SIZE, BASE_MAP_PADDING);

            if (this.Input.KeyPressed(Keys.C))
                // TODO move to input handler of tilemap
                this.Tilemap.Center();

            if (!this.Client.IsFullscreen)
            {
                if (this.Input.KeyPressed(Keys.D0))
                    this.Client.CenterWindow();
                if (this.Input.KeyPressed(Keys.R))
                    this.Client.Resize(BASE_WINDOW_SIZE);
                if (this.Input.KeyPressed(Keys.OemPlus))
                    this.Client.Resize(this.Client.CurrentResolution + BASE_WINDOW_INCREMENT);
                if (this.Input.KeyPressed(Keys.OemMinus))
                    this.Client.Resize(this.Client.CurrentResolution - BASE_WINDOW_INCREMENT);
            }


            if (this.Input.MouseMoved())
            {
                this.BaseMouseVector = this.Input.CurrentVirtualMouseVector;
                this.ResolutionTranslatedMouseVector = this.Input.CurrentVirtualMouseVector;
                this.TilemapTranslatedMouseVector = this.Tilemap.Translate(this.ResolutionTranslatedMouseVector);
            }

            // if (this.Input.KeyPressed(Keys.Left))
            //     this.GridOrigin -= new Vector2(100, 0);
            // if (this.Input.KeyPressed(Keys.Right))
            //     this.GridOrigin += new Vector2(100, 0);
            // if (this.Input.KeyPressed(Keys.Up))
            //     this.GridOrigin -= new Vector2(0, 100);
            // if (this.Input.KeyPressed(Keys.Down))
            //     this.GridOrigin += new Vector2(0, 100);

            // if (this.Input.KeyPressed(Keys.I))
            //     this.Camera.CenterOn(this.GetPosition(this.CenterHexagon));

            // this.Camera.HandleInput(this.Input);
            // this.Tilemap.Update(gameTime);
            this.OnUpdate?.Invoke<NormalUpdate>(gameTime);

            if (this.Input.KeyPressed(Keys.B))
                this.RecalculateDebug();

            if (this.Side.IsActive)
            {
                this.Log.Clear();
                this.Log.AppendLine($"M1: {this.BaseMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M2: {this.ResolutionTranslatedMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M3: {this.TilemapTranslatedMouseVector.PrintRounded()}");
                // this.Log.AppendLine($"Current: {this.Client.CurrentResolution}");
                // this.Log.AppendLine($"Window: {this.Window.ClientBounds.Size}");
                this.Log.AppendLine($"Cursor: {this.Tilemap.CursorHexagon?.Into(this.Tilemap.Info).Coordinates.ToString() ?? "n/a"}");
                this.Log.AppendLine($"Source: {this.Tilemap.SourceHexagon?.Into(this.Tilemap.Info).Coordinates.ToString() ?? "n/a"}");
                this.Log.AppendLine($"Hexagons: {this.Tilemap.HexagonMap.Count}");
                this.Log.AppendLine($"Fullscreen: {this.Client.IsFullscreen}");
                // this.Log.AppendLine($"Tilemap: {this.Tilemap.MapSize}");
                // this.Log.AppendLine($"Grid: {this.Tilemap.GridSize}");
                // this.Log.AppendLine($"Padding: {this.Tilemap.TilemapPadding}");
                // this.Log.AppendLine($"Orientation: {this.Tilemap.Orientation}");
                this.Log.AppendLine(this.CalculatedDebug);

                // var cursorInfo = "Cursor:" + Environment.NewLine +
                //     ((this.Tilemap.CursorHexagon == null) ? "-none-" : this.Tilemap
                //         .Info(this.Tilemap.CursorHexagon)
                //         .Into(info => "Hex:" + info.Coordinates + Environment.NewLine + "Position:" + info.Position));

                // var sourceInfo = "Selected:" + Environment.NewLine +
                //     ((this.Tilemap.SourceHexagon == null) ? "-none-" : this.Tilemap
                //         .Info(this.Tilemap.SourceHexagon)
                //         .Into(info => "Hex:" + info.Coordinates + Environment.NewLine + "Position:" + info.Position));
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.Black);


            // SpriteSortMode.FrontToBack
            this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, transformMatrix: this.Tilemap.TranslationMatrix);

            this.SpriteBatch.DrawTo(this.BlankTexture, this.Tilemap.MapSize.ToRectangle(), Color.DarkSlateGray);//, depth: 0.15f);

            this.OnDraw?.Invoke<BackgroundDraw>(this.SpriteBatch);
            this.SpriteBatch.End();


            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp);

            // var mapToPanelSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, 1, BASE_WINDOW_HEIGHT);
            // var panelToLogSeparator = new Rectangle(BASE_MAP_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT, BASE_SIDE_PANEL_WIDTH, 1);
            // var panelOverlay = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, BASE_SIDE_PANEL_WIDTH, BASE_WINDOW_HEIGHT);
            // this.SpriteBatch.DrawTo(this.BlankTexture, mapToPanelSeparator, Color.BurlyWood, depth: 0.9f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, panelToLogSeparator, Color.BurlyWood, depth: 0.9f);
            // this.SpriteBatch.DrawTo(this.BlankTexture, panelOverlay, Color.SlateGray, depth: 0.85f);

            this.OnDraw?.Invoke<ForegroundDraw>(this.SpriteBatch);
            this.SpriteBatch.End();


            this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap);
            this.OnDraw?.Invoke<MenuDraw>(this.SpriteBatch);
            this.SpriteBatch.End();
        }

        protected override bool BeginDraw()
        {
            this.GraphicsDevice.SetRenderTarget(this.Client.RenderTarget);
            return base.BeginDraw();
        }

        protected override void EndDraw()
        {
            this.GraphicsDevice.SetRenderTarget(null);
            this.SpriteBatch.Begin();
            this.SpriteBatch.Draw(this.Client.RenderTarget, this.GraphicsDevice.Viewport.Bounds, Color.White);
            this.SpriteBatch.End();
            base.EndDraw();
        }

        #endregion

        #region Helper Methods

        protected void RecalculateDebug()
        {
        }

        #endregion

        // some ideas:
        // add mutable settings for stuff like SamplerState, maybe BlendState, panel color. 
        //      Can affect different SpriteBatch scopes (font, map, panel)
        //      Also in settings would be font size? May be tricky to fit it
        //      And whether to start in fullscreen -> meaning global settings should be stored in config file
        // fullscreen just borderless mode? not sure of impact on non-windows
        // all form controls need keyboard support, like the blinking selector from pan engine
    }
}