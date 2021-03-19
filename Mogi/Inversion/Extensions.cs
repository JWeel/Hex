namespace Mogi.Inversion
{
    public static class Extensions
    {
        #region Attach

        public static T Attach<T>(this IRoot root, T instance)
            where T : class
        {
            if (instance is IUpdate updater)
            {
                root.OnUpdate += updater.Update;
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnUpdate -= updater.Update;
            }
            if (instance is IDraw drawer)
            {
                root.OnDraw += drawer.Draw;
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnDraw -= drawer.Draw;
            }
            if (instance is IResize resizer)
            {
                root.OnResize += resizer.Resize;
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnResize -= resizer.Resize;
            }

            return instance;   
        }
            
        #endregion
    }
}