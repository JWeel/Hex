using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.State;
using System;

namespace Mogi.Inversion
{
    /// <summary> Exposes events that should be raised when an instance implementing this interface handles the corresponding tasks. </summary>
    public interface IRoot
    {
        event Action<GameTime> OnUpdate;
        event Action<SpriteBatch> OnDraw;
        event Action<WindowState> OnResize;
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
        void Update(GameTime gameTime);
    }

    /// <summary> Exposes a method that should draw state. </summary>
    public interface IDraw
    {
        void Draw(SpriteBatch spriteBatch);
    }

    /// <summary> Exposes a method that should react to window resizing. </summary>
    public interface IResize
    {
        void Resize(WindowState window);
    }
}