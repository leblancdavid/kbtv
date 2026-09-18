#nullable enable

using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public class TopStateOverlayModelsTests : KBTVTestClass
    {
        public TopStateOverlayModelsTests(Node testScene) : base(testScene) { }

        // ───────────── StatusFeedModel ─────────────

        [Test]
        public void Add_NewEntry_AppearsFirst()
        {
            var feed = new StatusFeedModel();
            feed.Add("first", StatusFeedKind.Info, 5f);
            feed.Add("second", StatusFeedKind.Curse, 90f);

            AssertThat(feed.Entries.Count == 2);
            AssertThat(feed.Entries[0].Message == "second");
            AssertThat(feed.Entries[1].Message == "first");
        }

        [Test]
        public void Add_OverCap_DropsOldest()
        {
            var feed = new StatusFeedModel();
            for (int i = 0; i < StatusFeedModel.MaxEntries + 2; i++)
            {
                feed.Add($"msg{i}", StatusFeedKind.Info, i);
            }

            AssertThat(feed.Entries.Count == StatusFeedModel.MaxEntries);
            AssertThat(feed.Entries[0].Message == $"msg{StatusFeedModel.MaxEntries + 1}");
            AssertThat(feed.Entries[StatusFeedModel.MaxEntries - 1].Message == $"msg{StatusFeedModel.MaxEntries - 1}");
        }

        [Test]
        public void Add_KeepsKindAndTimestamp()
        {
            var feed = new StatusFeedModel();
            feed.Add("penalty", StatusFeedKind.Warning, 753f);

            AssertThat(feed.Entries[0].Kind == StatusFeedKind.Warning);
            AssertThat(feed.Entries[0].FormattedTime == "12:33");
            AssertThat(feed.Entries[0].ElapsedSeconds == 753f);
        }

        [Test]
        public void EntriesWithinWindow_FiltersOlderThanWindow()
        {
            var feed = new StatusFeedModel();
            feed.Add("old", StatusFeedKind.Info, 0f);
            feed.Add("mid", StatusFeedKind.Info, 30f);
            feed.Add("new", StatusFeedKind.Curse, 70f);

            var recent = feed.EntriesWithinWindow(now: 70f, windowSeconds: 60f);

            AssertThat(recent.Count == 2);
            AssertThat(recent[0].Message == "new");
            AssertThat(recent[1].Message == "mid");
        }

        [Test]
        public void EntriesWithinWindow_Boundary_Inclusive()
        {
            var feed = new StatusFeedModel();
            feed.Add("edge", StatusFeedKind.Info, 10f);

            var recent = feed.EntriesWithinWindow(now: 70f, windowSeconds: 60f);

            AssertThat(recent.Count == 1);
            AssertThat(recent[0].Message == "edge");
        }

        [Test]
        public void EntriesWithinWindow_AllExpired_Empty()
        {
            var feed = new StatusFeedModel();
            feed.Add("a", StatusFeedKind.Info, 0f);
            feed.Add("b", StatusFeedKind.Info, 5f);

            AssertThat(feed.EntriesWithinWindow(now: 100f, windowSeconds: 60f).Count == 0);
        }

        [Test]
        public void EntriesWithinWindow_EmptyFeed_Empty()
        {
            var feed = new StatusFeedModel();
            AssertThat(feed.EntriesWithinWindow(now: 42f, windowSeconds: 60f).Count == 0);
        }

        [Test]
        public void Clear_RemovesAllEntries_ButAllowsReuse()
        {
            var feed = new StatusFeedModel();
            feed.Add("a", StatusFeedKind.Info, 0f);
            feed.Add("b", StatusFeedKind.Info, 1f);
            feed.Clear();

            AssertThat(feed.Entries.Count == 0);

            feed.Add("c", StatusFeedKind.Info, 2f);
            AssertThat(feed.Entries.Count == 1);
            AssertThat(feed.Entries[0].Message == "c");
        }

        [Test]
        public void FormatTime_MinutesAndSeconds_Padded()
        {
            AssertThat(StatusFeedModel.FormatTime(0f) == "00:00");
            AssertThat(StatusFeedModel.FormatTime(59f) == "00:59");
            AssertThat(StatusFeedModel.FormatTime(60f) == "01:00");
            AssertThat(StatusFeedModel.FormatTime(125f) == "02:05");
            AssertThat(StatusFeedModel.FormatTime(3600f) == "60:00");
        }

        [Test]
        public void FormatTime_NonWholeSeconds_Truncates()
        {
            AssertThat(StatusFeedModel.FormatTime(89.9f) == "01:29");
        }

        // ───────────── ListenerTrendTracker ─────────────

        [Test]
        public void Update_EmptyHistory_IsFlat()
        {
            var tracker = new ListenerTrendTracker();
            int trend = tracker.Update(1.0f, 1000);

            AssertThat(trend == ListenerTrendTracker.Flat);
        }

        [Test]
        public void Update_OnlyRecentHalfFilled_IsFlat()
        {
            var tracker = new ListenerTrendTracker();
            tracker.Update(0.1f, 1000);
            tracker.Update(0.6f, 1000);

            int trend = tracker.Update(1.1f, 1000);

            AssertThat(trend == ListenerTrendTracker.Flat);
        }

        [Test]
        public void Update_RisingCount_IsUp()
        {
            var tracker = new ListenerTrendTracker();
            tracker.Update(0.5f, 500);
            tracker.Update(1.0f, 500);
            tracker.Update(2.0f, 500);
            tracker.Update(3.5f, 900);
            tracker.Update(4.0f, 900);
            tracker.Update(4.5f, 900);

            // Recent window (3.0-6.0) averages 900 vs older window (0.0-3.0) averaging 500.
            int trend = tracker.Update(6.0f, 900);

            AssertThat(trend == ListenerTrendTracker.Up);
        }

        [Test]
        public void Update_FallingCount_IsDown()
        {
            var tracker = new ListenerTrendTracker();
            tracker.Update(0.5f, 1000);
            tracker.Update(1.0f, 1000);
            tracker.Update(2.0f, 1000);
            tracker.Update(3.5f, 200);
            tracker.Update(4.0f, 200);
            tracker.Update(4.5f, 200);

            int trend = tracker.Update(6.0f, 200);

            AssertThat(trend == ListenerTrendTracker.Down);
        }

        [Test]
        public void Update_WithinDeadzone_IsFlat()
        {
            var tracker = new ListenerTrendTracker();
            tracker.Update(0.5f, 1000);
            tracker.Update(1.0f, 1000);
            tracker.Update(2.0f, 1000);
            tracker.Update(3.5f, 1005);
            tracker.Update(4.0f, 1005);

            int trend = tracker.Update(6.0f, 1005);

            AssertThat(trend == ListenerTrendTracker.Flat);
        }

[Test]
        public void Update_StaleSamplesOutsideWindow_DoNotSkewTrend()
        {
            var tracker = new ListenerTrendTracker();
            tracker.Update(0.0f, 1000);
            tracker.Update(0.5f, 1000);
            tracker.Update(1.0f, 1000);

            // Fill both window halves with identical counts well past the old samples.
            tracker.Update(30.0f, 1000);
            tracker.Update(30.5f, 1000);
            tracker.Update(32.0f, 1000);
            tracker.Update(33.5f, 1000);
            tracker.Update(34.0f, 1000);
            tracker.Update(35.0f, 1000);

            // Old 0.x samples fall outside the 6s window; both halves average 1000 -> flat.
            int trend = tracker.Update(36.0f, 1000);

            AssertThat(trend == ListenerTrendTracker.Flat);
        }
    }
}