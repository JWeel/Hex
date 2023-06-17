using Microsoft.Xna.Framework;
using Mogi.Inversion;

namespace Mogi.Controls
{
    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl<TUpdate, TDraw> : IControl, IUpdate<TUpdate>, IDraw<TDraw>
        where TUpdate : IPhase
        where TDraw : IPhase
    {
    }

    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl : IActivate, IUpdate, IDraw
    {
        void Move(Point movement);
    }
}