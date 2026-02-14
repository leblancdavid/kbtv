using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Items
{
    public enum EvidenceStatus
    {
        Raw,
        Processing,
        Identified,
        InCabinet,
        OnWebsite,
        Sold
    }

    public class IdentifiedEvidence : EvidenceItem
    {
        private EvidenceBonusType _bonusType;
        private float _bonusAmount;
        private EvidenceStatus _status;
        private string? _targetTopic;
        private double _analysisProgress;
        private DateTime _analysisStartTime;
        private DateTime? _analysisCompleteTime;
        private string? _analysisId;

        public EvidenceBonusType BonusType => _bonusType;
        public float BonusAmount => _bonusAmount;
        public EvidenceStatus Status => _status;
        public string? TargetTopic => _targetTopic;
        public double AnalysisProgress => _analysisProgress;
        public DateTime AnalysisStartTime => _analysisStartTime;
        public DateTime? AnalysisCompleteTime => _analysisCompleteTime;
        public string? AnalysisId => _analysisId;

        public string BonusDisplayName => EvidenceBonusConfig.GetBonusDisplayName(_bonusType);
        public string BonusDescription => EvidenceBonusConfig.GetBonusDescription(_bonusType, _bonusAmount);
        public int SellPrice => EvidenceBonusConfig.GetSellPrice(Tier);
        public float AnalysisTimeSeconds => EvidenceBonusConfig.GetAnalysisTimeSeconds(Tier);

        private IdentifiedEvidence() { }

        public static IdentifiedEvidence CreateRaw(EvidenceItem item)
        {
            return new IdentifiedEvidence
            {
                Word = EvidenceBonusConfig.GetRandomItemName(),
                SourceCallerName = item.SourceCallerName,
                EvidenceLevel = item.EvidenceLevel,
                Tier = item.Tier,
                _status = EvidenceStatus.Raw,
                _bonusType = EvidenceBonusConfig.GetRandomBonusType(),
                _bonusAmount = EvidenceBonusConfig.GetBonusAmount(item.Tier, EvidenceBonusConfig.GetRandomBonusType())
            };
        }

        public static IdentifiedEvidence CreateIdentified(
            EvidenceItem item,
            EvidenceBonusType bonusType,
            float bonusAmount,
            string? targetTopic = null)
        {
            return new IdentifiedEvidence
            {
                Word = EvidenceBonusConfig.GetRandomItemName(),
                SourceCallerName = item.SourceCallerName,
                EvidenceLevel = item.EvidenceLevel,
                Tier = item.Tier,
                _status = EvidenceStatus.Identified,
                _bonusType = bonusType,
                _bonusAmount = bonusAmount,
                _targetTopic = targetTopic,
                _analysisCompleteTime = DateTime.UtcNow
            };
        }

        public void StartAnalysis(string analysisId)
        {
            _status = EvidenceStatus.Processing;
            _analysisId = analysisId;
            _analysisStartTime = DateTime.UtcNow;
            _analysisProgress = 0f;
        }

        public void UpdateAnalysisProgress(double elapsedSeconds)
        {
            if (_status != EvidenceStatus.Processing)
                return;

            float totalTime = AnalysisTimeSeconds;
            _analysisProgress = Math.Min(1.0, elapsedSeconds / totalTime);

            if (_analysisProgress >= 1.0)
            {
                _status = EvidenceStatus.Identified;
                _analysisCompleteTime = DateTime.UtcNow;
            }
        }

        public bool IsAnalysisComplete => _status == EvidenceStatus.Identified || _status == EvidenceStatus.InCabinet || _status == EvidenceStatus.OnWebsite || _status == EvidenceStatus.Sold;

        public void MoveToCabinet()
        {
            GD.Print($"IdentifiedEvidence.MoveToCabinet: Current status: {_status}, Target: {EvidenceStatus.Identified}");
            if (_status == EvidenceStatus.Identified)
            {
                _status = EvidenceStatus.InCabinet;
                GD.Print($"IdentifiedEvidence.MoveToCabinet: Status changed to {_status}");
            }
            else
            {
                GD.Print($"IdentifiedEvidence.MoveToCabinet: Cannot move - status is {_status}");
            }
        }

        public void MoveToWebsite()
        {
            GD.Print($"IdentifiedEvidence.MoveToWebsite: Current status: {_status}, Target: {EvidenceStatus.Identified}");
            if (_status == EvidenceStatus.Identified)
            {
                _status = EvidenceStatus.OnWebsite;
                GD.Print($"IdentifiedEvidence.MoveToWebsite: Status changed to {_status}");
            }
            else
            {
                GD.Print($"IdentifiedEvidence.MoveToWebsite: Cannot move - status is {_status}");
            }
        }

        public void MarkAsSold()
        {
            _status = EvidenceStatus.Sold;
        }

        public override string ToString()
        {
            return $"{Word} ({Tier}) - {BonusDisplayName}{BonusAmount:F1} [{_status}]";
        }
    }
}
