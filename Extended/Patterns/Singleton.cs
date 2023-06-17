
namespace Extended.Patterns
{
    public class Singleton<T>
        where T : new()
    {
        #region Properties

        public T Instance { get; } = new T();

        #endregion
    }
}