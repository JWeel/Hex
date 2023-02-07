using Extended.Collections;
using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Inversion;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Mogi.Controls
{
    /// <summary> Represents a control that contains other controls. </summary>
    public class Panel<TUpdate, TDraw> : IControl<TUpdate, TDraw>, IEnumerable<IControl>, IActivate
        where TUpdate : IPhase
        where TDraw : IPhase
    {
        #region Constructors

        /// <summary> Initializes a new instance that starts in an inactive state. </summary>
        public Panel()
            : this(isActive: false)
        {
        }

        /// <summary> Initializes a new instance with a specified active state. </summary>
        public Panel(bool isActive)
        {
            this.IsActive = isActive;
            this.Controls = new InsertionSet<IControl>();
        }

        #endregion

        #region Properties

        public event Action OnActivate;
        public event Action OnDeactivate;

        /// <summary> A collection of controls contained in this panel that are updated and drawn in insertion order. </summary>
        public InsertionSet<IControl> Controls { get; }

        public bool IsActive { get; protected set; }

        #endregion

        #region Methods

        /// <summary> Adds a control that will be updated and drawn before all other controls in the panel. </summary>
        public void Insert(IControl control) =>
            this.Controls.Insert(control);

        /// <summary> Adds a control that will be updated and drawn after all other controls in the panel. </summary>
        public void Append(IControl control) =>
            this.Controls.Append(control);

        /// <summary> Moves a specified control to the front of the panel. If it was not part of the panel yet, it will be added to it. </summary>
        public void Focus(IControl control)
        {
            // Remove will do nothing if the control does not exist in the set
            this.Controls.Remove(control);
            this.Controls.Insert(control);
        }

        public void Reset()
        {
            this.Controls.Clear();
        }

        #endregion

        #region IControl Implementation

        public void Update(GameTime gameTime)
        {
            // needed if the panel is not attached to an IRoot
            if (!this.IsActive)
                return;
            this.Controls.Each(control => control.Update(gameTime));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // needed if the panel is not attached to an IRoot
            if (!this.IsActive)
                return;
            this.Controls.Each(control => control.Draw(spriteBatch));
        }

        /// <summary> Toggles the <see cref="IsActive"/> state of the control. </summary>
        public void Toggle()
        {
            if (this.IsActive)
                this.Deactivate();
            else
                this.Activate();
        }

        /// <summary> Sets the <see cref="IsActive"/> state of the control to <see langword="true"/>. </summary>
        public void Activate() 
        {
            this.IsActive = true;
            this.Controls.Each(control => control.Activate());
            this.OnActivate?.Invoke();
        }

        /// <summary> Sets the <see cref="IsActive"/> state of the control to <see langword="false"/>. </summary>
        public void Deactivate() 
        {
            this.IsActive = false;
            this.Controls.Each(control => control.Deactivate());
            this.OnDeactivate?.Invoke();
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