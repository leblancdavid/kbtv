namespace KBTV.Dialogue
{
    public struct BroadcastLine
    {
        public BroadcastLineType Type;
        public string Text;
        public string Speaker;
        public string SpeakerId;
        public ConversationPhase Phase;
        public string? ArcId;

        public static BroadcastLine None() => new()
        {
            Type = BroadcastLineType.None,
            Text = string.Empty,
            Speaker = string.Empty,
            SpeakerId = string.Empty,
            Phase = ConversationPhase.Intro
        };

        public static BroadcastLine PutCallerOnAir(ConversationPhase phase = ConversationPhase.Intro) => new()
        {
            Type = BroadcastLineType.PutCallerOnAir,
            Text = string.Empty,
            Speaker = string.Empty,
            SpeakerId = string.Empty,
            Phase = phase
        };

        public static BroadcastLine ShowOpening(string text) => new()
        {
            Type = BroadcastLineType.ShowOpening,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Intro
        };

        public static BroadcastLine VernDialogue(string text, ConversationPhase phase, string? arcId = null) => new()
        {
            Type = BroadcastLineType.VernDialogue,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = phase,
            ArcId = arcId
        };

        public static BroadcastLine CallerDialogue(string text, string speaker, string speakerId, ConversationPhase phase, string? arcId = null) => new()
        {
            Type = BroadcastLineType.CallerDialogue,
            Text = text,
            Speaker = speaker,
            SpeakerId = speakerId,
            Phase = phase,
            ArcId = arcId
        };

        public static BroadcastLine BetweenCallers(string text) => new()
        {
            Type = BroadcastLineType.BetweenCallers,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Resolution
        };

        public static BroadcastLine DeadAirFiller(string text) => new()
        {
            Type = BroadcastLineType.DeadAirFiller,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Intro
        };

        public static BroadcastLine ShowClosing(string text) => new()
        {
            Type = BroadcastLineType.ShowClosing,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Resolution
        };
    }
}
