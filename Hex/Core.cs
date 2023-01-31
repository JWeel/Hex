using Extended.Extensions;
using Extended.Patterns;
using Hex.Auxiliary;
using Hex.Enums;
using Hex.Extensions;
using Hex.Helpers;
using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mogi.Controls;
using Mogi.Extensions;
using Mogi.Framework;
using Mogi.Helpers;
using Mogi.Inversion;
using Mogi.Scopes;
using System.IO;
using System.Text;

namespace Hex
{
    public class Core : Game, IRoot
    {
        #region Constants

        private const string CONTENT_ROOT_DIRECTORY = "Content";
        private const string CONTENT_SUB_DIRECTORY_LEVEL = "Level";

        private const int BASE_WINDOW_WIDTH = 1280;
        private const int BASE_WINDOW_HEIGHT = 720;
        private const int BASE_MAP_PANEL_WIDTH = 790; // 1280 / 1.618 = 791.10 : using 790 for even number
        private const int BASE_MAP_PANEL_HEIGHT = BASE_WINDOW_HEIGHT;
        private const int BASE_SIDE_PANEL_WIDTH = BASE_WINDOW_WIDTH - BASE_MAP_PANEL_WIDTH;
        private const int BASE_SIDE_PANEL_HEIGHT = 445; // 720 / 1.618 = 444.99
        private static readonly Vector2 BASE_WINDOW_SIZE = new Vector2(BASE_WINDOW_WIDTH, BASE_WINDOW_HEIGHT);
        private static readonly Vector2 BASE_WINDOW_INCREMENT = BASE_WINDOW_SIZE / 8; // used for keyboard-based scaling
        private static readonly Rectangle BASE_WINDOW_RECTANGLE = BASE_WINDOW_SIZE.ToRectangle();
        private static readonly Vector2 BASE_MAP_PANEL_SIZE = new Vector2(BASE_MAP_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT);

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

        protected InputHelper<CriticalUpdate> Input { get; set; }
        protected ConfigurationHelper Configuration { get; set; }
        protected StageHelper Stage { get; set; }

        protected Panel<NormalUpdate, ControlDraw> Storybook { get; set; }
        protected Panel<NormalUpdate, ControlDraw> Designer { get; set; }

        protected SpriteFont Font { get; set; }
        protected Texture2D BlankTexture { get; set; }

        /// <summary> Mouse position relative to window. </summary>
        protected Vector2 BaseMouseVector { get; set; }

        /// <summary> Resolution translation is needed when client resolution does not match virtual resolution. </summary>
        protected Vector2 ClientResolutionTranslatedMouseVector { get; set; }

        /// <summary> Stage translation is needed when stage camera is zoomed. </summary>
        protected Vector2 StageCameraTranslatedMouseVector { get; set; }

        /// <summary> A container of rasterization settings that can be used in spritebatch drawing to enable scissoring. </summary>
        /// <remarks> When scissoring is enabled, a rectangle can be set to <see cref="GraphicsDevice.ScissorRectangle"/>, which will limit all drawing to inside the rectangle. Textures outside of it are culled (not drawn). <para/> Without these settings, the scissor rectangle is ignored. </remarks>
        protected RasterizerState ScissorRasterizer { get; } = new RasterizerState { ScissorTestEnable = true };

        /// <summary> Tracks the current state of the application, which determines the controls to show. </summary>
        protected Cyclic<State> State { get; set; }

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
            this.Font = this.Content.Load<SpriteFont>("Graphics/Alphabet/saga");
            this.State = Cyclic.Enum<State>();
            this.State.OnChange += this.ChangeState;

            var dependency = Dependency.Start(this);
            dependency.Register(this.Content);
            dependency.Register(this.Client);
            dependency.Register(this.SpriteBatch);
            dependency.Register(this.BlankTexture);
            dependency.Register(this.Font);
            this.Input = dependency.Register<InputHelper<CriticalUpdate>>();
            this.Configuration = dependency.Register<ConfigurationHelper>();
            this.Stage = dependency.Register<StageHelper>();

            // TODO implement loading from config.ini file
            this.Configuration.Load();

            if (this.Configuration.StartInFullscreen)
                this.Client.ToggleFullscreen();

            // var stageContainer = new Rectangle(new Point(240, 50), (BASE_WINDOW_SIZE / 1.55f).ToPoint());
            var stageContainer = BASE_MAP_PANEL_SIZE.ToRectangle();
            this.Stage.Arrange(stageContainer, this.GetStagePath("plateau"));

            this.Storybook = new Panel<NormalUpdate, ControlDraw>();
            this.Designer = new Panel<NormalUpdate, ControlDraw>();

            // temporary panel stuff
            {
                this.PanelTexture = this.Content.Load<Texture2D>("Graphics/panel");
                this.YesTexture = this.Content.Load<Texture2D>("Graphics/buttonYes");
                this.NoTexture = this.Content.Load<Texture2D>("Graphics/buttonNo");
                var borderTexture = this.Content.Load<Texture2D>("Graphics/border1");
                var overlayTexture = this.Content.Load<Texture2D>("Graphics/overlay");

                this.Storybook.Append(new Patch<NormalUpdate, ControlDraw>(BASE_MAP_PANEL_SIZE.ToRectangle(), this.PanelTexture, 11, Color.SandyBrown));
                this.Storybook.Append(new Label<NormalUpdate, ControlDraw>(new Rectangle(100, 80, 0, 0), this.Font, "Welcome to the world."));
                this.Storybook.Append(new Label<NormalUpdate, ControlDraw>(new Rectangle(100, 140, 0, 0), this.Font, "In this world you will face many challenges. Pity they were all in vain.\nWho knows what will happen next."));
                this.Attach(this.Storybook);

                this.Designer.Append(new Patch<NormalUpdate, ControlDraw>(BASE_MAP_PANEL_SIZE.ToRectangle(), this.PanelTexture, 11, Color.ForestGreen));
                var portraitControl = new Basic<NormalUpdate, ControlDraw>(new Rectangle(100, 100, 100, 100), this.BlankTexture, Color.Crimson);
                this.Designer.Append(portraitControl);
                this.Attach(this.Designer);

                this.Attach(new PhasedUpdateWrapper<NormalUpdate>(gameTime =>
                {
                    if (this.Input.KeyPressed(Keys.OemPeriod))
                        portraitControl.Recolor(Color.AntiqueWhite);
                    if (this.Input.KeyPressed(Keys.OemQuestion))
                        portraitControl.Recolor(Color.PeachPuff);
                }));

                // move to DossierHandler
                var dossierPanel = new Panel<NormalUpdate, ControlDraw>(isActive: true);
                this.Attach(dossierPanel);

                // use different panel for different type of top-side-panel
                // then toggle the one that is being used, and untoggle the others
                var upperSidePanelContainer = new Rectangle(BASE_MAP_PANEL_WIDTH, 0, BASE_SIDE_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT);
                var actorPanel = new Panel<NormalUpdate, ControlDraw>(isActive: true);
                dossierPanel.Append(actorPanel);
                actorPanel.Append(new Patch<NormalUpdate, ControlDraw>(upperSidePanelContainer, this.PanelTexture, 10, Color.BlanchedAlmond));

                // would be nice if positions were relative to panel
                actorPanel.Append(new Basic<NormalUpdate, ControlDraw>(BASE_MAP_PANEL_WIDTH + 40, 40, 70, 70, this.BlankTexture, Color.WhiteSmoke));

                var actorPortraitBasicBlank = new Basic<NormalUpdate, ControlDraw>(BASE_MAP_PANEL_WIDTH + 42, 42, 66, 66, this.BlankTexture, new Color(201, 185, 161));
                var actorPortraitBasic = new Basic<NormalUpdate, ControlDraw>(BASE_MAP_PANEL_WIDTH + 42, 42, 66, 66, this.BlankTexture, new Color(201, 185, 161));
                actorPortraitBasic.Toggle();
                actorPanel.Append(actorPortraitBasicBlank);
                actorPanel.Append(actorPortraitBasic);
                this.Stage.OnSourceActorChange += actor =>
                {
                    if (actor == null)
                    {
                        if (actorPortraitBasic.IsActive)
                            actorPortraitBasic.Toggle();
                    }
                    else
                    {
                        // the texture could be a func so it animates, but portrait might not need to be animated...
                        actorPortraitBasic.Retexture(actor.Texture);
                        if (!actorPortraitBasic.IsActive)
                            actorPortraitBasic.Toggle();
                    }
                };

                var lowerSidePanelContainer = new Rectangle(BASE_MAP_PANEL_WIDTH, BASE_SIDE_PANEL_HEIGHT, BASE_SIDE_PANEL_WIDTH, BASE_MAP_PANEL_HEIGHT - BASE_SIDE_PANEL_HEIGHT);
                dossierPanel.Append(new Basic<NormalUpdate, ControlDraw>(lowerSidePanelContainer, this.BlankTexture, new Color(162, 178, 204))
                    .WithInput(this.Input)
                    .With(control =>
                    {
                        control.OnMouseEnter += x => x.Recolor(Color.MediumSlateBlue);
                        control.OnMouseLeave += x => x.Recolor(new Color(162, 178, 204));
                    }));


                var exitConfirmationPanelSize = new Vector2(400, 100);
                var exitConfirmationPanelLocation = (BASE_WINDOW_SIZE / 2) - (exitConfirmationPanelSize / 2);
                var exitConfirmationPanelRectangle = new Rectangle(exitConfirmationPanelLocation.ToPoint(), exitConfirmationPanelSize.ToPoint());
                this.ExitConfirmation = new Panel<CriticalUpdate, ControlDraw>();
                this.ExitConfirmation.Append(new Basic<CriticalUpdate, ControlDraw>(BASE_WINDOW_RECTANGLE, this.BlankTexture, new Color(100, 100, 100, 100)));
                this.ExitConfirmation.Append(new Patch<CriticalUpdate, ControlDraw>(exitConfirmationPanelRectangle, this.PanelTexture, 13));

                var exitConfirmationText = "Are you sure you want to quit?";
                var exitConformationTextScale = 1.5f;
                var exitConformationTextSize = this.Font.MeasureString(exitConfirmationText) * exitConformationTextScale;
                var exitConformationTextLocation = (BASE_WINDOW_SIZE / 2) - (exitConformationTextSize / 2) - new Vector2(0, 30);
                this.ExitConfirmation.Append(new Label<CriticalUpdate, ControlDraw>(new Rectangle(exitConformationTextLocation.ToPoint(), exitConformationTextSize.ToPoint()), this.Font, exitConfirmationText, exitConformationTextScale));

                var noYesButtonSize = new Vector2(40);
                var noButtonLocation = (BASE_WINDOW_SIZE / 2) - new Vector2(noYesButtonSize.X, 0) * 1.5f;
                var noButton = new Button<CriticalUpdate, ControlDraw>(new Rectangle(noButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.NoTexture, new Color(200, 0, 0));
                noButton.WithInput(this.Input);
                noButton.OnClick += button =>
                {
                    this.ExitConfirmation.Toggle();
                    this.ExitConfirmation.SetPrevent(this.ExitConfirmation.IsActive);
                };
                this.ExitConfirmation.Append(noButton);

                var yesButtonLocation = (BASE_WINDOW_SIZE / 2) + new Vector2(noYesButtonSize.X, 0) / 1.5f;
                var yesButton = new Button<CriticalUpdate, ControlDraw>(new Rectangle(yesButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.YesTexture, new Color(0, 200, 0));
                yesButton.WithInput(this.Input);
                yesButton.OnClick += button => this.Exit();
                this.ExitConfirmation.Append(yesButton);

                this.Log = new StringBuilder();
                this.Side = new Panel<NormalUpdate, ControlDraw>();
                this.Side.Append(new Patch<NormalUpdate, ControlDraw>(new Rectangle(970, 10, 300, 700), this.PanelTexture, 13, Color.BurlyWood));
                this.SideLabel = new Label<NormalUpdate, ControlDraw>(new Rectangle(980, 500, 280, 200), this.Font, () => this.Log.ToString());
                this.Side.Append(this.SideLabel);

                var toggleSize = new Vector2(40);
                var toggleLocation = new Vector2(1220, 20);
                this.Toggle = new Button<NormalUpdate, ControlDraw>(new Rectangle(toggleLocation.ToPoint(), toggleSize.ToPoint()), this.PanelTexture, Color.BurlyWood);
                this.Toggle.WithInput(this.Input);
                this.Toggle.OnClick += button => this.Side.Toggle();

                var overlayContainer = BASE_WINDOW_RECTANGLE;
                var overlayBasic = new Basic<NormalUpdate, ControlDraw>(overlayContainer, overlayTexture);
                this.Attach(overlayBasic);

                this.Attach(this.Side);
                this.Attach(this.Toggle);
                this.Attach(this.ExitConfirmation);
            }

            var clientWindowWrapper = new PhasedUpdateWrapper<NormalUpdate>(gametime =>
            {
                if (this.Input.KeyPressed(Keys.F11) || (this.Input.KeyPressed(Keys.Enter) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt)))
                    this.Client.ToggleFullscreen();

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
            });
            this.Attach(clientWindowWrapper);

            this.Attach(new PhasedUpdateWrapper<NormalUpdate>(gametime =>
            {
                if (this.Input.KeyPressed(Keys.OemQuestion))
                    this.Configuration.UseStickyCameraMovement = !this.Configuration.UseStickyCameraMovement;
            }));

            this.Attach(new PhasedUpdateWrapper<CriticalUpdate>(gametime => Static.Memo.Clear()));

            // move this above the attaching of map-area panels to hide it on non-stage state
            dependency.Register<FramerateHelper<CriticalUpdate, ControlDraw>>();

            this.State.Set(Enums.State.Storybook);
            // in case state is default state need to force change
            this.ChangeState(default, this.State);
        }
        Texture2D PanelTexture;
        Texture2D YesTexture;
        Texture2D NoTexture;
        Panel<CriticalUpdate, ControlDraw> ExitConfirmation;
        Button<NormalUpdate, ControlDraw> Toggle;
        Panel<NormalUpdate, ControlDraw> Side;
        Label<NormalUpdate, ControlDraw> SideLabel;
        StringBuilder Log;

        protected override void Update(GameTime gameTime)
        {
            this.OnUpdate?.Invoke<CriticalUpdate>(gameTime);

            // TODO: class Interrupter, simply contains a boolean that says stop processing
            // check it after critical update, if true return

            if (this.Input.KeyPressed(Keys.Escape))
            {
                this.ExitConfirmation.Toggle();
                // this.ExitConfirmation.SetPrevent(this.ExitConfirmation.IsActive);
            }
            if (this.ExitConfirmation.IsActive)
                return;

            this.IsMouseVisible = true;

            if (this.Input.KeyPressed(Keys.F10))
                this.Stage.Arrange(this.Stage.Container, this.GetStagePath("valley"));
            if (this.Input.KeyPressed(Keys.F9))
                this.Stage.Arrange(this.Stage.Container, this.GetStagePath("plateau"));
            if (this.Input.KeyPressed(Keys.F8))
                this.Stage.Arrange(this.Stage.Container, this.GetStagePath("grove"));
            if (this.Input.KeyPressed(Keys.F7))
                this.Stage.Arrange(this.Stage.Container, Shape.Hexagon);
            if (this.Input.KeyPressed(Keys.F6))
                this.Stage.Arrange(this.Stage.Container, Shape.Rectangle);
            if (this.Input.KeyPressed(Keys.F5))
                this.Stage.Arrange(this.Stage.Container, Shape.Triangle);
            if (this.Input.KeyPressed(Keys.F4))
                this.Stage.Arrange(this.Stage.Container, Shape.Parallelogram);
            if (this.Input.KeyPressed(Keys.F3))
                this.Stage.Arrange(this.Stage.Container, Shape.Line);

            if (this.Input.KeyPressed(Keys.Tab))
                this.Side.Toggle();

            if (this.Input.KeyPressed(Keys.OemOpenBrackets))
                this.State.Advance();
            if (this.Input.KeyPressed(Keys.OemCloseBrackets))
                this.State.Reverse();

            if (this.Input.MouseMoved())
            {
                this.BaseMouseVector = this.Input.CurrentMouseVector;
                this.ClientResolutionTranslatedMouseVector = this.Input.CurrentVirtualMouseVector;
                this.StageCameraTranslatedMouseVector = this.ClientResolutionTranslatedMouseVector.Transform(this.Stage.TranslationMatrix.Invert());
            }

            this.OnUpdate?.Invoke<NormalUpdate>(gameTime);

            if (this.Side.IsActive)
            {
                this.Log.Clear();
                // this.Log.AppendLine($"M1: {this.BaseMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M2: {this.ClientResolutionTranslatedMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M3: {this.StageCameraTranslatedMouseVector.PrintRounded()}");
                // this.Log.AppendLine($"Current: {this.Client.CurrentResolution}");
                // this.Log.AppendLine($"Window: {this.Window.ClientBounds.Size}");
                this.Log.AppendLine($"Cursor: {this.Stage.FocusTile?.Into(x => $"{x.Cube} E:{x.Elevation}") ?? "n/a"}");
                // this.Log.AppendLine($"Tiles: {this.Stage.TileCount}");
                // this.Log.AppendLine($"Interval: {this.Stage.TilemapRotationInterval}");
                // this.Log.AppendLine($"Container: {this.Stage.Container.Location}{this.Stage.Container.Size}");
                // this.Log.AppendLine($"Camera: {this.Stage.Camera.Position}");
                // this.Log.AppendLine($"{this.Stage.TilemapDebug}");
                // this.Log.AppendLine($"Fullscreen: {this.Client.IsFullscreen}");
                this.Log.AppendLine($"Faction: {this.Stage.SourceFaction?.Name ?? "n/a"}");
                this.Log.AppendLine(Static.Memo.ToString());
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.Black);

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap);
            this.SpriteBatch.DrawTo(this.BlankTexture, this.Stage.Container, Color.Ivory);
            this.SpriteBatch.End();

            using (new ScissorScope(this.GraphicsDevice, this.Stage.Container))
            {
                // try other SamplerStates
                this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointWrap,
                    rasterizerState: this.ScissorRasterizer, transformMatrix: this.Stage.TranslationMatrix);
                this.OnDraw?.Invoke<BackgroundDraw>(this.SpriteBatch);
                this.SpriteBatch.End();

                this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp,
                    rasterizerState: this.ScissorRasterizer, transformMatrix: this.Stage.TranslationMatrix);
                this.OnDraw?.Invoke<ForegroundDraw>(this.SpriteBatch);
                this.SpriteBatch.End();
            }

            this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap);
            this.OnDraw?.Invoke<ControlDraw>(this.SpriteBatch);
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

        protected string GetStagePath(string name)
        {
            return Path.Combine(CONTENT_ROOT_DIRECTORY, CONTENT_SUB_DIRECTORY_LEVEL, Path.ChangeExtension(name, ".csv"));
        }

        protected void ChangeState(State oldState, State newState)
        {
            IActivate Switch(State state) => state switch 
            {
                Enums.State.Stage => this.Stage,
                Enums.State.Designer => this.Designer,
                Enums.State.Storybook => this.Storybook,
                _ => throw state.Invalid()
            };
            Switch(oldState).Deactivate();
            Switch(newState).Activate();
        }

        #endregion

        // some ideas:
        // add to ConfigurationHelper stuff like SamplerState, maybe BlendState, panel color. 
        //      Can affect different SpriteBatch scopes (font, map, panel)
        //      Also in settings would be font size? May be tricky to fit it
        // fullscreen just borderless mode? --> not sure of impact on non-windows
        // all form controls need keyboard support, like the blinking selector from pan engine
        // slow pulse button press -> press and held, after 1 second pulse every .10? until released
        // font helper -> exposes Font to dependencies and can switch to other fonts
        // make abstract Tile -> can be hexagon or rectangle, maybe triangle
        // content zipped, use custom ContentManager that handles zipped
        // move all phases out of Mogi and make the classes inside that depend on them generic
        //      use documentation to give example of what phase looks like, or maybe add ExamplePhase : IPhase
    }
}