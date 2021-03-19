using Extended.Collections;
using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Collections.Generic;

namespace Mogi.Controls
{
    /// <summary> Represents a control that contains other controls. </summary>
    public class Panel : Control<Panel>, IEnumerable<IControl>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a destination area. The base overlay color will be set to <see cref="Color.Transparent"/> and therefore no background texture will be drawn. </summary>
        /// <remarks> This is the only case where the base overlay color is assigned as transparent instead of the default white. </remarks>
        public Panel(Rectangle destination)
            : this(destination, texture: default, Color.Transparent)
        {
        }

        /// <summary> Initializes a new instance with a destination area and an overlay color. A texture will be assigned the first time the control is drawn. </summary>
        public Panel(Rectangle destination, Color color)
            : this(destination, texture: default, color)
        {
        }

        /// <summary> Initializes a new instance with a destination area and a texture. </summary>
        public Panel(Rectangle destination, Texture2D texture)
            : this(destination, texture, Color.White)
        {
        }

        /// <summary> Initializes a new instance with a destination area, a texture, and an overlay color. </summary>
        public Panel(Rectangle destination, Texture2D texture, Color color)
            : base(destination, texture, color)
        {
            this.Controls = new InsertionSet<IControl>();
        }

        #endregion

        #region Properties

        /// <summary> A collection of controls contained in this panel that are updated and drawn in insertion order. </summary>
        public InsertionSet<IControl> Controls { get; }

        #endregion

        #region Methods

        /// <summary> Adds a control that will be updated and drawn before all other controls in the panel. </summary>
        public void Insert(IControl control) =>
            this.Controls.Insert(control);

        /// <summary> Adds a control that will be updated and drawn after all other controls in the panel. </summary>
        public void Append(IControl control) =>
            this.Controls.Append(control);

        /// <summary> Moves a specified control to the front of the panel. If it was not part of the panel, it will be added. </summary>
        public void Focus(IControl control)
        {
            // Remove does not care if it does not exist in the set
            this.Controls.Remove(control);
            this.Controls.Insert(control);
        }

        #endregion

        #region IControl Implementation

        public override bool Update(GameTime gameTime)
        {
            if (!this.IsActive)
                return true;
            this.Controls.Each(control => control.Update(gameTime));
            return true;
        }

        public override bool Draw(SpriteBatch spriteBatch)
        {
            if (!this.IsActive)
                return true;
            if (!base.Draw(spriteBatch))
                return false;
            this.Controls.Each(control => control.Draw(spriteBatch));
            return true;
        }

        #endregion

        #region IEnumerable Implementation

        public IEnumerator<IControl> GetEnumerator() =>
            this.Controls.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        #endregion
    }
}