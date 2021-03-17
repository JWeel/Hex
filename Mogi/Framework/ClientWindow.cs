using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Extensions;
using System;

namespace Mogi.Framework
{
    /// <summary> Merges functionality of <see cref="GameWindow"/> and <see cref="GraphicsDeviceManager"/> into a central point of access to help react to changes in window size. </summary>
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

        /// <summary> A render target that is meant to be set and unset on the GraphicsDevice before and after drawing, respectively. It scales all graphics from virtual resolution to client size. </summary>
        public RenderTarget2D RenderTarget { get; protected set; }

        /// <summary> The fixed base resolution that is scaled to fit the client window. </summary>
        public Vector2 VirtualResolution { get; protected set; }

        /// <summary> The current resolution in which graphics are rendered. It will match the client window size in windowed mode. </summary>
        public Vector2 CurrentResolution { get; protected set; }

        protected Vector2 PreviousResolution { get; set; }

        protected GameWindow Window { get; }
        protected GraphicsDeviceManager Graphics { get; }
        protected Vector2 MinimumResolution { get; }

        protected float VirtualAspectRatio => this.VirtualResolution.X / this.VirtualResolution.Y;

        protected Vector2 ScreenResolution => this.Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.ToSizeVector();

        #endregion

        #region Public Methods

        /// <summary> Sets up the window to match virtual resolution and prepares it for changes. </summary>
        public void Initialize()
        {
            // The purpose of this class is to react to user resizing.
            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += this.OnWindowResize;

            // HardwareModeSwitch: if this is set to true, fullscreen automatically scales the regular backbuffer.
            // However, toggling is a lot slower. Also, resizing a non-fullscreen window does not rescale.
            // When set to false, fullscreen is not auto-scaled. By adding a render target it will still auto-scale.
            // This render target can then also be used for non-fullscreen scaling using ClientSizeChanged event.
            this.Graphics.HardwareModeSwitch = false;
            this.Graphics.IsFullScreen = false;
            this.Graphics.PreferredBackBufferWidth = (int) this.VirtualResolution.X;
            this.Graphics.PreferredBackBufferHeight = (int) this.VirtualResolution.Y;

            // GraphicsDeviceManager and GameWindow properties require a call to GraphicsDeviceManager.ApplyChanges
            this.Graphics.ApplyChanges();

            // This value is meant to be set and unset on the GraphicsDevice before and after drawing, respectively.
            // Setting a render target changes the GraphicsDevice.Viewport size to match render target size.
            // After unsetting it, the viewport returns to client size. The target can then be drawn as a texture,
            // and everything that was drawn on it will be drawn to the client and automatically scale to client size.
            this.RenderTarget = new RenderTarget2D(this.Graphics.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
        }

        public void ToggleFullscreen()
        {
            // GraphicsDeviceManager.ToggleFullScreen internally calls GraphicsDeviceManager.ApplyChanges
            this.Graphics.ToggleFullScreen();
        }

        /// <summary> Manually resizes the window to match a given width, then triggers resize events. </summary>
        /// <param name="width"> The desired width of the window. </param>
        /// <param name="recenterWindow"> Indicates whether or not the client window, after resizing, should be repositioned so that it sits in the center of the client screen. </param>
        /// <remarks> Height will be automatically changed to preserve the aspect ratio of the virtual resolution. 
        /// <br/> This method automatically clamps the resolution: the minimum is defined in the constructor, the maximum is the larger of client screen resolution and virtual resolution.
        /// <br/> This method triggers resize events before potentially recentering the client window. Any event handlers that are subscribed to <see cref="OnResize"/> should not rely on window position.
        /// <br/> This method should not be called when in fullscreen mode and will return immediately if it is. </remarks>
        public void Resize(int width, bool recenterWindow)
        {
            if (this.Graphics.IsFullScreen)
                return;

            width = Math.Max(width, (int) this.MinimumResolution.X);
            width = Math.Min(width, (int) Math.Max(this.VirtualResolution.X, this.ScreenResolution.X));

            // Setting graphics backbuffer and calling ApplyChanges causes Window.ClientBounds to change
            this.Graphics.PreferredBackBufferWidth = width;
            this.Graphics.ApplyChanges();

            // Window.ClientBounds having changed can be leveraged to use OnWindowResize to update height
            // This will then also raise ClientWindow.OnResize event
            this.OnWindowResize(this, default);

            if (recenterWindow)
            {
                this.Window.Position = (this.ScreenResolution / 2 - this.CurrentResolution / 2).ToPoint();
            }
        }

        #endregion

        #region Protected Methods

        protected void OnWindowResize(object sender, EventArgs e)
        {
            // In fullscreen, backbuffer is scaled to window automatically, so there is no need to set the backbuffer.
            // Therefore there is no need to set the backbuffer resolution.
            // This can be leveraged to preserve the windowed resolution when going back to windowed mode.
            // Alternatively, this class would need to keep track of WasPreviouslyFullScreen + LastWindowedResolution
            if (!this.Graphics.IsFullScreen)
            {
                // Need to unsubscribe because this event would be triggered again by GraphicsDeviceManager.ApplyChanges
                this.Window.ClientSizeChanged -= this.OnWindowResize;

                // Set backbuffer to match client size, adjusting the second coordinate to preserve aspect ratio
                if (this.Window.ClientBounds.Width != this.CurrentResolution.X)
                {
                    this.Graphics.PreferredBackBufferWidth = this.Window.ClientBounds.Width;
                    this.Graphics.PreferredBackBufferHeight = (int) (this.Window.ClientBounds.Width / this.VirtualAspectRatio);
                }
                else if (this.Window.ClientBounds.Height != this.CurrentResolution.Y)
                {
                    this.Graphics.PreferredBackBufferHeight = this.Window.ClientBounds.Height;
                    this.Graphics.PreferredBackBufferWidth = (int) (this.Window.ClientBounds.Height * this.VirtualAspectRatio);
                }

                this.Graphics.ApplyChanges();
                this.Window.ClientSizeChanged += this.OnWindowResize;
                this.CurrentResolution = this.Window.ClientBounds.Size.ToVector2();
            }
            // Note: ClientSizeChanged gets raised twice when going to fullscreen, but only once when going back
            this.OnResize?.Invoke();
        }

        #endregion
    }
}