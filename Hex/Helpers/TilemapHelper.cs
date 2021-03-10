using Hex.Models;

namespace Hex.Helpers
{
    public class TilemapHelper
    {
        #region Constructors

        public TilemapHelper()
        {
            this.HexagonMap = new HexagonMap(default, default, default);
        }

        #endregion

        #region Properties

        public HexagonMap HexagonMap { get; }

        #endregion
    }
}