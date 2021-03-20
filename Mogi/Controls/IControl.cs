using Mogi.Inversion;

namespace Mogi.Controls
{
    /// <summary> Defines a graphical user interface element. </summary>
    public interface IControl : IUpdate, IDraw, IPrioritize
    {
        bool IsActive { get; }
    }
}