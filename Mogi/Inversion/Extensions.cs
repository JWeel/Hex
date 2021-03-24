using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mogi.Framework;
using System;
using System.Linq;

namespace Mogi.Inversion
{
    public static class Extensions
    {
        #region Attach

        /// <summary> Uses the interfaces implemented by the specified <paramref name="instance"/> to subscribes it to events on <paramref name="root"/>. </summary>
        /// <param name="root"> The root instance which exposes events. </param>
        /// <param name="instance"> The instance which will subscribe to events on <paramref name="root"/>. </param>
        /// <typeparam name="T"> The type of the instance which can subscribe to events. </typeparam>
        /// <returns> <paramref name="instance"/> </returns>
        public static T Attach<T>(this IRoot root, T instance)
            where T : class
        {
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
            if (type.TryGetSubscriber<T, ClientWindow>(instance, typeof(IResize<>), nameof(IResize<IPhase>.Resize), out var resize))
            {
                // root.OnResize += resize;
                // if (instance is ITerminate terminator)
                //     terminator.OnTerminate += () => root.OnResize -= resize;
            }
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