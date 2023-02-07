using Hex.Phases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Inversion;

namespace Hex.Helpers
{
    public class ExpansionHelper : IUpdate<NormalUpdate>, IDraw<ForegroundDraw>, IActivate
    {
        #region Constructors

        public ExpansionHelper()
        {
        }
            
        #endregion

        #region Data Members

        public bool IsActive { get; protected set; }

        #endregion

        #region Public Methods

        public void Update(GameTime gameTime)
        {
        }
        public void Draw(SpriteBatch spriteBatch)
        {
        }

        public void Activate()
        {
            this.IsActive = true;
        }

        public void Deactivate()
        {
            this.IsActive = false;
        }

        #endregion
    }
}