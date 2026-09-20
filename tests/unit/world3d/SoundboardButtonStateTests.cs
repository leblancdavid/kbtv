using Chickensoft.GoDotTest;
using Godot;
using KBTV.World3D;

namespace KBTV.Tests.Unit.World3D
{
    public class SoundboardButtonStateTests : KBTVTestClass
    {
        public SoundboardButtonStateTests(Node testScene) : base(testScene) { }

        private static ButtonStateFacts Facts(
            bool adBreakActive = false, bool adQueued = false, bool queueEnabled = false,
            bool inBreakWindow = false, bool breakDue = false, bool musicBedPlaying = false,
            bool callerOnAir = false, bool curseActive = false) =>
            new(adBreakActive, adQueued, queueEnabled, inBreakWindow, breakDue, musicBedPlaying, callerOnAir, curseActive);

        [Test]
        public void AdsLook_PrioritisesActiveOverQueuedOverFlashingOverReady()
        {
            AssertThat(SoundboardButtonState.AdsLook(Facts(adBreakActive: true, adQueued: true)) == ButtonLampLook.Active);
            AssertThat(SoundboardButtonState.AdsLook(Facts(adQueued: true, inBreakWindow: true)) == ButtonLampLook.Queued);
            AssertThat(SoundboardButtonState.AdsLook(Facts(breakDue: true)) == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.AdsLook(Facts(queueEnabled: true, breakDue: true)) == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.AdsLook(Facts(queueEnabled: true, inBreakWindow: true)) == ButtonLampLook.Ready);
        }

        [Test]
        public void AdsLook_ReadyWhenWindowOpenEvenIfQueueFlagFalse()
        {
            AssertThat(SoundboardButtonState.AdsLook(Facts(inBreakWindow: true)) == ButtonLampLook.Ready);
        }

        [Test]
        public void AdsLook_OffWhenNoWindowAndNotQueued()
        {
            AssertThat(SoundboardButtonState.AdsLook(Facts()) == ButtonLampLook.Off);
        }

        [Test]
        public void DropLook_UrgentDuringCurseBeatsCallerReady()
        {
            AssertThat(SoundboardButtonState.DropLook(true, true) == ButtonLampLook.Urgent);
            AssertThat(SoundboardButtonState.DropLook(true, false) == ButtonLampLook.Ready);
            AssertThat(SoundboardButtonState.DropLook(false, false) == ButtonLampLook.Off);
        }

        [Test]
        public void DelayLook_FlashesOnlyWhileCurseActive()
        {
            AssertThat(SoundboardButtonState.DelayLook(true) == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.DelayLook(false) == ButtonLampLook.Off);
        }

        [Test]
        public void MusicLook_BedBeatsWindowFlashBeatsIdle()
        {
            AssertThat(SoundboardButtonState.MusicLook(true, true) == ButtonLampLook.Queued);
            AssertThat(SoundboardButtonState.MusicLook(true, false) == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.MusicLook(false, false) == ButtonLampLook.Idle);
        }

        [Test]
        public void Resolve_PressFlashOverridesEverything()
        {
            var ads = SoundboardButtonState.Resolve(
                SoundboardButton.Ads, Facts(adBreakActive: true, adQueued: true, queueEnabled: true,
                    inBreakWindow: true, breakDue: true, callerOnAir: true, curseActive: true),
                pressed: true);
            AssertThat(ads == ButtonLampLook.PressFlash);
        }

        [Test]
        public void Resolve_RoutesEachButtonToItsOwnRule()
        {
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Music, Facts(musicBedPlaying: true), false)
                == ButtonLampLook.Queued);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Delay, Facts(curseActive: true), false)
                == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Ads, Facts(inBreakWindow: true), false)
                == ButtonLampLook.Ready);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Ads, Facts(breakDue: true), false)
                == ButtonLampLook.Flashing);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Drop, Facts(callerOnAir: true), false)
                == ButtonLampLook.Ready);
        }
    }
}
