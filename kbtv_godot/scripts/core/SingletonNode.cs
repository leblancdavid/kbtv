using Godot;

namespace KBTV.Core
{
    /// <summary>
    /// Base class for singleton Nodes in Godot.
    /// Handles Instance property and duplicate removal automatically.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type</typeparam>
    public abstract partial class SingletonNode<T> : Node where T : Node
    {
        public static T Instance { get; private set; }

        public override void _Ready()
        {
            if (Instance != null && Instance != this)
            {
                QueueFree();
                return;
            }
            Instance = (T)(object)this;
            OnSingletonReady();
        }

        /// <summary>
        /// Called after singleton is established. Override instead of _Ready().
        /// </summary>
        protected virtual void OnSingletonReady() { }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}