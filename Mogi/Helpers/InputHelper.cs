using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mogi.Enums;
using Mogi.Extensions;
using Mogi.Framework;
using Mogi.Inversion;
using System.Linq;

namespace Mogi.Helpers
{
    /// <summary> Provides methods to determine keyboard and mouse state. </summary>
    public class InputHelper : IUpdate<CriticalUpdate>
    {
        #region Constructors

        /// <summary> Initializes a new instance. </summary>
        public InputHelper(ClientWindow client)
        {
            this.Client = client;
        }

        #endregion

        #region Properties

        protected ClientWindow Client { get; }

        protected KeyboardState CurrentKeyboard { get; set; }
        protected KeyboardState PreviousKeyboard { get; set; }

        protected MouseState CurrentMouse { get; set; }
        protected MouseState PreviousMouse { get; set; }

        #endregion

        #region Public Methods

        /// <summary> Determines whether a specified key was previously up and is now down. </summary>
        public bool KeyPressed(Keys key) =>
            (this.PreviousKeyboard.IsKeyUp(key) && this.CurrentKeyboard.IsKeyDown(key));

        /// <summary> Determines whether all specified keys were previously up and are now down. </summary>
        public bool KeysPressed(params Keys[] keys) => keys.All(this.KeyPressed);

        /// <summary> Determines whether any of the specified keys were previously up and are now down. </summary>
        public bool KeysPressedAny(params Keys[] keys) => keys.Any(this.KeyPressed);

        /// <summary> Determines whether a specified key was previously down and is now up. </summary>
        public bool KeyReleased(Keys key) =>
            (this.PreviousKeyboard.IsKeyDown(key) && this.CurrentKeyboard.IsKeyUp(key));

        /// <summary> Determines whether all specified keys were previously down and are now up. </summary>
        public bool KeysReleased(params Keys[] keys) => keys.All(this.KeyReleased);

        /// <summary> Determines whether any of the specified keys were previously down and are now up. </summary>
        public bool KeysReleasedAny(params Keys[] keys) => keys.Any(this.KeyReleased);

        /// <summary> Determines whether a specified key is currently down. </summary>
        public bool KeyDown(Keys key) => this.CurrentKeyboard.IsKeyDown(key);

        /// <summary> Determines whether all specified keys are currently down. </summary>
        public bool KeysDown(params Keys[] keys) => keys.All(this.KeyDown);

        /// <summary> Determines whether any of the specified keys are currently down. </summary>
        public bool KeysDownAny(params Keys[] keys) => keys.Any(this.KeyDown);

        /// <summary> Determines whether a specified key is currently up. </summary>
        public bool KeyUp(Keys key) => this.CurrentKeyboard.IsKeyUp(key);

        /// <summary> Determines whether all specified keys are currently up. </summary>
        public bool KeysUp(params Keys[] keys) => keys.All(this.KeyUp);

        /// <summary> Determines whether any of the specifed keys are currently up. </summary>
        public bool KeysUpAny(params Keys[] keys) => keys.Any(this.KeyUp);

        /// <summary> Determines whether the specifed mouse button is currently down. </summary>
        public bool MouseDown(MouseButton button) =>
            this.CurrentMouse.GetButtonState(button).IsPressed();

        /// <summary> Determines whether the specified mouse button is currently up. </summary>
        public bool MouseUp(MouseButton button) =>
            this.CurrentMouse.GetButtonState(button).IsReleased();

        /// <summary> Determines whether the specified mouse button was previously up and is now down. </summary>
        public bool MousePressed(MouseButton button) =>
            (!this.PreviousMouse.GetButtonState(button).IsPressed() && this.CurrentMouse.GetButtonState(button).IsPressed());

        /// <summary> Determines whether the specified mouse button was previously down and is now up. </summary>
        public bool MouseReleased(MouseButton button) =>
            (!this.PreviousMouse.GetButtonState(button).IsReleased() && this.CurrentMouse.GetButtonState(button).IsReleased());

        /// <summary> Determines whether the mouse scroll wheel was rolled. </summary>
        public bool MouseScrolled() =>
            (this.PreviousMouse.ScrollWheelValue != this.CurrentMouse.ScrollWheelValue);

        /// <summary> Determines whether the mouse scroll wheel was rolled up. </summary>
        public bool MouseScrolledUp() =>
            (this.CurrentMouse.ScrollWheelValue > this.PreviousMouse.ScrollWheelValue);

        /// <summary> Determines whether the mouse scroll wheel was rolled down. </summary>
        public bool MouseScrolledDown() =>
            (this.CurrentMouse.ScrollWheelValue < this.PreviousMouse.ScrollWheelValue);

        /// <summary> Determines whether the mouse position was changed. </summary>
        public bool MouseMoved() =>
            (this.PreviousMousePoint != this.CurrentMousePoint);

        /// <summary> Retrieves the current mouse position. </summary>
        public Point CurrentMousePoint => this.CurrentMouse.Position;

        /// <summary> Retrieves the previous mouse position. </summary>
        public Point PreviousMousePoint => this.PreviousMouse.Position;

        /// <summary> Retrieves the current mouse position as a vector. </summary>
        public Vector2 CurrentMouseVector => this.CurrentMousePoint.ToVector2();

        /// <summary> Retrieves the previous mouse position as a vector. </summary>
        public Vector2 PreviousMouseVector => this.PreviousMousePoint.ToVector2();

        /// <summary> Retrieves the previous mouse position relative to virtual resolution as a vector. </summary>
        public Vector2 PreviousVirtualMouseVector => this.Client.Translate(this.PreviousMouseVector);

        /// <summary> Retrieves the current mouse position relative to virtual resolution as a vector. </summary>
        public Vector2 CurrentVirtualMouseVector => this.Client.Translate(this.CurrentMouseVector);

        #endregion

        #region Protected Methods

        public void Update(GameTime gameTime)
        {
            this.PreviousKeyboard = this.CurrentKeyboard;
            this.CurrentKeyboard = Keyboard.GetState();

            this.PreviousMouse = this.CurrentMouse;
            this.CurrentMouse = Mouse.GetState();
        }

        #endregion
    }
}