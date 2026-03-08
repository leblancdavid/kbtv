using System;
using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Ads;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Dialogue;
using KBTV.Audio;
using KBTV.Callers;

namespace KBTV.Tests.Unit.Ads
{
    public class AdManagerTests : KBTVTestClass
    {
        public AdManagerTests(Node testScene) : base(testScene) { }

        [Test]
        public void AdManager_ResetsState_BetweenShows()
        {
            // Arrange - Create a real AdManager with mocked dependencies
            var adManager = new AdManager();
            var schedule = new AdSchedule(2, 2);
            schedule.GenerateBreakSchedule(600f);
            
            // First show initialization
            adManager.Initialize(schedule, 600f);
            
            // Simulate some break activity
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            // Verify state after first break
            AssertThat(adManager.BreaksRemaining == 1);
            AssertThat(adManager.IsActive == true);
            
            // Act - Reset and reinitialize for second show
            adManager.Initialize(schedule, 600f);
            
            // Assert - State should be completely reset
            AssertThat(adManager.BreaksRemaining == 2);
            AssertThat(adManager.IsActive == true);
            AssertThat(adManager.IsAdBreakActive == false);
            AssertThat(adManager.IsInBreakWindow == false);
        }

        [Test]
        public void AdManager_TracksBreaksPlayed_Accurately()
        {
            // Arrange
            var adManager = new AdManager();
            var schedule = new AdSchedule(3, 2);
            schedule.GenerateBreakSchedule(600f);
            adManager.Initialize(schedule, 600f);
            
            // Act - Simulate 3 complete breaks
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            AssertThat(adManager.BreaksRemaining == 2);
            
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            AssertThat(adManager.BreaksRemaining == 1);
            
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            // Assert - All breaks complete
            AssertThat(adManager.BreaksRemaining == 0);
            AssertThat(adManager.IsLastSegment == true);
        }

        [Test]
        public void AdManager_Schedule_MatchesBreaksPerShow()
        {
            // Arrange & Act
            var schedule1 = new AdSchedule(1, 2);
            schedule1.GenerateBreakSchedule(600f);
            
            var schedule2 = new AdSchedule(5, 3);
            schedule2.GenerateBreakSchedule(600f);
            
            var schedule0 = new AdSchedule(0, 2);
            schedule0.GenerateBreakSchedule(600f);
            
            // Assert
            AssertThat(schedule1.Breaks.Count == 1);
            AssertThat(schedule2.Breaks.Count == 5);
            AssertThat(schedule0.Breaks.Count == 0);
        }

        [Test]
        public void AdManager_NoExtraBreak_AfterAllBreaksComplete()
        {
            // Arrange
            var adManager = new AdManager();
            var schedule = new AdSchedule(2, 2);
            schedule.GenerateBreakSchedule(600f);
            adManager.Initialize(schedule, 600f);
            
            bool showEndedFired = false;
            adManager.OnShowEnded += () => showEndedFired = true;
            
            // Act - Complete both breaks
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            AssertThat(adManager.IsActive == true);
            
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            // Assert - Show should have ended, no more breaks
            AssertThat(adManager.IsActive == false);
            AssertThat(adManager.IsLastSegment == true);
            AssertThat(showEndedFired == true);
            AssertThat(adManager.BreaksRemaining == 0);
        }

        [Test]
        public void AdManager_ScheduleBreakTimers_StopsAfterAllBreaks()
        {
            // Arrange
            var adManager = new AdManager();
            var schedule = new AdSchedule(2, 2);
            schedule.GenerateBreakSchedule(600f);
            adManager.Initialize(schedule, 600f);
            
            // Act - Complete first break
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            // Simulate that timers would be rescheduled by checking BreaksRemaining
            // The ScheduleBreakTimers method is called from EndAdBreak
            
            // Assert - Should still have one break remaining
            AssertThat(adManager.BreaksRemaining == 1);
            
            // Complete second break
            adManager.StartBreak();
            adManager.EndAdBreak();
            
            // Assert - No breaks remaining, no more scheduling
            AssertThat(adManager.BreaksRemaining == 0);
            AssertThat(adManager.IsActive == false);
        }
    }
}
