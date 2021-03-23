using Mogi.Inversion;

namespace Mogi.Controls
{
    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl : IUpdate<NormalUpdate>, IDraw<MenuDraw>
    {
        bool IsActive { get; }
    }
}