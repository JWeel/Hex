using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Framework;
using System;

namespace Mogi.Inversion
{
    /// <summary> Exposes events that should be raised when an instance implementing this interface handles the corresponding tasks. </summary>
    public interface IRoot
    {
        /// <summary> An update event to which dependencies can subscribe. It is meant to be the first event raised in <see cref="Game.Update(GameTime)"/>. </summary>
        EventPhaseMap<GameTime> OnUpdate { get; set; }

        /// <summary> A draw event to which dependencies can subscribe. It is meant to be the first event raised in <see cref="Game.Draw(GameTime)"/>. </summary>
        EventPhaseMap<SpriteBatch> OnDraw { get; set; }
    }

    /// <summary> Exposes an event that should be raised when an instance implementing this interface terminates. </summary>
    public interface ITerminate
    {
        event Action OnTerminate;
    }

    /// <summary> Exposes a method that should load dependencies. </summary>
    public interface IRegister
    {
        void Register(DependencyMap dependencyMap);
    }

    /// <summary> Exposes a method that should update state. </summary>
    public interface IUpdate<T> where T : LogicalPhase
    {
        void Update(GameTime gameTime);
    }

    /// <summary> Exposes a method that should draw state. </summary>
    public interface IDraw<T> where T : LogicalPhase
    {
        void Draw(SpriteBatch spriteBatch);
    }

    /// <summary> Exposes a method that should react to window resizing. </summary>
    public interface IResize
    {
        void Resize(ClientWindow window);
    }

    public interface IPrevent
    {
        bool Prevent();
    }
}