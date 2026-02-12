using System;
using System.Collections.Generic;
using KBTV.Callers;
using KBTV.Items;

namespace KBTV.Persistence
{
    [Serializable]
    public class IdentifiedEvidenceData
    {
        public string Word = "";
        public string SourceCallerName = "";
        public string EvidenceLevel = "";
        public int Tier;
        public int BonusType;
        public float BonusAmount;
        public int Status;
        public string? TargetTopic;
        public string? AnalysisId;
        public long AnalysisStartTimeTicks;
        public long? AnalysisCompleteTimeTicks;
    }

    [Serializable]
    public class EvidenceCabinetData
    {
        public int UpgradeLevel = 1;
    }

    [Serializable]
    public class EvidenceWebsiteData
    {
        public int UpgradeLevel = 1;
    }

    [Serializable]
    public class EvidenceSystemData
    {
        public List<IdentifiedEvidenceData> RawEvidence = new();
        public List<IdentifiedEvidenceData> ProcessingEvidence = new();
        public List<IdentifiedEvidenceData> IdentifiedEvidence = new();
        public EvidenceCabinetData Cabinet = new();
        public EvidenceWebsiteData Website = new();
    }
}
