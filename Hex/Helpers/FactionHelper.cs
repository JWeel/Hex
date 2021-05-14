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

        public bool Final =>
            (this.ActiveFaction == this.Factions.Last());

        protected Faction[] Factions { get; set; }
        protected Faction LastActiveFaction { get; set; }

        #endregion

        #region Methods

        public void Toggle()
        {
            var toggled = this.LastActiveFaction;
            this.LastActiveFaction = this.ActiveFaction;
            this.ActiveFaction = toggled;
        }

        public void Cycle()
        {
            if (this.ActiveFaction == null)
                return;

            if (this.ActiveFaction == this.Factions.Last())
            {
                this.ActiveFaction = this.Factions[0];
                return;
            }
            var index = this.Factions.IndexOf(this.ActiveFaction);
            this.ActiveFaction = this.Factions[index + 1];
        }

        #endregion
    }
}