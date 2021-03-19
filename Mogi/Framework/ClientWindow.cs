using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using System;

namespace Mogi.Framework
{
    /// <summary> Mixes the functionality of <see cref="GameWindow"/> and <see cref="GraphicsDeviceManager"/> with management of virtual resolution, providing a single point of access for methods and data related to window-scaled graphics. </summary>
    public class ClientWindow
    {
        #region Constructors

        /// <summary> Initializes a new instance with a window, a graphics device manager and a virtual resolution. Minimum resolution will be set to half of virtual resolution. </summary>
        /// <param name="window"> An instance of <see cref="GameWindow"/> that should come from <see cref="Game.Window"/>. </param>
        /// <param name="graphics"> An instance of <see cref="GraphicsDeviceManager"/> that should have been created in the <see cref="Game"/> constructor. </param>
        /// <param name="virtualResolution"> Defines the virtual resolution of the client window. Values smaller than <see langword="1"/> will be set to <see langword="1"/>. </param>
        public ClientWindow(GameWindow window, GraphicsDeviceManager graphics, Vector2 virtualResolution)
            : this(window, graphics, virtualResolution, minimumResolution: virtualResolution / 2)
        {
        }

        /// <summary> Initializes a new instance with a window, a graphics device manager, a virtual resolution and a minimum resolution. </summary>
        /// <param name="window"> An instance of <see cref="GameWindow"/> that should come from <see cref="Game.Window"/>. </param>
        /// <param name="graphics"> An instance of <see cref="GraphicsDeviceManager"/> that should have been created in the <see cref="Game"/> constructor. </param>
        /// <param name="virtualResolution"> Defines the virtual resolution of the client window. X and Y values smaller than <see langword="1"/> will be set to <see langword="1"/>. </param>
        /// <param name="minimumResolution"> Defines the minimum resolution of the client window. The vector will be clamped between <see cref="Vector2.One"/> and <paramref name="virtualResolution"/>. </param>
        public ClientWindow(GameWindow window, GraphicsDeviceManager graphics, Vector2 virtualResolution, Vector2 minimumResolution)
        {
            this.Window = window;
            this.Graphics = graphics;
            this.VirtualResolution = Vector2.Max(virtualResolution, Vector2.One);
            this.MinimumResolution = Vector2.Clamp(minimumResolution, min: Vector2.One, max: this.VirtualResolution);
            this.CurrentResolution = this.VirtualResolution;
        }

        #endregion

        #region Properties

        /// <summary> Raised when the size of the window changes for any reason (including toggling fullscreen mode). </summary>
        public event Action OnResize;

        /// <summary> A render target that is meant to be set and unset on the GraphicsDevice before and after drawing, respectively. This will cause all graphics to be scaled from virtual resolution to client size. </summary>
        public RenderTarget2D RenderTarget { get; protected set; }

        /// <summary> The fixed base resolution that is scaled to fit the client window. </summary>
        public Vector2 VirtualResolution { get; protected set; }

        /// <summary> The current resolution in which graphics are rendered. It will match the client window size in windowed mode. </summary>
        public Vector2 CurrentResolution { get; protected set; }

        /// <summary> Indicates whether fullscreen mode is on. </summary>
        public bool IsFullscreen
        {
            get => this.Graphics.IsFullScreen;
            set => this.Graphics.IsFullScreen = value;
        }

        protected GameWindow Window { get; }
        protected GraphicsDeviceManager Graphics { get; }
        protected Vector2 MinimumResolution { get; }
        protected bool WasPreviouslyFullScreen { get; set; }
        protected Vector2 LastWindowedResolution { get; set; }

        /// <summary> The aspect ratio of the virtual resolution (width / height). </summary>
        protected float VirtualAspectRatio => this.VirtualResolution.X / this.VirtualResolution.Y;

        /// <summary> A vector that contains the screen resolution. </summary>
        protected Vector2 MonitorResolution => this.Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.ToSizeVector();

        /// <summary> Contains the result of dividing current resolution by virtual resolution. Can be used to calculate relative coordinates. </summary>
        protected Vector2 RelativeResolution { get; set; }

        #endregion

        #region Public Methods

        /// <summary> Sets up the window to match virtual resolution and prepares it for changes. </summary>
        public void Initialize()
        {
            // The purpose of this class is to react to user resizing.
            this.Window.AllowUserResizing = true;

            // HardwareModeSwitch: if this is set to true, fullscreen automatically scales the regular backbuffer.
            // However, toggling is a lot slower. Also, resizing a non-fullscreen window does not rescale.
            // When set to false, fullscreen is not auto-scaled. By adding a render target it will still auto-scale.
            // This render target can then also be used for non-fullscreen scaling using ClientSizeChanged event.
            this.Graphics.HardwareModeSwitch = false;

            // GraphicsDeviceManager and GameWindow properties require a call to GraphicsDeviceManager.ApplyChanges
            // ResizeBackBuffer internally calls that method, after it sets the preferred backbuffer size.
            this.ResizeBackBuffer(this.VirtualResolution);

            // ClientSizeChanged is raised when user changes window or when fullscreen mode is toggled
            this.Window.ClientSizeChanged += this.OnWindowResize;

            // This value is meant to be set and unset on the GraphicsDevice before and after drawing, respectively.
            // Setting a render target changes the GraphicsDevice.Viewport size to match render target size.
            // After unsetting it, the viewport returns to client size. The target can then be drawn as a texture,
            // and everything that was drawn on it will be drawn to the client and automatically scale to client size.
            this.RenderTarget = new RenderTarget2D(this.Graphics.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

            if (this.CurrentResolution == this.MonitorResolution)
                this.CenterWindow();
        }

        /// <summary> Used to toggle between fullscreen mode and windowed mode. </summary>
        public void ToggleFullscreen()
        {
            // GraphicsDeviceManager.ToggleFullScreen internally calls GraphicsDeviceManager.ApplyChanges
            // It then triggers OnWindowResize. This calls ResizeBackBuffer, which calls ApplyChanges again.
            // To avoid the double call, OnWindowResize is called here directly.
            this.IsFullscreen = !this.IsFullscreen;
            this.OnWindowResize(default, default);

            // IDEA: Maybe instead of Monogame fullscreen toggle do it using this.Resize(this.MonitorResolution)
            // That should stop the double triggered OnWindowResize, but may have some side effects
        }

        /// <summary> Manually resizes the window to match a given resolution, then triggers resize events. In fullscreen mode, this method does nothing. </summary>
        /// <remarks> The resolution will be modified to preserve aspect ratio, and is clamped if outside the following range: the minimum is defined in the constructor of this class, the maximum is the larger of client screen resolution and virtual resolution. </remarks>
        /// <param name="resolution"> The desired window resolution. </param>
        // Note: VirtualResolution larger than MonitorResolution is untested.
        public void Resize(Vector2 resolution)
        {
            if (this.IsFullscreen)
                return;

            var oldResolution = this.CurrentResolution;
            var newResolution = Vector2.Clamp(resolution, min: this.MinimumResolution, max: Vector2.Max(this.VirtualResolution, this.MonitorResolution));

            // ResizeBackBuffer will be leveraged to handle preserving aspect ratio and raising ClientWindow.OnResize
            this.ResizeBackBuffer(newResolution);

            // When resolution matches screen, repositioning in the center creates a borderless fullscreen effect.
            // This means there will be no windowbar (just like when toggling fullscreen).
            // When resizing down from this repositioned fullscreen, the windowbar will be out of reach.
            // So for both these scenarios the window should be repositioned. In all other cases, it is left alone.
            if (this.CurrentResolution != this.MonitorResolution && oldResolution != this.MonitorResolution)
                return;
            this.CenterWindow();
        }

        /// <summary> Centers the window to the middle of the screen. </summary>
        public void CenterWindow()
        {
            this.Window.Position = (this.MonitorResolution / 2 - this.CurrentResolution / 2).ToPoint();
        }

        /// <summary> Translates a screen coordinate to a coordinate relative to the virtual resolution. </summary>
        public Vector2 Translate(Vector2 value) =>
            value / this.RelativeResolution;

        #endregion

        #region Protected Methods

        // Note: ClientSizeChanged gets raised twice when going to fullscreen, but only once when going back
        protected void OnWindowResize(object sender, EventArgs e)
        {
            // GraphicsDeviceManager.ApplyChanges (called by ResizeBackBuffer) sometimes triggers this event again.
            this.Window.ClientSizeChanged -= this.OnWindowResize;

            if (!this.WasPreviouslyFullScreen && this.IsFullscreen)
                this.LastWindowedResolution = this.CurrentResolution;

            if (this.WasPreviouslyFullScreen && !this.IsFullscreen)
                this.ResizeBackBuffer(this.LastWindowedResolution);
            else
                this.ResizeBackBuffer(this.Window.ClientBounds.Size.ToVector2());

            this.WasPreviouslyFullScreen = this.IsFullscreen;

            // It is now safe to resubscribe to the event
            this.Window.ClientSizeChanged += this.OnWindowResize;
        }

        protected void ResizeBackBuffer(Vector2 resolution)
        {
            var backbufferWidthDelta = Math.Abs(this.Graphics.PreferredBackBufferWidth - resolution.X);
            var backbufferHeightDelta = Math.Abs(this.Graphics.PreferredBackBufferHeight - resolution.Y);

            // Set backbuffer to match resolution, adjusting the second coordinate to preserve aspect ratio.
            if (backbufferWidthDelta > backbufferHeightDelta)
            {
                this.Graphics.PreferredBackBufferWidth = (int) resolution.X;
                this.Graphics.PreferredBackBufferHeight = (int) (resolution.X / this.VirtualAspectRatio);
            }
            else
            {
                this.Graphics.PreferredBackBufferHeight = (int) resolution.Y;
                this.Graphics.PreferredBackBufferWidth = (int) (resolution.Y * this.VirtualAspectRatio);
            }

            // Changing the BackBuffer and calling ApplyChanges will also cause the ClientBounds to be changed.
            this.Graphics.ApplyChanges();

            // BackBuffer and ClientBounds are equal now, so can construct vector from either: shortest code wins.
            this.CurrentResolution = this.Window.ClientBounds.Size.ToVector2();

            // This can be used to translate screen coordinates from actual to virtual.
            // This could also be a calculated property.
            this.RelativeResolution = this.CurrentResolution / this.VirtualResolution;

            this.OnResize?.Invoke();
        }

        #endregion
    }
}