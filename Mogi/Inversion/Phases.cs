
namespace Mogi.Inversion
{
    /// <summary> Indicates a phase of processing. </summary>
    public interface IPhase
    {
    }

    public class CriticalUpdate : IPhase
    {
    }

    public class NormalUpdate : IPhase
    {
    }

    public class BackgroundDraw : IPhase
    {
    }

    public class ForegroundDraw : IPhase
    {
    }

    public class MenuDraw : IPhase
    {
    }
}