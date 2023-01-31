using Hex.Auxiliary;

namespace Hex.Helpers
{
    /// <summary> Exposes settings that can be configured externally. </summary>
    public class ConfigurationHelper
    {
        #region Constructors

        public ConfigurationHelper()
        {
        }

        #endregion

        #region Properties

        /// <summary> When <see langword="true"/>, the client window starts in fullscreen mode.
        /// <br/> When <see langword="false"/>, the client window starts in windowed mode. </summary>
        public bool StartInFullscreen { get; set; }

        /// <summary> When <see langword="true"/>, camera movement is turned on and off by separate presses of the assigned button.
        /// <br/> When <see langword="false"/>, pressing and releasing the button turns movement on and off, respectively. </summary>
        public bool UseStickyCameraMovement { get; set; }

        /// <summary> When <see langword="true"/>, rotating the tilemap will automatically cause it to be repositioned with a selected tile in the middle.
        /// <br/> When <see langword="false"/>, or if no tile is selected, tilemap positioning is unaffected. </summary>
        public bool CenterTilemapRotationOnSource { get; set; }

        #endregion

        #region Methods

        // TODO implement loading from config.ini file
        public void Load()
        {
            Extern.IsWindowsLaptop().Match(boolean =>
            {
                // this.StartInFullscreen = boolean;
                // this.UseStickyCameraMovement = boolean;
            });

            this.CenterTilemapRotationOnSource = false;
        }

        #endregion
    }
}