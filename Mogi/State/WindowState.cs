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
            this.Window = window;
            this.Graphics = graphics;
            this.VirtualResolution = virtualResolution;
            this.CurrentResolution = window.ClientBounds.Size.ToVector2();
        }

        #endregion

        #region Data Members

        protected GameWindow Window { get; }
        protected GraphicsDeviceManager Graphics { get; }
        protected Vector2 VirtualResolution { get; set; }
        protected Vector2 CurrentResolution { get; set; }

        // possibly expose a mutable collection of 'regions' which can be used as subwindows for other classes (CameraHelper)

        public event Action<WindowState> OnResize;

        #endregion

        #region Methods

        public void Resize()
        {

            this.OnResize?.Invoke(this);
        }

        #endregion
    }
}