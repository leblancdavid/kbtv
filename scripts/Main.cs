using System.Reflection;
using Godot;
using KBTV.Core;
using KBTV.UI;

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
            AddChild(new PreShowUIManager());
            AddChild(new TabContainerManager());
            AddChild(new PostShowUIManager());
            
            GD.Print("Main: UI managers instantiated successfully");
        }
    }
}
