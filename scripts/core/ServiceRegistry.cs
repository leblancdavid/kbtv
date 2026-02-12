using System;
using System.Collections.Generic;
using Godot;
using KBTV.Items;
using KBTV.Economy;
using KBTV.Managers;

namespace KBTV.Core
{
    /// <summary>
    /// Minimal ServiceRegistry for backwards compatibility.
    /// New code should use AutoInject instead.
    /// </summary>
    public class ServiceRegistry
    {
        private static ServiceRegistry? _instance;
        public static ServiceRegistry Instance => _instance ??= new ServiceRegistry();
        public static bool IsInitialized => _instance != null;

        private readonly Dictionary<Type, object> _services = new();

        public T? Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }

        public void Register<TInterface, TImplementation>(TImplementation instance) where TInterface : class where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = instance;
        }

        public void RegisterSelf<TInterface>(object instance) where TInterface : class
        {
            _services[typeof(TInterface)] = instance;
        }

        public void Unregister<TInterface>()
        {
            _services.Remove(typeof(TInterface));
        }

        public void Clear()
        {
            _services.Clear();
        }

        // Convenience properties for common services
        public IEvidenceAnalyzer? EvidenceAnalyzer => Get<IEvidenceAnalyzer>();
        public IEvidenceCabinet? EvidenceCabinet => Get<IEvidenceCabinet>();
        public IEvidenceWebsite? EvidenceWebsite => Get<IEvidenceWebsite>();
        public EconomyManager? EconomyManager => Get<EconomyManager>();
        public IListenerManager? ListenerManager => Get<IListenerManager>();
    }
}
