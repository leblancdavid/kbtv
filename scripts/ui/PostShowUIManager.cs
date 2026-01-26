using Godot;
using KBTV.Core;

namespace KBTV.UI
{
	/// <summary>
	/// Manages the PostShow UI layer - displays summary after broadcast ends.
	/// </summary>
	public partial class PostShowUIManager : Node
	{
		public override void _Ready()
		{
			base._Ready();
			GD.Print("PostShowUIManager: Initializing with services...");
			CreatePostShowUI();
		}

		private void CreatePostShowUI()
		{
			var uiManager = ServiceRegistry.Instance?.UIManager;
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
				GD.Print("PostShowUIManager: PostShowPanel loaded successfully");
			}
			else
			{
				GD.PrintErr("PostShowUIManager: Failed to load PostShowPanel.tscn");
			}
			GD.Print("PostShowUIManager: Initialization complete");
		}
	}
}
