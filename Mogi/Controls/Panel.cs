using Extended.Collections;
using Extended.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Inversion;
using System.Collections;
using System.Collections.Generic;

namespace Mogi.Controls
{
    /// <summary> Represents a control that contains other controls. </summary>
    public class Panel<TUpdate, TDraw> : IControl<TUpdate, TDraw>, IEnumerable<IControl<TUpdate, TDraw>>, IPrevent, IActivate
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
            this.Controls = new InsertionSet<IControl<TUpdate, TDraw>>();
        }

        #endregion

        #region Properties

        /// <summary> A collection of controls contained in this panel that are updated and drawn in insertion order. </summary>
        public InsertionSet<IControl<TUpdate, TDraw>> Controls { get; }

        public bool IsActive { get; protected set; }

        #endregion

        #region Methods

        /// <summary> Adds a control that will be updated and drawn before all other controls in the panel. </summary>
        public void Insert(IControl<TUpdate, TDraw> control) =>
            this.Controls.Insert(control);

        /// <summary> Adds a control that will be updated and drawn after all other controls in the panel. </summary>
        public void Append(IControl<TUpdate, TDraw> control) =>
            this.Controls.Append(control);

        /// <summary> Moves a specified control to the front of the panel. If it was not part of the panel yet, it will be added to it. </summary>
        public void Focus(IControl<TUpdate, TDraw> control)
        {
            // Remove will do nothing if the control does not exist in the set
            this.Controls.Remove(control);
            this.Controls.Insert(control);
        }

        #endregion

        #region IControl Implementation

        public void Update(GameTime gameTime)
        {
            // if (!this.IsActive)
            //     return;
            this.Controls.Each(control => control.Update(gameTime));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // if (!this.IsActive)
            //     return;
            this.Controls.Each(control => control.Draw(spriteBatch));
        }

        /// <summary> Toggles the <see cref="IsActive"/> state of the control. </summary>
        public void Toggle() =>
            this.IsActive = !this.IsActive;

        /// <summary> Sets the <see cref="IsActive"/> state of the control to <see langword="true"/>. </summary>
        public void Activate() =>
            this.IsActive = true;

        /// <summary> Sets the <see cref="IsActive"/> state of the control to <see langword="false"/>. </summary>
        public void Deactivate() =>
            this.IsActive = false;

        #endregion

        #region IPrevent Implementation

        public bool Prevent() => this.IsPreventing;

        public void SetPrevent(bool value) => this.IsPreventing = value;

        protected bool IsPreventing { get; set; }

        #endregion

        #region IEnumerable Implementation

        public IEnumerator<IControl<TUpdate, TDraw>> GetEnumerator() =>
            this.Controls.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();

        #endregion
    }
}