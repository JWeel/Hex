namespace Mogi.Helpers
{
    /// <summary> Provides sharable state. </summary>
    public class StateHelper
    {
        #region Constructors

        /// <summary> Initializes a new instance. </summary>
        public StateHelper()
        {
        }

        #endregion

        #region Properties

        public bool IsPausing { get; set; }

        #endregion
    }
}