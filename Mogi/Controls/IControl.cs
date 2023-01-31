using Mogi.Inversion;

namespace Mogi.Controls
{
    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl<TUpdate, TDraw> : IUpdate<TUpdate>, IDraw<TDraw>
        where TUpdate : IPhase
        where TDraw : IPhase
    {
        bool IsActive { get; }
    }
}