using Godot;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Economy;
using KBTV.Data;
using System.Collections.Generic;
using System.Text;

namespace KBTV.UI
{
	/// <summary>
	/// Post-show summary panel displayed after the broadcast ends.
	/// Shows placeholder stats and allows player to continue to the next night.
	/// </summary>
	public partial class PostShowPanel : Control, IDependent
	{
		private Label _incomeLabel = null!;
		private Label _callersLabel = null!;
		private Label _listenersLabel = null!;
		private Button _continueButton = null!;
		
		// Level-up UI elements
		private PanelContainer _levelUpPanel = null!;
		private Label _levelUpTopicLabel = null!;
		private Label _levelUpTierLabel = null!;
		private Label _levelUpBonusesLabel = null!;
		
		// Level-up state
		private List<TopicXP> _topicsReadyToLevel = new();
		private int _currentLevelUpIndex = 0;

		public override void _Notification(int what) => this.Notify(what);

		public override void _Ready()
		{
			_incomeLabel = GetNode<Label>("VBoxContainer/StatsContainer/IncomeLabel");
			_callersLabel = GetNode<Label>("VBoxContainer/StatsContainer/CallersLabel");
			_listenersLabel = GetNode<Label>("VBoxContainer/StatsContainer/ListenersLabel");
			_continueButton = GetNode<Button>("VBoxContainer/ContinueButton");

			if (_continueButton == null)
			{
				GD.PrintErr("PostShowPanel: ContinueButton not found");
				return;
			}

			_continueButton.Pressed += OnContinuePressed;
			
			// Create level-up UI elements
			CreateLevelUpUI();
		}

		private void CreateLevelUpUI()
		{
			var vBox = GetNode<VBoxContainer>("VBoxContainer");
			
			// Create level-up panel
			_levelUpPanel = new PanelContainer();
			_levelUpPanel.Name = "LevelUpPanel";
			_levelUpPanel.Visible = false; // Hidden by default
			vBox.AddChild(_levelUpPanel);
			vBox.MoveChild(_levelUpPanel, 1); // Insert after header, before stats
			
			var levelUpVBox = new VBoxContainer();
			levelUpVBox.AddThemeConstantOverride("separation", 10);
			_levelUpPanel.AddChild(levelUpVBox);
			
			// Level-up header
			var headerLabel = new Label();
			headerLabel.Text = "★ LEVEL UP! ★";
			headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
			headerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			headerLabel.AddThemeFontSizeOverride("font_size", 18);
			levelUpVBox.AddChild(headerLabel);
			
			// Topic name
			_levelUpTopicLabel = new Label();
			_levelUpTopicLabel.Text = "Topic Name";
			_levelUpTopicLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_levelUpTopicLabel.AddThemeFontSizeOverride("font_size", 16);
			levelUpVBox.AddChild(_levelUpTopicLabel);
			
			// Tier progression
			_levelUpTierLabel = new Label();
			_levelUpTierLabel.Text = "Tier: Skeptic → Curious";
			_levelUpTierLabel.HorizontalAlignment = HorizontalAlignment.Center;
			levelUpVBox.AddChild(_levelUpTierLabel);
			
			// Bonuses
			_levelUpBonusesLabel = new Label();
			_levelUpBonusesLabel.Text = "New Bonuses:\n• Mental: +0% → +5%\n• Screening hints unlocked";
			_levelUpBonusesLabel.HorizontalAlignment = HorizontalAlignment.Center;
			levelUpVBox.AddChild(_levelUpBonusesLabel);
		}

		public void OnResolved()
		{
			// First check for level-ups
			CheckForLevelUps();
			
			// If no level-ups, show income stats
			if (_topicsReadyToLevel.Count == 0)
			{
				UpdateStats();
			}
			else
			{
				ShowNextLevelUp();
			}
		}

		private void CheckForLevelUps()
		{
			var topicManager = DependencyInjection.Get<TopicManager>(this);
			if (topicManager == null) return;
			
			_topicsReadyToLevel.Clear();
			foreach (var topicXP in topicManager.GetAllTopicXPs())
			{
				var nextTierThreshold = TopicXP.GetTierThreshold(topicXP.CurrentTier + 1);
				if (topicXP.XP >= nextTierThreshold && topicXP.CurrentTier < XPTier.TrueBeliever)
				{
					_topicsReadyToLevel.Add(topicXP);
				}
			}
			
			_currentLevelUpIndex = 0;
			GD.Print($"PostShowPanel: Found {_topicsReadyToLevel.Count} topics ready to level up");
		}

		private void ShowNextLevelUp()
		{
			if (_currentLevelUpIndex >= _topicsReadyToLevel.Count)
			{
				// No more level-ups, show income stats
				_levelUpPanel.Visible = false;
				UpdateStats();
				return;
			}
			
			var topicXP = _topicsReadyToLevel[_currentLevelUpIndex];
			var oldTier = topicXP.CurrentTier;
			var newTier = oldTier + 1;
			
			// Level up the topic (preserves overflow XP)
			topicXP.LevelUp();
			
			// Update UI
			_levelUpTopicLabel.Text = topicXP.TopicName;
			_levelUpTierLabel.Text = $"{TopicXP.GetTierName(oldTier)} → {TopicXP.GetTierName(newTier)}";
			
			// Show bonuses
			var bonuses = new System.Text.StringBuilder("New Bonuses:\n");
			bonuses.Append($"• Mental: +{(int)(TopicXP.GetMentalBonusForTier(oldTier) * 100)}% → +{(int)(TopicXP.GetMentalBonusForTier(newTier) * 100)}%\n");
			
			if (newTier >= XPTier.Interested && oldTier < XPTier.Interested)
				bonuses.Append("• Screening hints unlocked\n");
			if (newTier >= XPTier.Believer && oldTier < XPTier.Believer)
				bonuses.Append("• Better caller pool available\n");
			if (newTier >= XPTier.TrueBeliever && oldTier < XPTier.TrueBeliever)
				bonuses.Append("• Expert guests available\n");
			
			_levelUpBonusesLabel.Text = bonuses.ToString();
			
			_levelUpPanel.Visible = true;
			_currentLevelUpIndex++;
			
			// Update button text
			_continueButton.Text = _currentLevelUpIndex >= _topicsReadyToLevel.Count ? "Continue" : "Awesome!";
			
			GD.Print($"PostShowPanel: Showing level-up for {topicXP.TopicName}: {oldTier} → {newTier}");
		}

		private void UpdateStats()
		{
			var economyManager = DependencyInjection.Get<EconomyManager>(this);
			var listenerManager = DependencyInjection.Get<ListenerManager>(this);

			int income = economyManager?.CurrentMoney ?? 0;
			int peakListeners = listenerManager?.PeakListeners ?? 0;

			if (_incomeLabel != null)
			{
				_incomeLabel.Text = $"Income: ${income}";
			}

			if (_callersLabel != null)
			{
				_callersLabel.Text = $"Show Complete!";
			}

			if (_listenersLabel != null)
			{
				_listenersLabel.Text = $"Peak Listeners: {peakListeners}";
			}
		}

		private void OnContinuePressed()
		{
			if (_levelUpPanel.Visible && _currentLevelUpIndex < _topicsReadyToLevel.Count)
			{
				// Show next level-up
				ShowNextLevelUp();
			}
			else
			{
				// Advance to next night (returns to PreShow)
				var gameStateManager = DependencyInjection.Get<GameStateManager>(this);
				gameStateManager?.AdvancePhase();
			}
		}
	}
}
