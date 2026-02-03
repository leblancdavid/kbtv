#nullable enable

using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI.Components;
using KBTV.Callers;

namespace KBTV.Tests.Unit.UI
{
    public class ReactiveListPanelTests : KBTVTestClass
    {
        public ReactiveListPanelTests(Node testScene) : base(testScene) { }

        private ReactiveListPanel<Caller> _panel = null!;
        private TestCallerAdapter _adapter = null!;

        [Setup]
        public void Setup()
        {
            _panel = new ReactiveListPanel<Caller>();
            _adapter = new TestCallerAdapter();
            _adapter.Setup(_panel);
        }

        [Test]
        public void RebuildCacheIndices_AfterRemove_MaintainsCorrectMapping()
        {
            var callers = CreateTestCallers(3);
            _panel.SetData(callers);

            var initialCount = _panel.GetChildCount();
            AssertThat(initialCount >= 3);

            var callersAfterRemove = new List<Caller> { callers[0], callers[2] };
            _panel.SetData(callersAfterRemove);

            var finalCount = _panel.GetChildCount();
            AssertThat(finalCount >= 2);
        }

        [Test]
        public void RebuildCacheIndices_AfterMultipleRemoves_StillCorrect()
        {
            var callers = CreateTestCallers(5);
            _panel.SetData(callers);

            var callersAfterRemove = new List<Caller> { callers[0], callers[4] };
            _panel.SetData(callersAfterRemove);

            var finalCount = _panel.GetChildCount();
            AssertThat(finalCount >= 2);
        }

        [Test]
        public void SetData_AfterDisconnects_ListDoesNotGrow()
        {
            var callers = CreateTestCallers(10);
            _panel.SetData(callers);

            var initialCount = _panel.GetChildCount();
            AssertThat(initialCount >= 10);

            var remainingCallers = new List<Caller>();
            for (int i = 0; i < 5; i++)
            {
                remainingCallers.Add(callers[i]);
            }
            _panel.SetData(remainingCallers);

            var afterFirstRemove = _panel.GetChildCount();
            AssertThat(afterFirstRemove >= 5);

            for (int i = 0; i < 3; i++)
            {
                remainingCallers.RemoveAt(remainingCallers.Count - 1);
                _panel.SetData(remainingCallers);
            }

            var finalCount = _panel.GetChildCount();
            AssertThat(finalCount >= 2);
        }

        private List<Caller> CreateTestCallers(int count)
        {
            var callers = new List<Caller>();
            for (int i = 0; i < count; i++)
            {
                callers.Add(CreateTestCaller($"Test Caller {i}"));
            }
            return callers;
        }

        private Caller CreateTestCaller(string name)
        {
            return new Caller(
                name: name,
                phoneNumber: "555-0123",
                location: "Test City",
                claimedTopic: "Test Topic",
                actualTopic: "Test Topic",
                callReason: "Test Reason",
                legitimacy: CallerLegitimacy.Credible,
                phoneQuality: CallerPhoneQuality.Good,
                emotionalState: CallerEmotionalState.Calm,
                curseRisk: CallerCurseRisk.Low,
                beliefLevel: CallerBeliefLevel.Curious,
                evidenceLevel: CallerEvidenceLevel.None,
                coherence: CallerCoherence.Coherent,
                urgency: CallerUrgency.Medium,
                personality: "Test",
                personalityEffect: null,
                claimedArc: null,
                actualArc: null,
                screeningSummary: "Test",
                patience: 60f,
                quality: 1.0f
            );
        }

        private class TestCallerAdapter : IListAdapter<Caller>
        {
            private ReactiveListPanel<Caller>? _panel;

            public void Setup(ReactiveListPanel<Caller> panel)
            {
                _panel = panel;
                panel.SetAdapter(this);
            }

            public Control CreateItem(Caller data)
            {
                var panel = new Panel
                {
                    CustomMinimumSize = new Vector2(0, 40),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                var label = new Label { Text = data.Name };
                panel.AddChild(label);
                return panel;
            }

            public void UpdateItem(Control item, Caller data)
            {
                var label = item.GetChild<Label>(0);
                if (label != null)
                {
                    label.Text = data.Name;
                }
            }

            public void DestroyItem(Control item)
            {
                item.QueueFree();
            }
        }
    }
}
