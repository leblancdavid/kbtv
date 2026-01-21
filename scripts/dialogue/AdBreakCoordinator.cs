using System;
using Godot;
using KBTV.Ads;
using KBTV.Core;

namespace KBTV.Dialogue
{
    public partial class AdBreakCoordinator : Node
    {
        private AdManager _adManager;
        private VernDialogueTemplate _vernDialogue;

        private int _currentAdIndex = 0;
        private int _totalAdsInBreak = 0;
        private bool _adBreakActive = false;
        public string? CurrentAdSponsor { get; private set; }

        public bool IsAdBreakActive => _adBreakActive;

        public AdBreakCoordinator(AdManager adManager, VernDialogueTemplate vernDialogue)
        {
            _adManager = adManager;
            _vernDialogue = vernDialogue;
        }

        public BroadcastLine GetAdBreakLine()
        {
            if (!_adBreakActive)
            {
                CurrentAdSponsor = null;
                return BroadcastLine.None();
            }

            if (_currentAdIndex < _totalAdsInBreak)
            {
                var adType = DetermineAdType(_adManager?.CurrentListeners ?? 100);
                var sponsorName = AdData.GetAdTypeDisplayName(adType);

                CurrentAdSponsor = sponsorName;

                // Add to transcript
                var transcriptRepository = ServiceRegistry.Instance.TranscriptRepository;
                transcriptRepository?.AddEntry(new TranscriptEntry(
                    Speaker.System,
                    $"Ad sponsored by {sponsorName}",
                    ConversationPhase.Intro,
                    "system"
                ));

                _currentAdIndex++;
                string adText = _totalAdsInBreak > 1 ? $"AD BREAK ({_currentAdIndex})" : "AD BREAK";
                return BroadcastLine.Ad(adText);
            }
            else
            {
                _adBreakActive = false;
                CurrentAdSponsor = null;
                _adManager?.EndCurrentBreak();
                return BroadcastLine.None();
            }
        }

        private AdType DetermineAdType(int listenerCount)
        {
            // Simple tiered system based on listener count
            if (listenerCount >= 1000) return AdType.PremiumSponsor;
            if (listenerCount >= 500) return AdType.NationalSponsor;
            if (listenerCount >= 200) return AdType.RegionalBrand;
            return AdType.LocalBusiness;
        }

        public void OnAdBreakStarted()
        {
            _adBreakActive = true;
            _currentAdIndex = 0;
            _totalAdsInBreak = _adManager?.CurrentBreakSlots ?? 0;
        }

        public void OnAdBreakEnded()
        {
            _adBreakActive = false;
            _currentAdIndex = 0;
            _totalAdsInBreak = 0;
            CurrentAdSponsor = null;
        }
    }
}