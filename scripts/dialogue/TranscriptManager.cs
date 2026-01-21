using System;
using Godot;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    public partial class TranscriptManager : Node
    {
        private ITranscriptRepository _transcriptRepository;

        public TranscriptManager(ITranscriptRepository transcriptRepository)
        {
            _transcriptRepository = transcriptRepository;
        }

        public void AddEntry(BroadcastLine line, Caller? caller = null)
        {
            if (line.Type == BroadcastLineType.None)
            {
                return;
            }

            if (line.Type == BroadcastLineType.Music)
            {
                _transcriptRepository?.AddEntry(TranscriptEntry.CreateMusicLine());
            }
            else if (line.Type == BroadcastLineType.VernDialogue ||
                line.Type == BroadcastLineType.DeadAirFiller ||
                line.Type == BroadcastLineType.BetweenCallers ||
                line.Type == BroadcastLineType.OffTopicRemark ||
                line.Type == BroadcastLineType.ShowOpening ||
                line.Type == BroadcastLineType.ShowClosing)
            {
                _transcriptRepository?.AddEntry(
                    TranscriptEntry.CreateVernLine(line.Text, line.Phase, line.ArcId)
                );
            }
            else if (line.Type == BroadcastLineType.CallerDialogue)
            {
                var speakerName = caller?.Name ?? "Caller";
                _transcriptRepository?.AddEntry(
                    new TranscriptEntry(Speaker.Caller, line.Text, line.Phase, line.ArcId, speakerName)
                );
            }
            else if (line.Type == BroadcastLineType.AdBreak || line.Type == BroadcastLineType.Ad)
            {
                _transcriptRepository?.AddEntry(
                    new TranscriptEntry(Speaker.System, line.Text, ConversationPhase.Intro, "system")
                );
            }
        }
    }
}