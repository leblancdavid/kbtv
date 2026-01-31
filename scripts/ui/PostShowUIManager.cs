using Godot;
using KBTV.Core;

namespace KBTV.UI
{
	/// <summary>
	/// Manages the PostShow UI layer - displays summary after broadcast ends.
	/// </summary>
	public partial class PostShowUIManager : Node, IDependent
	{
		public override void _Notification(int what) => this.Notify(what);
		public override void _Ready()
		{
			base._Ready();
		}

		public void OnResolved()
		{
			CreatePostShowUI();
		}

		private void CreatePostShowUI()
		{
			var uiManager = DependencyInjection.Get<IUIManager>(this);
			if (uiManager == null)
			{
				GD.PrintErr("PostShowUIManager: UIManager not available");
				return;
			}

			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = "PostShowCanvasLayer";
			canvasLayer.Layer = 10;
			canvasLayer.Visible = false;
			AddChild(canvasLayer);

			uiManager.RegisterPostShowLayer(canvasLayer);

			var panelScene = GD.Load<PackedScene>("res://scenes/ui/PostShowPanel.tscn");
			if (panelScene != null)
			{
				var panel = panelScene.Instantiate();
				canvasLayer.AddChild(panel);
			}
			else
			{
				GD.PrintErr("PostShowUIManager: Failed to load PostShowPanel.tscn");
			}
		}
	}
}
