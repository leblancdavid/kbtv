using System.Reflection;
using Godot;
using KBTV.Core;
using KBTV.UI;
using KBTV.Monitors;
using KBTV.Dialogue;

namespace KBTV
{
    public partial class Main : Node2D
    {
        private ServiceProviderRoot _serviceProviderRoot;

        public override void _Ready()
        {
            GD.Print("Main: Game scene loaded, initializing ServiceProviderRoot");
            
            // Get the ServiceProviderRoot child and initialize it
            _serviceProviderRoot = GetNode<ServiceProviderRoot>("ServiceProviderRoot");
            _serviceProviderRoot.Initialize();
            
            GD.Print("Main: ServiceProviderRoot initialized successfully");
            
            // Instantiate UI managers after services are initialized
            _serviceProviderRoot.AddChild(new PreShowUIManager());
            _serviceProviderRoot.AddChild(new TabContainerManager());
            _serviceProviderRoot.AddChild(new PostShowUIManager());
            
            GD.Print("Main: UI managers instantiated successfully");

            // Add monitors after services are initialized to avoid dependency injection timing issues
            _serviceProviderRoot.AddChild(new CallerMonitor());
            _serviceProviderRoot.AddChild(new VernStatsMonitor());
            _serviceProviderRoot.AddChild(new ScreeningMonitor());
            _serviceProviderRoot.AddChild(new ConversationDisplay());
            _serviceProviderRoot.AddChild(new InputHandler());
            
            GD.Print("Main: Monitors instantiated successfully");

            // Finish loading phase and transition to PreShow
            _serviceProviderRoot.GameStateManager.FinishLoading();
        }
    }
}
