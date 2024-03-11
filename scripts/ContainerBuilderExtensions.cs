using System;
using Project.Runtime.Core.Factory;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Runtime.Core.Helpers.VContainer
{
	public static class ContainerBuilderExtensions
	{
		public static void RegisterViewPoolFactory<TView, TPool>(this IContainerBuilder builder, TView prefab, ViewPoolConfig poolConfig, Lifetime lifetime)
			where TView : MonoBehaviour, IViewPool 
			where TPool : IViewPool<TView>
		{
			builder.RegisterFactory<Transform, TView>(resolver =>
			{
				return transform => resolver.Instantiate(prefab, transform);
			}, lifetime);

			builder.RegisterEntryPoint<TPool>().WithParameter(poolConfig);
		}
	}
}