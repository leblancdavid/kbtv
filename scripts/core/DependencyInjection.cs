using System;
using System.Collections.Generic;
using Godot;

namespace KBTV.Core
{
	/// <summary>
	/// Custom dependency injection framework to replace broken AutoInject source generators.
	/// Provides tree-scoped dependency resolution for Godot nodes.
	/// </summary>
	public static class DependencyInjection
	{
		private static readonly Dictionary<Type, Func<Node, object>> _resolvers = new();

		/// <summary>
		/// Register a service resolver for a specific type.
		/// </summary>
		public static void Register<TService>(Func<Node, TService> resolver) where TService : class
		{
			_resolvers[typeof(TService)] = resolver;
		}

		/// <summary>
		/// Get a service instance for the given node.
		/// Searches up the scene tree for providers.
		/// </summary>
		public static TService Get<TService>(Node node) where TService : class
		{
			// First try registered resolvers
			if (_resolvers.TryGetValue(typeof(TService), out var resolver))
			{
				return (TService)resolver(node);
			}

			// Search up the scene tree for providers
			var current = node;
			while (current != null)
			{
				if (current is IProvide<TService> provider)
				{
					return provider.Value();
				}
				current = current.GetParent();
			}

			throw new InvalidOperationException($"No provider found for service {typeof(TService).Name}");
		}

		/// <summary>
		/// Extension method to provide DependOn<T>() functionality for any Node.
		/// </summary>
		public static TService DependOn<TService>(this Node node) where TService : class
		{
			return Get<TService>(node);
		}

		/// <summary>
		/// Extension method to provide Notify functionality for IDependent nodes.
		/// Replaces Chickensoft.AutoInject's IAutoNode.Notify() method.
		/// </summary>
		public static void Notify(this Node node, int what)
		{
			if (node is IDependent dependent)
			{
				switch (what)
				{
					case (int)Node.NotificationReady:
						// Call OnResolved() when node is ready (simplified pattern for custom DI)
						dependent.OnResolved();
						break;
					// Add other notification types as needed
				}
			}
		}

		/// <summary>
		/// Extension method to provide services for nodes that implement IProvide<T>.
		/// This makes all provided services available to descendants.
		/// </summary>
		public static void Provide(this Node node)
		{
			// Mark that this node is providing services
			// The actual resolution happens in DependOn<T>()
		}
	}

	/// <summary>
	/// Interface for service providers (replaces Chickensoft.AutoInject.IProvide<T>).
	/// </summary>
	public interface IProvide<TService> where TService : class
	{
		TService Value();
	}

	/// <summary>
	/// Interface for dependent nodes (replaces Chickensoft.AutoInject.IDependent).
	/// </summary>
	public interface IDependent
	{
		/// <summary>
		/// Called when all dependencies are resolved.
		/// </summary>
		void OnResolved();
	}

	/// <summary>
	/// Interface for auto-managed nodes (replaces Chickensoft.AutoInject.IAutoNode).
	/// Simplified version without mixins.
	/// </summary>
	public interface IAutoNode : IDependent
	{
	}
}
