using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BashoKit.Pooling {
    public static class ViewPoolExtensions {
        public static void RegisterViewPoolFactory<TView>(this IContainerBuilder builder, TView prefab, ViewPoolConfig poolConfig, Lifetime lifetime)
            where TView : MonoBehaviour, IViewPool {
            builder.RegisterFactory<Transform, TView>(resolver => { return transform => resolver.Instantiate(prefab, transform); }, lifetime);
            builder.RegisterEntryPoint<ViewPool<TView>>().WithParameter(poolConfig);
        }

        /// Register a UI slots pool, the default settings imply that the object active state is toggled and the object is reparented to the pool root when returned.
        public static void RegisterUISlotsPool<TView>(this IContainerBuilder builder, TView prefab, int initialSize, Lifetime lifetime = Lifetime.Singleton)
            where TView : MonoBehaviour, IViewPool {
            var poolName = prefab.name + "Pool";
            var config = new ViewPoolConfig(
                initialSize,
                poolName,
                PoolReleaseStrategy.DefaultUIElement,
                PoolRetrieveStrategy.DefaultUIElement
            );

            builder.RegisterViewPoolFactory(prefab, config, lifetime);
        }

        /// Register a world entity pool, the default settings imply that the object is moved out of bounds when returned and the object is moved to given coords when retrieved.
        public static void RegisterWorldEntityPool<TView>(this IContainerBuilder builder, TView prefab, int initialSize, Lifetime lifetime = Lifetime.Singleton)
            where TView : MonoBehaviour, IViewPool {
            var poolName = prefab.name + "Pool";
            var config = new ViewPoolConfig(
                initialSize,
                poolName,
                PoolReleaseStrategy.DefaultWorldEntity,
                PoolRetrieveStrategy.DefaultWorldEntity
            );

            builder.RegisterViewPoolFactory(prefab, config, lifetime);
        }
    }
}