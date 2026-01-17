#nullable enable

using System.Linq;
using Godot;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Monitors and updates caller patience over time.
    /// Runs as a Node in the scene tree so _Process() is called every frame.
    /// Handles patience drain and disconnection when patience runs out.
    /// </summary>
    public partial class CallerPatienceMonitor : Node
    {
        private ICallerRepository? _repository;

        public override void _Ready()
        {
            if (ServiceRegistry.IsInitialized)
            {
                _repository = ServiceRegistry.Instance.CallerRepository;
            }
        }

        public override void _Process(double delta)
        {
            if (_repository == null)
            {
                return;
            }

            float dt = (float)delta;
            
            var incoming = _repository.IncomingCallers;
            var screening = _repository.CurrentScreening;

            if (incoming.Count > 0)
            {
                foreach (var caller in incoming.ToList())
                {
                    caller.UpdateWaitTime(dt);
                }
            }

            screening?.UpdateWaitTime(dt);
        }
    }
}
