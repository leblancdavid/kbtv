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
        void MoveToCabinet(IdentifiedEvidence evidence);
        void MoveToWebsite(IdentifiedEvidence evidence);
        bool SellEvidence(IdentifiedEvidence evidence);
        bool IsProcessingEvidence { get; }
        void StartProcessingSpecificEvidence(IdentifiedEvidence evidence);
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
        public bool IsProcessingEvidence => GetProcessingCount() > 0;

        public override void _Ready()
        {
            GD.Print("EvidenceAnalyzer._Ready: Adding test evidence in _Ready");
            AddTestEvidence();
            GD.Print($"EvidenceAnalyzer._Ready: Completed. Total evidence: {_evidence.Count}, Identified: {GetIdentifiedCount()}");
        }

        public void Initialize()
        {
            GD.Print("EvidenceAnalyzer.Initialize: Starting initialization");
            if (ServiceRegistry.IsInitialized)
            {
                ServiceRegistry.Instance.RegisterSelf<IEvidenceAnalyzer>(this);
            }

            var saveManager = ServiceRegistry.Instance?.Get<SaveManager>();
            GD.Print($"EvidenceAnalyzer.Initialize: saveManager={saveManager?.CurrentSave != null}");
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

            // Always add test identified evidence for development testing
            GD.Print("EvidenceAnalyzer.Initialize: Adding test evidence");
            GD.Print($"EvidenceAnalyzer.Initialize: Completed. Total evidence: {_evidence.Count}, Identified: {GetIdentifiedCount()}");
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

        public void StartProcessingSpecificEvidence(IdentifiedEvidence evidence)
        {
            if (evidence.Status != EvidenceStatus.Raw || IsProcessingEvidence)
                return;

            StartAnalysis(evidence);
        }

        public void MoveToCabinet(IdentifiedEvidence evidence)
        {
            GD.Print($"MoveToCabinet status check - evidence.Status: {evidence.Status}, expected: {EvidenceStatus.Identified}");
            if (evidence.Status != EvidenceStatus.Identified)
                return;

            var cabinet = ServiceRegistry.Instance?.EvidenceCabinet;
            GD.Print($"MoveToCabinet called - Evidence: {evidence.Word}, Status: {evidence.Status}, CabinetUsed: {CabinetUsed}, Capacity: {cabinet?.Capacity}");
            if (cabinet != null && CabinetUsed >= cabinet.Capacity)
            {
                Log.Warning($"Cannot move to cabinet: Cabinet full ({CabinetUsed}/{cabinet.Capacity})");
                return;
            }

            evidence.MoveToCabinet();
            GD.Print($"MoveToCabinet completed - Evidence status: {evidence.Status}");
        }

        public void MoveToWebsite(IdentifiedEvidence evidence)
        {
            GD.Print($"MoveToWebsite status check - evidence.Status: {evidence.Status}, expected: {EvidenceStatus.Identified}");
            if (evidence.Status != EvidenceStatus.Identified)
                return;

            var website = ServiceRegistry.Instance?.EvidenceWebsite;
            GD.Print($"MoveToWebsite called - Evidence: {evidence.Word}, Status: {evidence.Status}, WebsiteUsed: {WebsiteUsed}, Capacity: {website?.Capacity}");
            if (website != null && WebsiteUsed >= website.Capacity)
            {
                Log.Warning($"Cannot move to website: Website full ({WebsiteUsed}/{website.Capacity})");
                return;
            }

            evidence.MoveToWebsite();
            GD.Print($"MoveToWebsite completed - Evidence status: {evidence.Status}");
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
            var cabinet = ServiceRegistry.Instance?.EvidenceCabinet;
            cabinet?.Upgrade(cabinet.Capacity / 5 + 1);
        }

        public void UpgradeWebsite(int additionalSlots)
        {
            var website = ServiceRegistry.Instance?.EvidenceWebsite;
            website?.Upgrade(website.Capacity / 5 + 1);
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

        private void AddTestEvidence()
        {
            GD.Print("AddTestEvidence: Adding 5 test identified evidence items");
            // Add test identified evidence of various tiers for immediate testing of File/Post/Sell buttons
            var commonItem = EvidenceItem.Create("Test Evidence Common", "Test Caller 1", "Low", EvidenceTier.Common);
            var uncommonItem = EvidenceItem.Create("Test Evidence Uncommon", "Test Caller 2", "Medium", EvidenceTier.Uncommon);
            var rareItem = EvidenceItem.Create("Test Evidence Rare", "Test Caller 3", "High", EvidenceTier.Rare);
            var veryRareItem = EvidenceItem.Create("Test Evidence VeryRare", "Test Caller 4", "Irrefutable", EvidenceTier.VeryRare);
            var oneOfAKindItem = EvidenceItem.Create("Test Evidence OneOfAKind", "Test Caller 5", "Irrefutable", EvidenceTier.OneOfAKind);

            var commonEvidence = IdentifiedEvidence.CreateIdentified(commonItem, EvidenceBonusType.VernPhysical, 10f);
            var uncommonEvidence = IdentifiedEvidence.CreateIdentified(uncommonItem, EvidenceBonusType.VernEmotional, 15f);
            var rareEvidence = IdentifiedEvidence.CreateIdentified(rareItem, EvidenceBonusType.ListenerGrowth, 20f);
            var veryRareEvidence = IdentifiedEvidence.CreateIdentified(veryRareItem, EvidenceBonusType.ShowQuality, 25f);
            var oneOfAKindEvidence = IdentifiedEvidence.CreateIdentified(oneOfAKindItem, EvidenceBonusType.TopicXP, 30f);

            _evidence.Add(commonEvidence);
            _evidence.Add(uncommonEvidence);
            _evidence.Add(rareEvidence);
            _evidence.Add(veryRareEvidence);
            _evidence.Add(oneOfAKindEvidence);
            GD.Print($"AddTestEvidence: Added evidence. Total evidence count: {_evidence.Count}, Identified count: {GetIdentifiedCount()}");
        }
    }
}
