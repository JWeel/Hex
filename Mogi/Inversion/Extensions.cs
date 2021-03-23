using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            Func<bool> preventFunc = instance is IPrevent preventer ? preventer.Prevent : () => false;
            var type = instance.GetType();

            if (type.TryGetSubscriber<T, GameTime>(instance, typeof(IUpdate<>), nameof(IUpdate<IPhase>.Update), out var update))
            {
                root.OnUpdate += update;
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnUpdate -= update;
            }
            if (type.TryGetSubscriber<T, SpriteBatch>(instance, typeof(IDraw<>), nameof(IDraw<IPhase>.Draw), out var draw))
            {
                root.OnDraw += draw;
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnDraw -= draw;
            }
            // if (instance is IResize resizer)
            // {
            //     root.OnResize += (resizer.Resize, getPriority, preventFunc);
            //     if (instance is ITerminate terminator)
            //         terminator.OnTerminate += () => root.OnResize -= resizer.Resize;
            // }
            return instance;
        }

        private static bool TryGetSubscriber<TInstance, TParameter>(this Type root, TInstance instance, Type genericTypeDefinition, string actionName, 
            out (Type, Action<TParameter>) subscriber)
        {
            var interfaceType = root.GetInterfaces()
                .Where(interfaceType => interfaceType.IsGenericType)
                .Where(interfaceType => (interfaceType.GetGenericTypeDefinition() == genericTypeDefinition))
                .FirstOrDefault();
            if (interfaceType == null)
            {
                subscriber = default;
                return false;
            }
            var phaseType = interfaceType.GenericTypeArguments.First();
            var action = interfaceType.GetMethods()
                .First(x => (x.Name == actionName))
                .CreateDelegate<Action<TParameter>>(instance);
            subscriber = (phaseType, action);
            return true;
        }

        #endregion
    }
}