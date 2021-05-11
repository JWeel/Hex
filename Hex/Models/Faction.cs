using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Faction
    {
        #region Constructors

        public Faction()
        {
            this.Allies = new [] { this };
        }
            
        #endregion

        #region Properties

        public string Name { get; init; }

        public Color Color { get; init; }

        public Faction[] Allies { get; protected set; }
            
        #endregion
    }
}