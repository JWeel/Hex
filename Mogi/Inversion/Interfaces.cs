using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Framework;
using System;

namespace Mogi.Inversion
{
    /// <summary> Exposes events that should be raised when an instance implementing this interface handles the corresponding tasks. </summary>
    public interface IRoot
    {
        /// <summary> An update event to which dependencies can subscribe. It is meant to be raised in <see cref="Game.Update(GameTime)"/>. </summary>
        PrioritizedEvent<GameTime> OnUpdate { get; set; }

        /// <summary> A draw event to which dependencies can subscribe. It is meant to be raised in <see cref="Game.Draw(GameTime)"/>. </summary>
        PrioritizedEvent<SpriteBatch> OnDraw { get; set; }

        // /// <summary> A resize event to which dependencies can subscribe. It is meant to be raised whenever the window is resized.
        // /// <para/> Consider forwarding the client window event:
        // /// <br/> <c>this.ClientWindow.OnResize += () => this.OnResize?.Invoke(this.ClientWindow);</c>  </summary>
        // PrioritizedEvent<ClientWindow> OnResize;
    }

    /// <summary> Exposes an event that should be raised when an instance implementing this interface terminates. </summary>
    public interface ITerminate
    {
        event Action OnTerminate;
    }

    /// <summary> Exposes a method that should load dependencies. </summary>
    public interface ILoad
    {
        void Load(DependencyMap dependencyMap);
    }

    /// <summary> Exposes a method that should update state. </summary>
    public interface IUpdate
    {
        bool Update(GameTime gameTime);
    }

    /// <summary> Exposes a method that should draw state. </summary>
    public interface IDraw
    {
        bool Draw(SpriteBatch spriteBatch);
    }

    /// <summary> Exposes a method that should react to window resizing. </summary>
    public interface IResize
    {
        bool Resize(ClientWindow window);
    }

    /// <summary> Exposes a method that determines priority of updating/drawing. </summary>
    public interface IPrioritize
    {
        int GetPriority();
    }
}