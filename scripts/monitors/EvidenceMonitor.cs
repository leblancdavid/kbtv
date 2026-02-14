#nullable enable

using Godot;
using KBTV.Core;
using KBTV.Items;

namespace KBTV.Monitors
{
    /// <summary>
    /// Monitors evidence processing state and updates time-based properties.
    /// Handles evidence analysis progress and completion.
    ///
    /// State Updates:
    /// - Processing evidence: Advance analysis progress
    ///
    /// Side Effects:
    /// - Marks evidence as identified when analysis completes
    /// </summary>
    public partial class EvidenceMonitor : DomainMonitor
    {
        protected IEvidenceAnalyzer? _analyzer;

        protected IEvidenceAnalyzer EvidenceAnalyzer => DependencyInjection.Get<IEvidenceAnalyzer>(this);

        public override void OnResolved()
        {
            _analyzer = EvidenceAnalyzer;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _analyzer?.Update();
        }
    }
}