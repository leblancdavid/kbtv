using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Persistence;
using KBTV.Screening;
using KBTV.UI;

namespace KBTV.Core
{
    /// <summary>
    /// Global service registry using Godot Autoload pattern.
    /// All core services are registered here for centralized access.
    /// Use this instead of direct singletons for testability and flexibility.
    /// </summary>
    [GlobalClass]
    public partial class ServiceRegistry : Node
    {
        private static ServiceRegistry _instance;
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();

        public static bool IsInitialized { get; private set; } = false;

        public static ServiceRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    GD.PrintErr("ServiceRegistry: Instance not found! Make sure ServiceRegistry is added as an autoload.");
                }
                return _instance;
            }
        }

        public override void _Ready()
        {
            _instance = this;
            IsInitialized = true;

            RegisterCoreServices();

            GD.Print("ServiceRegistry: Initialized");
        }

        private void RegisterCoreServices()
        {
            var repository = new CallerRepository();
            Register<ICallerRepository>(repository);
            NotifyRegistered();

            var screeningController = new ScreeningController();
            Register<IScreeningController>(screeningController);
            NotifyRegistered();

            var arcRepository = new ArcRepository();
            Register<IArcRepository>(arcRepository);
            NotifyRegistered();
        }

        public void NotifyRegistered()
        {
            GD.Print($"ServiceRegistry: Service registered");
        }

        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                GD.PrintErr($"ServiceRegistry: Attempted to register null service for type {typeof(TService).Name}");
                return;
            }

            var type = typeof(TService);
            if (_services.ContainsKey(type))
            {
                GD.Print($"WARNING: ServiceRegistry: Overwriting existing service for type {type.Name}");
            }

            _services[type] = instance;
            GD.Print($"ServiceRegistry: Registered {type.Name}");
        }

        public void RegisterSelf<TService>(TService instance) where TService : class
        {
            if (instance == null)
            {
                GD.PrintErr($"ServiceRegistry: Attempted to register null service for type {typeof(TService).Name}");
                return;
            }

            var type = typeof(TService);
            _services[type] = instance;

            var concreteType = instance.GetType();
            if (concreteType != type)
            {
                _services[concreteType] = instance;
                GD.Print($"ServiceRegistry: Registered {type.Name} (concrete: {concreteType.Name})");
            }
            else
            {
                GD.Print($"ServiceRegistry: Registered {type.Name}");
            }

            NotifyRegistered();
        }

        public void Register<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            _factories[interfaceType] = () => new TImplementation();
            GD.Print($"ServiceRegistry: Registered factory for {interfaceType.Name}");
        }

        public void RegisterFactory<TService>(Func<TService> factory) where TService : class
        {
            if (factory == null)
            {
                GD.PrintErr($"ServiceRegistry: Attempted to register null factory for type {typeof(TService).Name}");
                return;
            }

            var type = typeof(TService);
            _factories[type] = () => factory();
            GD.Print($"ServiceRegistry: Registered factory for {type.Name}");
        }

        public TService Get<TService>() where TService : class
        {
            var type = typeof(TService);

            if (_services.TryGetValue(type, out var service))
            {
                return (TService)service;
            }

            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = factory();
                if (instance != null)
                {
                    _services[type] = instance;
                }
                return (TService)instance;
            }

            GD.PrintErr($"ServiceRegistry: Service not found for type {type.Name}");
            return null!;
        }

        public bool HasService<TService>() where TService : class
        {
            var type = typeof(TService);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }

        public void Unregister<TService>() where TService : class
        {
            var type = typeof(TService);
            _services.Remove(type);
            _factories.Remove(type);
            GD.Print($"ServiceRegistry: Unregistered {type.Name}");
        }

        public void ClearAll()
        {
            _services.Clear();
            _factories.Clear();
            GD.Print("ServiceRegistry: Cleared all services");
        }

        public TService Require<TService>() where TService : class
        {
            var service = Get<TService>();
            if (service == null)
            {
                throw new InvalidOperationException($"Required service {typeof(TService).Name} is not registered");
            }
            return service;
        }

        public ICallerRepository CallerRepository => Get<ICallerRepository>();
        public IScreeningController ScreeningController => Get<IScreeningController>();
        public IConversationManager ConversationManager => Get<IConversationManager>();
        public ITranscriptRepository TranscriptRepository => Get<ITranscriptRepository>();
        public IArcRepository ArcRepository => Get<IArcRepository>();
        public GameStateManager GameStateManager => Get<GameStateManager>();
        public TimeManager TimeManager => Get<TimeManager>();
        public ListenerManager ListenerManager => Get<ListenerManager>();
        public EconomyManager EconomyManager => Get<EconomyManager>();
        public SaveManager SaveManager => Get<SaveManager>();
        public UIManager UIManager => Get<UIManager>();
        public GlobalTransitionManager GlobalTransitionManager => Get<GlobalTransitionManager>();
        public CallerGenerator CallerGenerator => Get<CallerGenerator>();
    }
}
