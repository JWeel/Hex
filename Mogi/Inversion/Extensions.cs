using Extended.Extensions;
using System;
using System.Linq;

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
            //     root.OnResize += (resizer.Resize, getPriority, preventFunc);
            //     if (instance is ITerminate terminator)
            //         terminator.OnTerminate += () => root.OnResize -= resizer.Resize;
            // }
            return instance;
        }

        #endregion

        #region Prioritizable Event Extensions

        /// <summary> Invokes attached delegates while checking prevention predicates in the following order: all prevented delegates are invoked first in order of priority, all delegates that were not prevented are invoked afterwards in order of priority.
        /// <br/> This allows higher priority components to draw on top of lower priority prevented components in <see cref="Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred"/> drawing mode. </summary>
        /// <param name="arg"> The argument that is passed into the delegates. </param>
        public static void InvokeForDrawing<T>(this PrioritizableEvent<T> source, T arg)
        {
            var preventing = false;
            source.GetInvocationList()
                // preventer should be part of unprevented group, so need track prevention state before and after
                // 'before' state goes to Prevented, 'after' state is set to the bool if it is not already true
                .Select(x => (Delegate: x.Delegate, Prevented: preventing, preventing = preventing || x.Prevent()))
                .GroupBy(x => x.Prevented)
                .Select(group => group.Select(x => x.Delegate).ToArray())
                .ToArray()
                .Reverse()
                .Each(group => group.Each(x => x?.Invoke(arg)));
            // Note for caching this sequence:
            // would need to listen to prevent predicate during Update or find way to loop over components (not ioc)
            // loop by priority
            //      if component is preventing
            //          if was previously preventing
            //              return/break
            //          mark cache to be refreshed, return/break
            //      else if was previously preventing
            //          mark cache to be refreshed, return/break
            // will be tricky to do this with ioc:
            //      callback on changed Prevent() backing bool can notify, but there may be higher prio overruling it
        }

        #endregion
    }
}