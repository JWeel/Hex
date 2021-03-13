using Microsoft.Xna.Framework;
using System;

namespace Mogi.State
{
    /// <summary> Provides methods and fields to keep track of window state. </summary>
    public class WindowState
    {
        #region Constructors

        public WindowState(GameWindow window, GraphicsDeviceManager graphics, Vector2 virtualResolution)
        {
            this.BackBufferGetter = () => new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            this.ClientWindowGetter = () => window.ClientBounds.Size.ToVector2();

            this.VirtualResolution = virtualResolution;
        }

        #endregion

        #region Data Members

        protected Vector2 VirtualResolution { get; set; }

        protected Func<Vector2> BackBufferGetter { get; set; }
        public Vector2 BackBuffer => this.BackBufferGetter();

        protected Func<Vector2> ClientWindowGetter { get; set; }
        public Vector2 ClientWindow => this.ClientWindowGetter();

        protected Vector2 BackBufferRelativeToVirtualResolution { get; set; }
        protected Vector2 BackBufferRelativeToWindowResolution { get; set; }

        // possibly expose a mutable collection of 'regions' which can be used as subwindows for other classes (CameraHelper)

        public event Action<WindowState> OnResize;

        #endregion

        #region Methods

        public void Resize()
        {
            this.BackBufferRelativeToVirtualResolution = this.BackBuffer / this.VirtualResolution;
            this.BackBufferRelativeToWindowResolution = this.BackBuffer / this.ClientWindow;

            this.OnResize?.Invoke(this);
        }

        public Vector2 Translate(Vector2 value) =>
            value / this.BackBufferRelativeToVirtualResolution * this.BackBufferRelativeToWindowResolution;

        #endregion
    }
}