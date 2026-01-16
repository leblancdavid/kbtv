using Godot;

namespace KBTV.UI
{
    public interface IUIManager
    {
        void RegisterPreShowLayer(CanvasLayer layer);
        void RegisterLiveShowLayer(CanvasLayer layer);
    }
}
