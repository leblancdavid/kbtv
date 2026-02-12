using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Persistence;

namespace KBTV.Items
{
    public interface IEvidenceAnalyzer
    {
        List<IdentifiedEvidence> GetRawEvidence();
        List<IdentifiedEvidence> GetProcessingEvidence();
        List<IdentifiedEvidence> GetIdentifiedEvidence();
        List<IdentifiedEvidence> GetCabinetEvidence();
        List<IdentifiedEvidence> GetWebsiteEvidence();
        void StartAnalysis(IdentifiedEvidence evidence);
        void StartAnalysisAll();
        void MoveToCabinet(IdentifiedEvidence evidence);
        void MoveToWebsite(IdentifiedEvidence evidence);
        bool SellEvidence(IdentifiedEvidence evidence);
        float GetTotalCabinetBonus(EvidenceBonusType type);
        float CalculatePassiveIncome(int currentListeners);
        void Update();
        int GetRawCount();
        int GetProcessingCount();
        int GetIdentifiedCount();
        int GetCabinetCount();
        int GetWebsiteCount();
        int GetRawCountByTier(EvidenceTier tier);
        void AddEvidence(EvidenceItem item);
        void PopulateFromSaveData(List<EvidenceItem> collectedEvidence);
    }

    public partial class EvidenceAnalyzer : Node, IEvidenceAnalyzer
    {
        private List<IdentifiedEvidence> _evidence = new();
        private Dictionary<string, IdentifiedEvidence> _processingEvidence = new();
        private int _cabinetSlots = 5;
        private int _websiteSlots = 5;

        public int CabinetSlots => _cabinetSlots;
        public int WebsiteSlots => _websiteSlots;
        public int CabinetUsed => _evidence.Count(e => e.Status == EvidenceStatus.InCabinet);
        public int WebsiteUsed => _evidence.Count(e => e.Status == EvidenceStatus.OnWebsite);

        public override void _Ready()
        {
        }

        public void Initialize()
        {
            if (ServiceRegistry.IsInitialized)
            {
                ServiceRegistry.Instance.RegisterSelf<IEvidenceAnalyzer>(this);
            }

            var saveManager = ServiceRegistry.Instance?.Get<SaveManager>();
            if (saveManager?.CurrentSave != null)
            {
                var evidenceSystem = saveManager.CurrentSave.EvidenceSystem;
                if (evidenceSystem?.RawEvidence != null && evidenceSystem.RawEvidence.Count > 0)
                {
                    foreach (var data in evidenceSystem.RawEvidence)
                    {
                        var item = EvidenceItem.Create(data.Word, data.SourceCallerName, data.EvidenceLevel, (EvidenceTier)data.Tier);
                        AddEvidence(item);
                    }
                }
                else if (saveManager.CurrentSave.CollectedEvidence != null && saveManager.CurrentSave.CollectedEvidence.Count > 0)
                {
                    PopulateFromSaveData(saveManager.CurrentSave.CollectedEvidence);
                }
            }
        }

        public void AddEvidence(EvidenceItem item)
        {
            var identified = IdentifiedEvidence.CreateRaw(item);
            _evidence.Add(identified);
        }

        public void AddEvidenceRange(List<EvidenceItem> items)
        {
            foreach (var item in items)
            {
                AddEvidence(item);
            }
        }

        public List<IdentifiedEvidence> GetRawEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.Raw).ToList();
        }

        public List<IdentifiedEvidence> GetProcessingEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.Processing).ToList();
        }

        public List<IdentifiedEvidence> GetIdentifiedEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.Identified).ToList();
        }

        public List<IdentifiedEvidence> GetCabinetEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.InCabinet).ToList();
        }

        public List<IdentifiedEvidence> GetWebsiteEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.OnWebsite).ToList();
        }

        public List<IdentifiedEvidence> GetSoldEvidence()
        {
            return _evidence.Where(e => e.Status == EvidenceStatus.Sold).ToList();
        }

        public void StartAnalysis(IdentifiedEvidence evidence)
        {
            if (evidence.Status != EvidenceStatus.Raw)
                return;

            var analysisId = Guid.NewGuid().ToString();
            evidence.StartAnalysis(analysisId);
            _processingEvidence[analysisId] = evidence;
        }

        public void StartAnalysisAll()
        {
            var rawEvidence = GetRawEvidence();
            foreach (var evidence in rawEvidence)
            {
                StartAnalysis(evidence);
            }
        }

        public void MoveToCabinet(IdentifiedEvidence evidence)
        {
            if (evidence.Status != EvidenceStatus.Identified)
                return;

            if (CabinetUsed >= _cabinetSlots)
            {
                Log.Warning($"Cannot move to cabinet: Cabinet full ({CabinetUsed}/{_cabinetSlots})");
                return;
            }

            evidence.MoveToCabinet();
        }

        public void MoveToWebsite(IdentifiedEvidence evidence)
        {
            if (evidence.Status != EvidenceStatus.Identified)
                return;

            if (WebsiteUsed >= _websiteSlots)
            {
                Log.Warning($"Cannot move to website: Website full ({WebsiteUsed}/{_websiteSlots})");
                return;
            }

            evidence.MoveToWebsite();
        }

        public bool SellEvidence(IdentifiedEvidence evidence)
        {
            if (evidence.Status != EvidenceStatus.Identified)
                return false;

            var economyManager = ServiceRegistry.Instance?.EconomyManager;
            if (economyManager != null)
            {
                economyManager.AddMoney(evidence.SellPrice, $"Sold evidence: {evidence.Word}");
            }

            evidence.MarkAsSold();
            return true;
        }

        public float GetTotalCabinetBonus(EvidenceBonusType type)
        {
            return GetCabinetEvidence()
                .Where(e => e.BonusType == type)
                .Sum(e => e.BonusAmount);
        }

        public float CalculatePassiveIncome(int currentListeners)
        {
            var websiteEvidence = GetWebsiteEvidence();
            float totalIncome = 0f;

            foreach (var evidence in websiteEvidence)
            {
                if (evidence.BonusType == EvidenceBonusType.IncomePerShow)
                {
                    float baseIncome = evidence.BonusAmount;
                    float listenerMultiplier = Math.Max(0.1f, currentListeners / 1000f);
                    totalIncome += baseIncome * listenerMultiplier;
                }
            }

            return totalIncome;
        }

        public void UpgradeCabinet(int additionalSlots)
        {
            _cabinetSlots += additionalSlots;
        }

        public void UpgradeWebsite(int additionalSlots)
        {
            _websiteSlots += additionalSlots;
        }

        public void Update()
        {
            var now = DateTime.UtcNow;
            var completedIds = new List<string>();

            foreach (var kvp in _processingEvidence)
            {
                var evidence = kvp.Value;
                var elapsed = (now - evidence.AnalysisStartTime).TotalSeconds;
                evidence.UpdateAnalysisProgress(elapsed);

                if (evidence.IsAnalysisComplete)
                {
                    completedIds.Add(kvp.Key);
                }
            }

            foreach (var id in completedIds)
            {
                _processingEvidence.Remove(id);
            }
        }

        public void ClearSoldEvidence()
        {
            _evidence.RemoveAll(e => e.Status == EvidenceStatus.Sold);
        }

        public int GetRawCount() => _evidence.Count(e => e.Status == EvidenceStatus.Raw);
        public int GetProcessingCount() => _evidence.Count(e => e.Status == EvidenceStatus.Processing);
        public int GetIdentifiedCount() => _evidence.Count(e => e.Status == EvidenceStatus.Identified);
        public int GetCabinetCount() => _evidence.Count(e => e.Status == EvidenceStatus.InCabinet);
        public int GetWebsiteCount() => _evidence.Count(e => e.Status == EvidenceStatus.OnWebsite);

        public int GetRawCountByTier(EvidenceTier tier)
        {
            return _evidence.Count(e => e.Status == EvidenceStatus.Raw && e.Tier == tier);
        }

        public void PopulateFromSaveData(List<EvidenceItem> collectedEvidence)
        {
            _evidence.Clear();
            foreach (var item in collectedEvidence)
            {
                var identified = IdentifiedEvidence.CreateRaw(item);
                _evidence.Add(identified);
            }
        }
    }
}
