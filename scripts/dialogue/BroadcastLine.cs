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
        public string? CallerGender;
        public int LineIndex;

        public static BroadcastLine None() => new()
        {
            Type = BroadcastLineType.None,
            Text = string.Empty,
            Speaker = string.Empty,
            SpeakerId = string.Empty,
            Phase = ConversationPhase.Intro
        };

        public static BroadcastLine Music() => new()
        {
            Type = BroadcastLineType.Music,
            Text = "Bumper Music",
            Speaker = "Music",
            SpeakerId = "MUSIC",
            Phase = ConversationPhase.Intro
        };

        public static BroadcastLine ReturnMusic() => new()
        {
            Type = BroadcastLineType.Music,
            Text = "Return Bumper Music",
            Speaker = "Music",
            SpeakerId = "RETURN_MUSIC",
            Phase = ConversationPhase.Resolution
        };

        public static BroadcastLine OutroMusic() => new()
        {
            Type = BroadcastLineType.Music,
            Text = "Outro Bumper Music",
            Speaker = "Music",
            SpeakerId = "OUTRO_MUSIC",
            Phase = ConversationPhase.Resolution
        };

        public static BroadcastLine ShowOpening(string text, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.ShowOpening,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Intro,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine VernDialogue(string text, ConversationPhase phase, string? arcId = null, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.VernDialogue,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = phase,
            ArcId = arcId,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine CallerDialogue(string text, string speaker, string speakerId, ConversationPhase phase, string? arcId = null, string? callerGender = null, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.CallerDialogue,
            Text = text,
            Speaker = speaker,
            SpeakerId = speakerId,
            Phase = phase,
            ArcId = arcId,
            CallerGender = callerGender,
            LineIndex = lineIndex
        };

        public static BroadcastLine BetweenCallers(string text, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.BetweenCallers,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Resolution,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine OffTopicRemark(string text, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.OffTopicRemark,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Resolution,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine DeadAirFiller(string text, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.DeadAirFiller,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Intro,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine ShowClosing(string text, int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.ShowClosing,
            Text = text,
            Speaker = "Vern",
            SpeakerId = "VERN",
            Phase = ConversationPhase.Resolution,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine AdBreakStart(string customText = "AD BREAK", int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.AdBreak,
            Text = customText,
            Speaker = "System",
            SpeakerId = "system",
            Phase = ConversationPhase.Resolution,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };

        public static BroadcastLine Ad(string customText = "[Playing Advertisement]", int lineIndex = 0) => new()
        {
            Type = BroadcastLineType.Ad,
            Text = customText,
            Speaker = "System",
            SpeakerId = "system",
            Phase = ConversationPhase.Resolution,
            ArcId = null,
            CallerGender = null,
            LineIndex = lineIndex
        };
    }
}
