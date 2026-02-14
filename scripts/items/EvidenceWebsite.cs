using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Items;
using KBTV.Managers;

namespace KBTV.Items
{
    public interface IEvidenceWebsite
    {
        int Capacity { get; }
        int Used { get; }
        bool HasSpace { get; }
        float CalculatePassiveIncome();
        float CalculateListenerMultiplier();
        List<IdentifiedEvidence> GetAllEvidence();
        int GetUpgradeCost();
        void Upgrade(int level);
    }

    public partial class EvidenceWebsite : Node, IEvidenceWebsite
    {
        private IEvidenceAnalyzer? _analyzer;
        private IListenerManager? _listenerManager;
        private int _baseCapacity = 5;
        private int _upgradeLevel = 1;

        public int Capacity => _baseCapacity + (_upgradeLevel - 1) * 5;
        public int Used => _analyzer?.GetWebsiteCount() ?? 0;
        public bool HasSpace => Used < Capacity;

        public override void _Ready()
        {
        }

        public void Initialize()
        {
            if (ServiceRegistry.IsInitialized)
            {
                _analyzer = ServiceRegistry.Instance.EvidenceAnalyzer;
                _listenerManager = ServiceRegistry.Instance.ListenerManager;
                ServiceRegistry.Instance.RegisterSelf<IEvidenceWebsite>(this);
            }
        }

        public float CalculatePassiveIncome()
        {
            if (_analyzer == null)
                return 0f;

            int currentListeners = _listenerManager?.CurrentListeners ?? 1000;
            return _analyzer.CalculatePassiveIncome(currentListeners);
        }

        public float CalculateListenerMultiplier()
        {
            var websiteEvidence = _analyzer?.GetWebsiteEvidence() ?? new List<IdentifiedEvidence>();
            float totalBonus = 0f;

            foreach (var evidence in websiteEvidence)
            {
                if (evidence.BonusType == EvidenceBonusType.ListenerGrowth)
                {
                    totalBonus += evidence.BonusAmount;
                }
            }

            return 1f + totalBonus;
        }

        public List<IdentifiedEvidence> GetAllEvidence()
        {
            return _analyzer?.GetWebsiteEvidence() ?? new List<IdentifiedEvidence>();
        }

        public void Upgrade(int newLevel)
        {
            _upgradeLevel = newLevel;
        }

        public int GetUpgradeCost()
        {
            return _upgradeLevel * 400;
        }

        public string GetUpgradeDescription()
        {
            int currentCap = Capacity;
            int nextCap = _baseCapacity + _upgradeLevel * 5;
            return $"Upgrade Website (+{nextCap - currentCap} slots)";
        }
    }
}
