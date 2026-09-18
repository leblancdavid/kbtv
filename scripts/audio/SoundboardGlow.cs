using Godot;

namespace KBTV.Audio;

/// <summary>
/// Speaking-channel detection and glow intensity for the 3D soundboard hover halo.
/// All methods are pure / static, making them easy to unit-test without an
/// AudioServer.
/// </summary>
public static class SoundboardGlow
{
    /// <summary>
    /// The two channels the halo can represent at any given time, plus a neutral
    /// "none" state when nothing audible is detected.
    /// </summary>
    public enum SpeakingChannel
    {
        None,
        Caller,
        Vern
    }

    /// <summary>
    /// dB level treated as silence for speaking-channel detection.
    /// Anything below this counts as "off" regardless of the other channel.
    /// </summary>
    public const float SilenceFloorDb = -50f;

    /// <summary>
    /// dB hysteresis band: if the louder channel is less than this many dB
    /// above the quieter channel the decision is suppressed (no rapid toggle
    /// when two voices are nearly equal).
    /// </summary>
    public const float HysteresisDb = 3f;

    /// <summary>
    /// Peak dB level that maps to full-intensity glow (1.0).
    /// </summary>
    public const float LoudPeakDb = -8f;

    /// <summary>
    /// Fraction of the max halo size a ring keeps while its channel is silent.
    /// Keeps the glow readable around a part (0.75 → the idle ring stays significantly
    /// larger than a knob, so it's always visible and never hides completely under the control).
    /// </summary>
    public const float MinRingFraction = 0.75f;

    /// <summary>
    /// Decides which channel is "speaking" based on the two bus peak levels.
    /// Both silent → None. Equal-and-audible → None (deliberately: a tie means
    /// the player can't tell which channel is really driving the music).
    /// Otherwise the louder channel wins; ties broken by hysteresis band.
    /// </summary>
    public static SpeakingChannel ChooseSpeakingChannel(float callerPeakDb, float vernPeakDb)
    {
        if (callerPeakDb < SilenceFloorDb && vernPeakDb < SilenceFloorDb)
        {
            return SpeakingChannel.None;
        }

        if (callerPeakDb < SilenceFloorDb)
        {
            return SpeakingChannel.Vern;
        }

        if (vernPeakDb < SilenceFloorDb)
        {
            return SpeakingChannel.Caller;
        }

        float diff = callerPeakDb - vernPeakDb;

        if (diff > HysteresisDb)
        {
            return SpeakingChannel.Caller;
        }

        if (diff < -HysteresisDb)
        {
            return SpeakingChannel.Vern;
        }

        return SpeakingChannel.None;
    }

    /// <summary>
    /// Maps a live peak dB level to a 0..1 glow multiplier.
    /// -8 dB or louder → 1.0, -80 dB or lower → 0.0, linear between.
    /// </summary>
    public static float GlowFromPeakDb(float peakDb)
    {
        return Mathf.Clamp((peakDb - SilenceFloorDb) / (LoudPeakDb - SilenceFloorDb), 0f, 1f);
    }

    /// <summary>
    /// Maps a 0..1 channel glow to a halo size multiplier so loudness is shown
    /// as SIZE rather than colour. Silent (0) → MinRingFraction (small idle ring),
    /// full glow (1) → 1.0 (max halo size), linear between.
    /// </summary>
    public static float SizeScaleFromGlow(float glow)
    {
        return Mathf.Lerp(MinRingFraction, 1f, Mathf.Clamp(glow, 0f, 1f));
    }

    /// <summary>
    /// Maps a <see cref="SoundboardControl"/> to its owning speaking channel.
    /// Master/Ads/None → None (white halo); every caller knob/level → Caller;
    /// every Vern knob/level → Vern.
    /// </summary>
    public static SpeakingChannel ChannelOf(SoundboardControl control)
    {
        return control switch
        {
            SoundboardControl.CallerGain => SpeakingChannel.Caller,
            SoundboardControl.CallerLowPass => SpeakingChannel.Caller,
            SoundboardControl.CallerHighPass => SpeakingChannel.Caller,
            SoundboardControl.CallerLevel => SpeakingChannel.Caller,
            SoundboardControl.VernGain => SpeakingChannel.Vern,
            SoundboardControl.VernLevel => SpeakingChannel.Vern,
            _ => SpeakingChannel.None
        };
    }
}
