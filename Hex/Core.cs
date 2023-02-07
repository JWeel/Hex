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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            this.Configuration = new ConfigurationHelper();
            this.Client = new ClientWindow(this.Window, new GraphicsDeviceManager(this), BASE_WINDOW_SIZE, this.Configuration.StartInFullscreen);
        }

        #endregion

        #region Data Members

        public PhasedEvent<GameTime> OnUpdate { get; set; }
        public PhasedEvent<SpriteBatch> OnDraw { get; set; }

        protected ConfigurationHelper Configuration { get; set; }
        protected ClientWindow Client { get; }
        protected SpriteBatch SpriteBatch { get; set; }

        protected InputHelper<CriticalUpdate> Input { get; set; }
        protected Panel<NormalUpdate, ControlDraw> Storybook { get; set; }
        protected Panel<NormalUpdate, ControlDraw> Designer { get; set; }
        protected ScenarioHelper Scenario { get; set; }
        protected OperationHelper Operation { get; set; }
        protected EncounterHelper Encounter { get; set; }
        protected ExpansionHelper Expansion { get; set; }

        protected JournalHelper Journal { get; set; }

        protected SpriteFont Font { get; set; }

        /// <summary> Contains a texture of 1 white pixel that can be used to draw arbitrary rectangles in solid color. </summary>
        protected Texture2D BlankTexture { get; set; }

        /// <summary> Mouse position relative to window. </summary>
        protected Vector2 BaseMouseVector { get; set; }

        /// <summary> Resolution translation is needed when client resolution does not match virtual resolution. </summary>
        protected Vector2 ClientResolutionTranslatedMouseVector { get; set; }

        /// <summary> Scenario translation is needed when scenario camera is zoomed. </summary>
        protected Vector2 ScenarioCameraTranslatedMouseVector { get; set; }

        /// <summary> A container of rasterization settings that can be used in spritebatch drawing to enable scissoring. </summary>
        /// <remarks> When scissoring is enabled, a rectangle can be set to <see cref="GraphicsDevice.ScissorRectangle"/>, which will limit all drawing to inside the rectangle. Textures outside of it are culled (not drawn). <para/> Without these settings, the scissor rectangle is ignored. </remarks>
        protected RasterizerState ScissorRasterizer { get; } = new RasterizerState { ScissorTestEnable = true };

        /// <summary> Tracks the current state of the application, which determines the controls to show. </summary>
        protected Cyclic<State> State { get; set; }

        // these can be used to test different looks. eventually may want to move it to a config
        protected Cyclic<KeyValuePair<string, SamplerState>> SamplerStateCycle = Cyclic.FromValues(SAMPLER_STATE_MAP.ToArray());
        protected static readonly IDictionary<string, SamplerState> SAMPLER_STATE_MAP = new Dictionary<string, SamplerState>
        {
            { nameof(SamplerState.AnisotropicClamp), SamplerState.AnisotropicClamp },
            { nameof(SamplerState.AnisotropicWrap), SamplerState.AnisotropicWrap },
            { nameof(SamplerState.LinearClamp), SamplerState.LinearClamp },
            { nameof(SamplerState.LinearWrap), SamplerState.LinearWrap },
            { nameof(SamplerState.PointClamp), SamplerState.PointClamp },
            { nameof(SamplerState.PointWrap), SamplerState.PointWrap },
        };

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
            this.BlankTexture = new Texture2D(this.GraphicsDevice, width: 1, height: 1)
                .With(texture => texture.SetData(new[] { Color.White }));
            this.Font = this.Content.Load<SpriteFont>("Graphics/Alphabet/saga");
            this.State = Cyclic.Enum<State>();
            this.State.OnChange += this.ChangeState;

            var dependency = Dependency.Start(this);
            dependency.Register(this.Configuration);
            dependency.Register(this.Content);
            dependency.Register(this.Client);
            dependency.Register(this.SpriteBatch);
            dependency.Register(this.BlankTexture);
            dependency.Register(this.Font);
            this.Input = dependency.Register<InputHelper<CriticalUpdate>>();
            this.Scenario = dependency.Register<ScenarioHelper>();
            this.Encounter = dependency.Register<EncounterHelper>();
            this.Operation = dependency.Register<OperationHelper>();
            this.Expansion = dependency.Register<ExpansionHelper>();
            this.Journal = dependency.Register<JournalHelper>();

            // var stageContainer = new Rectangle(new Point(240, 50), (BASE_WINDOW_SIZE / 1.55f).ToPoint());
            var stageContainer = BASE_MAP_PANEL_SIZE.ToRectangle();

            this.Scenario.Arrange(stageContainer, this.GetScenarioPath("plateau"));
            this.Encounter.Arrange(stageContainer);
            this.Operation.Arrange(BASE_WINDOW_SIZE.ToRectangle());

            // should be their own classes
            this.Storybook = new Panel<NormalUpdate, ControlDraw>();
            this.Designer = new Panel<NormalUpdate, ControlDraw>();

            // temporary panel stuff
            {
                this.PanelTexture = this.Content.Load<Texture2D>("Graphics/panel");
                this.YesTexture = this.Content.Load<Texture2D>("Graphics/buttonYes");
                this.NoTexture = this.Content.Load<Texture2D>("Graphics/buttonNo");
                var borderTexture = this.Content.Load<Texture2D>("Graphics/border1");
                var overlayTexture = this.Content.Load<Texture2D>("Graphics/overlay");

                this.Storybook.Append(new Patch(BASE_MAP_PANEL_SIZE.ToRectangle(), this.PanelTexture, 11, Color.SandyBrown));
                this.Storybook.Append(new Label(new Rectangle(100, 80, 0, 0), this.Font, "Welcome to the world."));
                this.Storybook.Append(new Label(new Rectangle(100, 140, 0, 0), this.Font, "In this world you will face many challenges. Pity they were all in vain.\nWho knows what will happen next."));
                this.Attach(this.Storybook);

                this.Designer.Append(new Patch(BASE_MAP_PANEL_SIZE.ToRectangle(), this.PanelTexture, 11, Color.ForestGreen));
                var portraitControl = new Basic(new Rectangle(100, 100, 100, 100), this.BlankTexture, Color.Crimson);
                this.Designer.Append(portraitControl);
                this.Attach(this.Designer);

                this.Attach(new PhasedUpdateWrapper<NormalUpdate>(gameTime =>
                {
                    if (this.Input.KeyPressed(Keys.OemPeriod))
                        portraitControl.Recolor(Color.AntiqueWhite);
                    if (this.Input.KeyPressed(Keys.OemComma))
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
                actorPanel.Append(new Patch(upperSidePanelContainer, this.PanelTexture, border: 15, Color.BlanchedAlmond));

                // would be nice if positions were relative to panel
                actorPanel.Append(new Basic(BASE_MAP_PANEL_WIDTH + 40, 40, 70, 70, this.BlankTexture, Color.WhiteSmoke));

                var actorPortraitBasicBlank = new Basic(BASE_MAP_PANEL_WIDTH + 42, 42, 66, 66, this.BlankTexture, new Color(201, 185, 161));
                var actorPortraitBasic = new Basic(BASE_MAP_PANEL_WIDTH + 42, 42, 66, 66, this.BlankTexture, new Color(201, 185, 161));
                actorPortraitBasic.Toggle();
                actorPanel.Append(actorPortraitBasicBlank);
                actorPanel.Append(actorPortraitBasic);
                this.Scenario.OnSourceActorChange += actor =>
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
                // dossierPanel.Append(new Patch(lowerSidePanelContainer, this.PanelTexture, border: 10, new Color(162, 178, 204))
                //     .WithInput(this.Input)
                //     .With(control =>
                //     {
                //         control.OnMouseEnter += x => x.Recolor(Color.MediumSlateBlue);
                //         control.OnMouseLeave += x => x.Recolor(new Color(162, 178, 204));
                //     }));
                this.Journal.Arrange(lowerSidePanelContainer);
                this.Attach(this.Journal);

                var exitConfirmationPanelSize = new Vector2(400, 100);
                var exitConfirmationPanelLocation = (BASE_WINDOW_SIZE / 2) - (exitConfirmationPanelSize / 2);
                var exitConfirmationPanelRectangle = new Rectangle(exitConfirmationPanelLocation.ToPoint(), exitConfirmationPanelSize.ToPoint());
                this.ExitConfirmation = new Panel<CriticalUpdate, PopupDraw>();
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
                };
                this.ExitConfirmation.Append(noButton);

                var yesButtonLocation = (BASE_WINDOW_SIZE / 2) + new Vector2(noYesButtonSize.X, 0) / 1.5f;
                var yesButton = new Button(new Rectangle(yesButtonLocation.ToPoint(), noYesButtonSize.ToPoint()), this.YesTexture, new Color(0, 200, 0));
                yesButton.WithInput(this.Input);
                yesButton.OnClick += button => this.Exit();
                this.ExitConfirmation.Append(yesButton);

                this.Log = new StringBuilder();
                this.Side = new Panel<NormalUpdate, ControlDraw>();
                this.Side.Append(new Patch(new Rectangle(970, 10, 300, 700), this.PanelTexture, 13, Color.BurlyWood));
                this.SideLabel = new Label(new Rectangle(980, 500, 280, 200), this.Font, () => this.Log.ToString());
                this.Side.Append(this.SideLabel);

                var toggleSize = new Vector2(40);
                var toggleLocation = new Vector2(1220, 20);
                this.Toggle = new Button(new Rectangle(toggleLocation.ToPoint(), toggleSize.ToPoint()), this.PanelTexture, Color.BurlyWood);
                this.Toggle.WithInput(this.Input);
                this.Toggle.OnClick += button => this.Side.Toggle();

                var overlayContainer = BASE_WINDOW_RECTANGLE;
                var overlayBasic = new Basic(overlayContainer, overlayTexture);
                this.Attach(overlayBasic);

                this.Attach(this.Side);
                this.Attach(new Panel<NormalUpdate, ControlDraw>(isActive: true).With(panel => panel.Append(this.Toggle)));
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

            // move this above the attaching of map-area panels to hide it on non-scenario state
            dependency.Register<FramerateHelper<CriticalUpdate, PopupDraw>>();

            this.State.Set(Enums.State.Encounter);
            // in case state is default state need to force change
            if (this.State.Value == default)
                this.ChangeState(default, this.State);
        }
        Texture2D PanelTexture;
        Texture2D YesTexture;
        Texture2D NoTexture;
        Panel<CriticalUpdate, PopupDraw> ExitConfirmation;
        Button Toggle;
        Panel<NormalUpdate, ControlDraw> Side;
        Label SideLabel;
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
            {
                if (this.Input.KeyPressed(Keys.Enter))
                    this.Exit();
                return;
            }

            this.IsMouseVisible = true;

            if (this.Input.KeyPressed(Keys.F10))
                this.Scenario.Arrange(this.Scenario.Container, this.GetScenarioPath("valley"));
            if (this.Input.KeyPressed(Keys.F9))
                this.Scenario.Arrange(this.Scenario.Container, this.GetScenarioPath("plateau"));
            if (this.Input.KeyPressed(Keys.F8))
                this.Scenario.Arrange(this.Scenario.Container, this.GetScenarioPath("grove"));
            if (this.Input.KeyPressed(Keys.F7))
                this.Scenario.Arrange(this.Scenario.Container, Shape.Hexagon);
            if (this.Input.KeyPressed(Keys.F6))
                this.Scenario.Arrange(this.Scenario.Container, Shape.Rectangle);
            if (this.Input.KeyPressed(Keys.F5))
                this.Scenario.Arrange(this.Scenario.Container, Shape.Triangle);
            if (this.Input.KeyPressed(Keys.F4))
                this.Scenario.Arrange(this.Scenario.Container, Shape.Parallelogram);
            if (this.Input.KeyPressed(Keys.F3))
                this.Scenario.Arrange(this.Scenario.Container, Shape.Line);

            if (this.Input.KeyPressed(Keys.Tab))
                this.Side.Toggle();

            if (this.Input.KeyPressed(Keys.OemOpenBrackets))
                this.State.Reverse();
            if (this.Input.KeyPressed(Keys.OemCloseBrackets))
                this.State.Advance();

            if (this.Input.MouseMoved())
            {
                this.BaseMouseVector = this.Input.CurrentMouseVector;
                this.ClientResolutionTranslatedMouseVector = this.Input.CurrentVirtualMouseVector;
                this.ScenarioCameraTranslatedMouseVector = this.ClientResolutionTranslatedMouseVector.Transform(this.Scenario.TranslationMatrix.Invert());
            }

            this.OnUpdate?.Invoke<NormalUpdate>(gameTime);

            if (this.Side.IsActive)
            {
                this.Log.Clear();
                this.Log.AppendLine(this.State.ToString());
                // this.Log.AppendLine($"M1: {this.BaseMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M2: {this.ClientResolutionTranslatedMouseVector.PrintRounded()}");
                this.Log.AppendLine($"M3: {this.ScenarioCameraTranslatedMouseVector.PrintRounded()}");
                this.Log.AppendLine($"Virtual: {this.Client.VirtualResolution}");
                this.Log.AppendLine($"Current: {this.Client.CurrentResolution}");
                this.Log.AppendLine($"Window: {this.Window.ClientBounds.Size}");
                this.Log.AppendLine($"Cursor: {this.Scenario.FocusTile?.Into(x => $"{x.Cube} E:{x.Elevation}") ?? "n/a"}");
                // this.Log.AppendLine($"Tiles: {this.Scenario.TileCount}");
                // this.Log.AppendLine($"Interval: {this.Scenario.TilemapRotationInterval}");
                // this.Log.AppendLine($"Container: {this.Scenario.Container.Location}{this.Scenario.Container.Size}");
                // this.Log.AppendLine($"Camera: {this.Scenario.Camera.Position}");
                // this.Log.AppendLine($"{this.Scenario.TilemapDebug}");
                // this.Log.AppendLine($"Fullscreen: {this.Client.IsFullscreen}");
                this.Log.AppendLine($"Faction: {this.Scenario.SourceFaction?.Name ?? "n/a"}");
                // this.Log.AppendLine($"SamplerState: {SamplerStateCycle.Value.Key}");
                // this.Log.AppendLine($"Touch: {TouchPanel.GetState().IsConnected}");
                this.Log.AppendLine(Static.Memo.ToString());
            }

            if (this.Input.KeyPressed(Keys.OemQuotes))
                SamplerStateCycle.Advance();
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Color.Black);

            // this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap);
            // this.SpriteBatch.DrawTo(this.BlankTexture, this.Scenario.Container, Color.Ivory);
            // this.SpriteBatch.End();

            // from here on out, all drawing should use blend state AlphaBlend, which supports transparency.

            // TODO better condition, any state that uses background/foreground (i.e. camera transform)
            if ((this.State == Enums.State.Scenario) || (this.State == Enums.State.Operation))
            {
                var container = this.State.Value switch
                {
                    Enums.State.Scenario => this.Scenario.Container,
                    Enums.State.Operation => this.Operation.Container,
                    _ => throw this.State.Value.Invalid()
                };
                var matrix = this.State.Value switch
                {
                    Enums.State.Scenario => this.Scenario.TranslationMatrix,
                    Enums.State.Operation => this.Operation.TranslationMatrix,
                    _ => throw this.State.Value.Invalid()
                };
                using (new ScissorScope(this.GraphicsDevice, container))
                {
                    // try other SamplerStates
                    this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointWrap,
                        rasterizerState: this.ScissorRasterizer, transformMatrix: matrix);
                    this.OnDraw?.Invoke<BackgroundDraw>(this.SpriteBatch);
                    this.SpriteBatch.End();

                    // sprites can have border artifacts when sampler state is Wrap instead of Clamp
                    // sprites will preserve pixel-style look when using sampler state Point
                    this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp,
                        rasterizerState: this.ScissorRasterizer, transformMatrix: matrix);
                    this.OnDraw?.Invoke<ForegroundDraw>(this.SpriteBatch);
                    this.SpriteBatch.End();
                }
            }

            // TODO better condition
            if (this.State != Enums.State.Operation)
            {
                // PointWrap is used to make controls look sharp/crisp. For blended/anti-aliased edges, use PortraitDraw
                this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
                this.OnDraw?.Invoke<ControlDraw>(this.SpriteBatch);
                this.SpriteBatch.End();

                this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
                this.OnDraw?.Invoke<PortraitDraw>(this.SpriteBatch);
                this.SpriteBatch.End();
            }

            // TODO better condition, any popup can trigger this (including framerate printer if it is active!)
            // (if other popups should never block exit, then exit should be drawn last and stop further processing)
            if (true)
            {
                this.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                this.OnDraw?.Invoke<PopupDraw>(this.SpriteBatch);
                this.SpriteBatch.End();
            }
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

        protected string GetScenarioPath(string name)
        {
            return Path.Combine(CONTENT_ROOT_DIRECTORY, CONTENT_SUB_DIRECTORY_LEVEL, Path.ChangeExtension(name, ".csv"));
        }

        protected void ChangeState(State oldState, State newState)
        {
            IActivate Switch(State state) => state switch
            {
                Enums.State.Storybook => this.Storybook, // lore
                Enums.State.Designer => this.Designer,   // character management
                Enums.State.Scenario => this.Scenario,   // tactical turn based tile combat
                Enums.State.Encounter => this.Encounter, // turn based stationary combat
                Enums.State.Operation => this.Operation, // 2d overhead stealth platforming
                Enums.State.Expansion => this.Expansion, // political map painting
                _ => throw state.Invalid()
            };
            Switch(oldState).Deactivate();
            Switch(newState).Activate();
            this.Journal.AppendLine($"I opened the {newState} page.");
        }

        #endregion

        // some ideas:
        // add to ConfigurationHelper stuff like SamplerState, maybe BlendState, panel color. 
        //      Can affect different SpriteBatch scopes (background/foreground/control/portrait)
        //      Also in settings would be font size? May be tricky to fit it
        // fullscreen just borderless mode? --> not sure of impact on non-windows
        // all form controls need keyboard support, like the blinking selector from pan engine
        // slow pulse button press -> press and held, after 1 second pulse every .10? until released
        // font helper -> exposes Font to dependencies and can switch to other fonts
        // make abstract Tile -> can be hexagon or rectangle, maybe triangle
        // content zipped, use custom ContentManager that handles zipped
        // alternate name for Extended project is Dotnext
    }
}