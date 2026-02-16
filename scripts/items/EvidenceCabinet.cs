using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Data;
using KBTV.Items;

namespace KBTV.Items
{
    public interface IEvidenceCabinet
    {
        int Capacity { get; }
        int Used { get; }
        bool HasSpace { get; }
        float GetTotalBonus(EvidenceBonusType type);
        float GetPhysicalBonus();
        float GetEmotionalBonus();
        float GetMentalBonus();
        float GetListenerGrowthBonus();
        float GetShowQualityBonus();
        float GetTopicXPBonus();
        float GetScreeningInfoBonus();
        List<IdentifiedEvidence> GetAllEvidence();
        int GetUpgradeCost();
        void Upgrade(int level);
    }

    public partial class EvidenceCabinet : Node, IEvidenceCabinet
    {
        private IEvidenceAnalyzer? _analyzer;
        private int _baseCapacity = 5;
        private int _upgradeLevel = 1;

        public int Capacity => _baseCapacity + (_upgradeLevel - 1) * 5;
        public int Used => _analyzer?.GetCabinetCount() ?? 0;
        public bool HasSpace => Used < Capacity;

        public override void _Ready()
        {
        }

        public void Initialize()
        {
            if (ServiceRegistry.IsInitialized)
            {
                _analyzer = ServiceRegistry.Instance.EvidenceAnalyzer;
                ServiceRegistry.Instance.RegisterSelf<IEvidenceCabinet>(this);
            }
        }

        public float GetTotalBonus(EvidenceBonusType type)
        {
            float bonus = _analyzer?.GetTotalCabinetBonus(type) ?? 0f;
            GD.Print($"EvidenceCabinet.GetTotalBonus({type}): analyzer={_analyzer != null}, bonus={bonus}");
            return bonus;
        }

        public float GetPhysicalBonus()
        {
            return GetTotalBonus(EvidenceBonusType.VernPhysical);
        }

        public float GetEmotionalBonus()
        {
            return GetTotalBonus(EvidenceBonusType.VernEmotional);
        }

        public float GetMentalBonus()
        {
            return GetTotalBonus(EvidenceBonusType.VernMental);
        }

        public float GetListenerGrowthBonus()
        {
            return GetTotalBonus(EvidenceBonusType.ListenerGrowth);
        }

        public float GetShowQualityBonus()
        {
            return GetTotalBonus(EvidenceBonusType.ShowQuality);
        }

        public float GetTopicXPBonus()
        {
            return GetTotalBonus(EvidenceBonusType.TopicXP);
        }

        public float GetScreeningInfoBonus()
        {
            return GetTotalBonus(EvidenceBonusType.ScreeningInfo);
        }

        public List<IdentifiedEvidence> GetAllEvidence()
        {
            return _analyzer?.GetCabinetEvidence() ?? new List<IdentifiedEvidence>();
        }

        public void Upgrade(int newLevel)
        {
            _upgradeLevel = newLevel;
        }

        public int GetUpgradeCost()
        {
            return _upgradeLevel * 300;
        }

        public string GetUpgradeDescription()
        {
            int currentCap = Capacity;
            int nextCap = _baseCapacity + _upgradeLevel * 5;
            return $"Upgrade Cabinet (+{nextCap - currentCap} slots)";
        }
    }
}
