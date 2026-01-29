#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Managers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable for commercial breaks and sponsor content.
    /// </summary>
    public partial class AdExecutable : BroadcastExecutable
    {
        private readonly string _adText;
        private readonly string? _sponsor;
        private readonly string? _audioPath;
        private readonly ListenerManager _listenerManager;

        public AdExecutable(string id, string adText, float duration, EventBus eventBus, ListenerManager listenerManager, IBroadcastAudioService audioService, SceneTree sceneTree, string? sponsor = null, string? audioPath = null) 
            : base(id, BroadcastItemType.Ad, true, duration, eventBus, audioService, sceneTree, new { adText, sponsor, audioPath })
        {
            _adText = adText;
            _sponsor = sponsor;
            _audioPath = audioPath;
            _listenerManager = listenerManager;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            var displayText = GetDisplayText();
            GD.Print($"AdExecutable: Playing commercial - {displayText}");
            
            // Calculate and add revenue (placeholder for now)
            var listenerCount = _listenerManager?.CurrentListeners ?? 100;
            var revenue = CalculateRevenue(listenerCount);
            
            // This would integrate with EconomyManager when available
            // ServiceRegistry.Instance.EconomyManager?.AddRevenue(revenue);
            
            if (!string.IsNullOrEmpty(_audioPath))
            {
                await PlayAudioAsync(_audioPath, cancellationToken);
            }
            else
            {
                // Default ad audio (silent placeholder)
                await DelayAsync(_duration, cancellationToken);
            }
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, GetDisplayText(), _audioPath ?? "res://assets/audio/ads/silent_ad.wav", _duration, new { Speaker = "AD" });
        }

        protected override async Task<float> GetAudioDurationAsync()
        {
            if (string.IsNullOrEmpty(_audioPath))
                return _duration;

            try
            {
                var audioStream = GD.Load<AudioStream>(_audioPath);
                if (audioStream is AudioStreamMP3 mp3Stream)
                {
                    return (float)mp3Stream.GetLength();
                }
                else if (audioStream is AudioStreamOggVorbis vorbisStream)
                {
                    return (float)vorbisStream.GetLength();
                }
                
                var length = audioStream?.GetLength() ?? 0.0;
                return length > 0 ? (float)length : _duration;
            }
            catch
            {
                return _duration;
            }
        }

        private string GetDisplayText()
        {
            if (!string.IsNullOrEmpty(_sponsor))
            {
                return $"This commercial break sponsored by {_sponsor}";
            }
            
            return _adText;
        }

        private float CalculateRevenue(int listenerCount)
        {
            // Simple revenue calculation based on listener count
            // This would use actual ad types and rates from the economy system
            var baseRate = 0.05f; // $0.05 per listener per ad
            return listenerCount * baseRate;
        }

        /// <summary>
        /// Create an ad executable based on listener count.
        /// </summary>
        public static AdExecutable CreateForListenerCount(string id, int listenerCount, int adIndex, EventBus eventBus, ListenerManager listenerManager, IBroadcastAudioService audioService, SceneTree sceneTree)
        {
            var adType = GetAdTypeForListenerCount(listenerCount);
            var sponsor = GetSponsorName(adType);
            var adText = $"Commercial Break {adIndex}";
            var audioPath = $"res://assets/audio/ads/{adType.ToString().ToLower()}_{adIndex}.mp3";
            
            GD.Print($"AdExecutable: Creating ad {adIndex} - Type: {adType}, Text: '{adText}', Audio: {audioPath}");
            
            return new AdExecutable(id, adText, 4.0f, eventBus, listenerManager, audioService, sceneTree, sponsor, audioPath);
        }

        private static AdType GetAdTypeForListenerCount(int listenerCount)
        {
            return listenerCount switch
            {
                < 50 => AdType.LocalBusiness,
                < 100 => AdType.RegionalBrand,
                < 200 => AdType.NationalSponsor,
                _ => AdType.PremiumSponsor
            };
        }

        private static string GetSponsorName(AdType adType)
        {
            return adType switch
            {
                AdType.LocalBusiness => "Local Business",
                AdType.RegionalBrand => "Regional Brand",
                AdType.NationalSponsor => "National Sponsor",
                AdType.PremiumSponsor => "Premium Sponsor",
                _ => "Unknown Sponsor"
            };
        }
    }

    /// <summary>
    /// Types of advertisements for revenue calculation.
    /// </summary>
    public enum AdType
    {
        LocalBusiness,
        RegionalBrand,
        NationalSponsor,
        PremiumSponsor
    }
}