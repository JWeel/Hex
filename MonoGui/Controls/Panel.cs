using Extended.Collections;
using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGui.Controls
{
    public class Panel : Control<Panel>
    {
        #region Constructors

        public Panel()
        {
            this.Controls = new InsertionSet<IControl>();
        }

        #endregion

        #region Properties

        public InsertionSet<IControl> Controls { get; }

        public bool IsActive { get; set; }

        #endregion

        #region Methods

        public void Add(IControl control) =>
            this.Controls.Insert(control);

        public void Focus(IControl control)
        {
            // Remove does not care if it does not exist in the set
            this.Controls.Remove(control);
            this.Controls.Insert(control);
        }

        public void Toggle() =>
            this.IsActive = !this.IsActive;

        public override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return;
            this.Controls.Each(control => control.Update(gameTime));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!this.IsActive)
                return;
            this.Controls.Each(control => control.Draw(spriteBatch));
        }

        #endregion
    }
}