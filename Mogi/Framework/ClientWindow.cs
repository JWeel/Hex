using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Mogi.Framework
{
    /// <summary> Merges functionality of <see cref="GameWindow"/> and <see cref="GraphicsDeviceManager"/> into a central point of access to help react to changes in window size. </summary>
    public class ClientWindow
    {
        #region Constructors

        public ClientWindow(GameWindow window, GraphicsDeviceManager graphics, Vector2 virtualResolution)
        {
            this.Window = window;
            this.Graphics = graphics;
            this.VirtualResolution = virtualResolution;
        }

        #endregion

        #region Properties

        public event Action OnResize;

        public RenderTarget2D RenderTarget { get; protected set; }
        public Vector2 VirtualResolution { get; protected set;}

        protected Vector2 PreviousClientBounds { get; set;}
        protected GameWindow Window { get; }
        protected GraphicsDeviceManager Graphics { get;}

        protected float VirtualAspectRatio => this.VirtualResolution.X / this.VirtualResolution.Y;

        #endregion

        #region Public Methods

        /// <summary> Sets up the window to match virtual resolution and prepares it for changes. </summary>
        public void Initialize()
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
            this.Graphics.PreferredBackBufferWidth = (int) this.VirtualResolution.X;
            this.Graphics.PreferredBackBufferHeight = (int) this.VirtualResolution.Y;
            this.Graphics.ApplyChanges();

            // This is the render target that is respectively set and unset before and after drawing. [See BeginDraw|EndDraw]
            // Setting a render target changes the GraphicsDevice.Viewport size to match render target size.
            // After unsetting it, the viewport returns to client size. The target can then be drawn as a texture,
            // and everything that was drawn on it will be drawn to the client and be automatically scale to client size.
            this.RenderTarget = new RenderTarget2D(this.Graphics.GraphicsDevice, this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
        }

        public void ToggleFullscreen()
        {
            this.Graphics.ToggleFullScreen();
        }

        /// <summary> Manually resizes the window to match a given resolution, then triggers resize events. </summary>
        /// <param name="width"> The desired width of the window. </param>
        /// <param name="recenterWindow"> Indicates whether or not the client window, after resizing, should be repositioned so that it sits in the center of the client screen. </param>
        /// <remarks> This method does not clamp the resolution to any minimum or maximum. This should be handled by the caller.
        /// <br/> This method triggers resize events before potentially recentering the client window. Any event handlers that are subscribed to <see cref="OnResize"/> should not rely on screen window position. </remarks>
        public void Resize(int width, bool recenterWindow)
        {
            this.Graphics.PreferredBackBufferWidth = windowWidth;
            this.Graphics.ApplyChanges();
            this.OnWindowResize(this, default);

            if (recenterWindow)
            {
                var screenResolution = new Vector2(this.Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width, this.Graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height);
                var backBufferr = new Vector2(this.Graphics.PreferredBackBufferWidth, this.Graphics.PreferredBackBufferHeight);
                this.Window.Position = screenResolution / 2 - backBuffer / 2;
            }
        }
            
        #endregion
        
        #region Protected Methods

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

                // Set backbuffer to match client size, adjusting the second coordinate to preserve aspect ratio
                if (this.Window.ClientBounds.Width != this.PreviousClientBounds.X)
                {
                    this.Graphics.PreferredBackBufferWidth = this.Window.ClientBounds.Width;
                    this.Graphics.PreferredBackBufferHeight = (int) (this.Window.ClientBounds.Width / this.VirtualAspectRatio);
                }
                else if (this.Window.ClientBounds.Height != this.PreviousClientBounds.Y)
                {
                    this.Graphics.PreferredBackBufferHeight = this.Window.ClientBounds.Height;
                    this.Graphics.PreferredBackBufferWidth = (int) (this.Window.ClientBounds.Height * this.VirtualAspectRatio);
                }

                this.Graphics.ApplyChanges();
                this.Window.ClientSizeChanged += this.OnWindowResize;
                this.PreviousClientBounds = this.Window.ClientBounds.Size.ToVector2();
            }
            // Note: ClientSizeChanged gets raised twice when going to fullscreen, but only once when going back
            this.OnResize?.Invoke();
        }
            
        #endregion
    }
}