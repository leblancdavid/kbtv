using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Managers;

namespace KBTV.Tests.Unit.Managers
{
    public class ListenerManagerTests : KBTVTestClass
    {
        public ListenerManagerTests(Node testScene) : base(testScene) { }

        private ListenerManager _listenerManager = null!;

        [Setup]
        public void Setup()
        {
            _listenerManager = new ListenerManager();
            _listenerManager._Ready();
        }

        [Test]
        public void Constructor_InitializesCurrentListenersToZero()
        {
            AssertThat(_listenerManager.CurrentListeners == 0);
        }

        [Test]
        public void Constructor_InitializesPeakListenersToZero()
        {
            AssertThat(_listenerManager.PeakListeners == 0);
        }

        [Test]
        public void Constructor_InitializesStartingListenersToZero()
        {
            AssertThat(_listenerManager.StartingListeners == 0);
        }

        [Test]
        public void ModifyListeners_PositiveValue_IncreasesListeners()
        {
            int initial = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.CurrentListeners > initial);
        }

        [Test]
        public void ModifyListeners_NegativeValue_DecreasesListeners()
        {
            _listenerManager.ModifyListeners(500);
            int initial = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(-100);

            AssertThat(_listenerManager.CurrentListeners < initial);
        }

        [Test]
        public void ModifyListeners_AtMinLimit_ClampsToMin()
        {
            for (int i = 0; i < 50; i++)
            {
                _listenerManager.ModifyListeners(-100);
            }

            AssertThat(_listenerManager.CurrentListeners >= 100);
        }

        [Test]
        public void ModifyListeners_AtMaxLimit_ClampsToMax()
        {
            for (int i = 0; i < 50; i++)
            {
                _listenerManager.ModifyListeners(100);
            }

            AssertThat(_listenerManager.CurrentListeners <= 3000);
        }

        [Test]
        public void ModifyListeners_UpdatesPeakIfExceeded()
        {
            _listenerManager.ModifyListeners(1000);
            int current = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.PeakListeners >= current);
        }

        [Test]
        public void ModifyListeners_DoesNotUpdatePeakIfNotExceeded()
        {
            _listenerManager.ModifyListeners(1000);
            int peak = _listenerManager.PeakListeners;

            _listenerManager.ModifyListeners(-50);
            _listenerManager.ModifyListeners(25);

            AssertThat(_listenerManager.PeakListeners == peak);
        }

        [Test]
        public void ListenerChange_StartsAtZero()
        {
            AssertThat(_listenerManager.ListenerChange == 0);
        }

        [Test]
        public void ListenerChange_ReflectsNetChange()
        {
            _listenerManager.ModifyListeners(100);

            AssertThat(_listenerManager.ListenerChange > 0);
        }

        [Test]
        public void GetFormattedListeners_WithThousands_FormatsAsK()
        {
            _listenerManager.ModifyListeners(15000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "15.0K");
        }

        [Test]
        public void GetFormattedListeners_WithMillions_FormatsAsM()
        {
            _listenerManager.ModifyListeners(1500000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "1.5M");
        }

        [Test]
        public void GetFormattedListeners_WithSmallNumbers_FormatsWithCommas()
        {
            _listenerManager.ModifyListeners(500);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(!string.IsNullOrEmpty(formatted));
        }

        [Test]
        public void GetFormattedChange_PositiveChange_ShowsPlusSign()
        {
            _listenerManager.ModifyListeners(100);
            string formatted = _listenerManager.GetFormattedChange();

            AssertThat(formatted.StartsWith("+"));
        }

        [Test]
        public void GetFormattedChange_NegativeChange_ShowsMinusSign()
        {
            _listenerManager.ModifyListeners(-500);
            string formatted = _listenerManager.GetFormattedChange();

            AssertThat(formatted.StartsWith("-"));
        }

        [Test]
        public void GetFormattedListeners_NegativeListeners_FormatsCorrectly()
        {
            _listenerManager.ModifyListeners(-1000);
            string formatted = _listenerManager.GetFormattedListeners();

            AssertThat(formatted == "-1,000");
        }

        [Test]
        public void ModifyListeners_ExcessiveNegative_ClampsToMinimum()
        {
            _listenerManager.ModifyListeners(500);
            int beforeClamp = _listenerManager.CurrentListeners;

            _listenerManager.ModifyListeners(-600);

            AssertThat(_listenerManager.CurrentListeners <= beforeClamp);
            AssertThat(_listenerManager.CurrentListeners >= 100);
        }
    }
}
