using UnityEngine;
using VContainer.Unity;

namespace VContainer.ObjectPool
{
	public static class ContainerBuilderExtensions
	{
		public static void RegisterViewPoolFactory<TView>(this IContainerBuilder builder, TView prefab,
				ViewPoolConfig poolConfig, Lifetime lifetime)
				where TView : MonoBehaviour, IViewPool
		{
			builder.RegisterFactory<Transform, TView>(
					resolver => { return transform => resolver.Instantiate(prefab, transform); }, lifetime);

			builder.RegisterEntryPoint<ViewPool<TView>>().WithParameter(poolConfig);
		}
	}
}