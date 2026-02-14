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

        public override void _Process(double delta)
        {
            // Override base class check - EvidenceMonitor uses _analyzer, not _repository
            if (_analyzer == null)
            {
                return;
            }

            OnUpdate((float)delta);
        }

        protected override void OnUpdate(float deltaTime)
        {
            _analyzer?.Update();
        }
    }
}