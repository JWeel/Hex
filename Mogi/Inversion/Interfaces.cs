using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Framework;
using System;

namespace Mogi.Inversion
{
    /// <summary> Exposes events that should be raised on each iteration of the game loop. </summary>
    public interface IRoot
    {
        /// <summary> An update event to which dependencies can subscribe. It should be raised in <see cref="Game.Update(GameTime)"/>. </summary>
        PhasedEvent<GameTime> OnUpdate { get; set; }

        /// <summary> A draw event to which dependencies can subscribe. It should be raised in <see cref="Game.Draw(GameTime)"/>. </summary>
        PhasedEvent<SpriteBatch> OnDraw { get; set; }
    }

    /// <summary> Exposes a method that should load dependencies. </summary>
    public interface IRegister
    {
        /// <summary> Loads dependencies using a specified dependency map. </summary>
        void Register(DependencyHandler dependencyHandler);
    }

    /// <summary> Exposes a method that should update state. </summary>
    public interface IUpdate
    {
        /// <summary> Updates state using a snapshot of elapsed game time. </summary>
        void Update(GameTime gameTime);
    }

    /// <summary> Exposes a method that should update state in a defined phase. </summary>
    public interface IUpdate<T>
        where T : IPhase
    {
        /// <summary> Updates state using a snapshot of elapsed game time. </summary>
        void Update(GameTime gameTime);
    }

    /// <summary> Exposes a method that should draw state. </summary>
    public interface IDraw
    {
        /// <summary> Draws to a specified spritebatch. </summary>
        void Draw(SpriteBatch spriteBatch);
    }

    /// <summary> Exposes a method that should draw state in a defined phase. </summary>
    public interface IDraw<T>
        where T : IPhase
    {
        /// <summary> Draws to a specified spritebatch. </summary>
        void Draw(SpriteBatch spriteBatch);
    }

    /// <summary> Exposes a method that should react to window resizing. </summary>
    public interface IResize<T> where T : IPhase
    {
        /// <summary> Recalculates sizes using a specified client window. </summary>
        void Resize(ClientWindow window);
    }

    /// <summary> Exposes an event that should be raised when an instance implementing this interface terminates. </summary>
    public interface ITerminate
    {
        /// <summary> An event that should be raised when this instance terminates. </summary>
        event Action OnTerminate;
    }

    /// <summary> Exposes a flag that indicates whether processing should occur. </summary>
    public interface IActivate
    {
        /// <summary> Determines whether this instance should be processed. </summary>
        bool IsActive { get; }

        /// <summary> Sets this instance to an active state. </summary>
        void Activate();

        /// <summary> Sets this instance to an inactive state. </summary>
        void Deactivate();
    }

    /// <summary> Defines a type which will be used when registering instead of the class type. </summary>
    /// <typeparam name="T"> The type the inheritor will be registered as. </typeparam>
    public interface IRegisterAs<T>
    {
    }
}