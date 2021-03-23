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

            if (type.TryGetGenericInterface(typeof(IUpdate<>), out var updateInterface))
            {
                var phase = updateInterface.GenericTypeArguments.First();
                var update = updateInterface.GetMethods().First(x => (x.Name == "Update")).CreateDelegate<Action<GameTime>>(instance);
                root.OnUpdate += (phase, update);
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnUpdate -= (phase, update);
            }
            if (type.TryGetGenericInterface(typeof(IDraw<>), out var drawInterface))
            {
                var phase = drawInterface.GenericTypeArguments.First();
                var draw = drawInterface.GetMethods().First(x => (x.Name == "Draw")).CreateDelegate<Action<SpriteBatch>>(instance);
                root.OnDraw += (phase, draw);
                if (instance is ITerminate terminator)
                    terminator.OnTerminate += () => root.OnDraw -= (phase, draw);
            }
            // if (instance is IResize resizer)
            // {
            //     root.OnResize += (resizer.Resize, getPriority, preventFunc);
            //     if (instance is ITerminate terminator)
            //         terminator.OnTerminate += () => root.OnResize -= resizer.Resize;
            // }
            return instance;
        }

        private static bool TryGetGenericInterface(this Type root, Type genericTypeDefinition, out Type interfaceType)
        {
            interfaceType = 
                root.GetInterfaces()
                    .Where(interfaceType => interfaceType.IsGenericType)
                    .Where(interfaceType => (interfaceType.GetGenericTypeDefinition() == genericTypeDefinition))
                    .FirstOrDefault();
            return (interfaceType != null);
        }

        #endregion
    }
}