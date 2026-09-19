using Chickensoft.GoDotTest;
using Godot;
using KBTV.World3D;

namespace KBTV.Tests.Unit.World3D
{
    public class SoundboardButtonStateTests : KBTVTestClass
    {
        public SoundboardButtonStateTests(Node testScene) : base(testScene) { }

        [Test]
        public void AdsLook_PrioritisesActiveOverQueuedOverReady()
        {
            AssertThat(SoundboardButtonState.AdsLook(true, true, false, false) == ButtonLampLook.Active);
            AssertThat(SoundboardButtonState.AdsLook(false, true, false, true) == ButtonLampLook.Queued);
            AssertThat(SoundboardButtonState.AdsLook(false, false, true, true) == ButtonLampLook.Ready);
        }

        [Test]
        public void AdsLook_ReadyWhenWindowOpenEvenIfQueueFlagFalse()
        {
            AssertThat(SoundboardButtonState.AdsLook(false, false, false, true) == ButtonLampLook.Ready);
        }

        [Test]
        public void AdsLook_OffWhenNoWindowAndNotQueued()
        {
            AssertThat(SoundboardButtonState.AdsLook(false, false, false, false) == ButtonLampLook.Off);
        }

        [Test]
        public void DropLook_UrgentDuringCurseBeatsCallerReady()
        {
            AssertThat(SoundboardButtonState.DropLook(true, true) == ButtonLampLook.Urgent);
            AssertThat(SoundboardButtonState.DropLook(true, false) == ButtonLampLook.Ready);
            AssertThat(SoundboardButtonState.DropLook(false, false) == ButtonLampLook.Off);
        }

        [Test]
        public void DelayLook_PendingOnlyWhileCurseActive()
        {
            AssertThat(SoundboardButtonState.DelayLook(true) == ButtonLampLook.Pending);
            AssertThat(SoundboardButtonState.DelayLook(false) == ButtonLampLook.Off);
        }

        [Test]
        public void Resolve_PressFlashOverridesEverything()
        {
            var ads = SoundboardButtonState.Resolve(SoundboardButton.Ads, pressed: true,
                adBreakActive: true, queued: true, queueEnabled: true, inBreakWindow: true,
                callerOnAir: true, curseActive: true);
            AssertThat(ads == ButtonLampLook.PressFlash);
        }

        [Test]
        public void Resolve_RoutesEachButtonToItsOwnRule()
        {
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Music, false,
                false, false, false, false, false, false) == ButtonLampLook.Idle);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Delay, false,
                false, false, false, false, false, true) == ButtonLampLook.Pending);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Ads, false,
                false, false, false, true, false, false) == ButtonLampLook.Ready);
            AssertThat(SoundboardButtonState.Resolve(SoundboardButton.Drop, false,
                false, false, false, false, true, false) == ButtonLampLook.Ready);
        }
    }
}
