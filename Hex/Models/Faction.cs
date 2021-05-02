using Microsoft.Xna.Framework;

namespace Hex.Models
{
    public class Faction
    {
        #region Constructors

        public Faction()
        {
        }
            
        #endregion

        #region Properties

        public Color Color { get; }

        public Faction[] Allies { get; protected set; }
            
        #endregion
    }
}