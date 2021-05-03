using Hex.Models;
using System.Collections.Generic;

namespace Hex.Helpers
{
    public class FactionHelper
    {
        #region Constructors

        public FactionHelper()
        {
        }

        #endregion

        #region Properties

        protected IDictionary<Faction, Faction[]> FriendlyMap { get; set; }
        protected IDictionary<Faction, Faction[]> NeutralMap { get; set; }
        protected IDictionary<Faction, Faction[]> HostileMap { get; set; }

        #endregion
    }
}