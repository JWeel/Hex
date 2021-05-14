using System.Linq;
using Extended.Extensions;

namespace Hex.Helpers
{
    public class TurnHelper
    {
        #region Constructors

        public TurnHelper(ActorHelper actor, FactionHelper faction)
        {
            this.Actor = actor;
            this.Faction = faction;
            this.TurnCount = 1;
        }

        #endregion

        #region Properties

        public ActorHelper Actor { get; }
        public FactionHelper Faction { get; }

        public int TurnCount { get; protected set; }

        #endregion

        #region Methods

        public void Next()
        {
            if (this.Faction.Final)
                this.TurnCount++;
            
            this.Faction.Cycle();
            this.Actor.Actors
                .Where(actor => (actor.Faction == this.Faction.ActiveFaction))
                .Each(actor => actor.Reset());
        }

        #endregion
    }
}