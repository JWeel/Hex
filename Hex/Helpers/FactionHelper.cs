using Extended.Extensions;
using Hex.Models;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Hex.Helpers
{
    public class FactionHelper
    {
        #region Constructors

        public FactionHelper()
        {
            this.Factions = new[]
            {
                new Faction { Name = "Free", Color = Color.PaleGoldenrod },
                new Faction { Name = "Monster", Color = Color.Green },
            };
            this.ActiveFaction = this.Factions[0];
        }

        #endregion

        #region Properties

        public Faction ActiveFaction { get; protected set; }

        protected Faction[] Factions { get; set; }

        #endregion

        #region Methods

        public void Cycle()
        {
            if (this.ActiveFaction == default)
            {
                this.ActiveFaction = this.Factions[0];
                return;
            }
            if (this.ActiveFaction == this.Factions.Last())
            {
                this.ActiveFaction = default;
                return;
            }
            var index = this.Factions.IndexOf(this.ActiveFaction);
            this.ActiveFaction = this.Factions[index + 1];
        }

        #endregion
    }
}