using System;
using Microsoft.Xna.Framework;

namespace Mogi.Inversion
{
    public static class Extensions
    {
        #region Attach

        public static T Attach<T>(this IRoot root, T instance)
            where T : class
        {
            Func<int> priorityFunc = instance is IPrioritize prioritizer ? prioritizer.GetPriority : () => 0;
            Func<bool> preventFunc = instance is IPrevent preventer ? preventer.Prevent : () => false;
            if (instance is IUpdate updater)
            {
                root.OnUpdate += (updater.Update, priorityFunc, preventFunc);
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnUpdate -= updater.Update;
            }
            if (instance is IDraw drawer)
            {
                root.OnDraw += (drawer.Draw, priorityFunc, preventFunc);
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnDraw -= drawer.Draw;
            }
            // if (instance is IResize resizer)
            // {
            //     root.OnResize += (resizer.Resize, getPriority);
            //     if (instance is ITerminate terminator)
            //         terminator.OnTerminate += () => root.OnResize -= resizer.Resize;
            // }
            return instance;
        }

        #endregion
    }
}