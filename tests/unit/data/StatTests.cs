using Chickensoft.GoDotTest;
using Godot;
using KBTV.Data;

namespace KBTV.Tests.Unit.Data
{
    public class StatTests : KBTVTestClass
    {
        public StatTests(Node testScene) : base(testScene) { }

        [Test]
        public void Constructor_SetsNameAndValue()
        {
            var stat = new Stat("TestStat", 50f);

            AssertThat(stat.Name == "TestStat");
            AssertThat(stat.Value == 50f);
        }

        [Test]
        public void Constructor_WithMinMax_ClampsValue()
        {
            var stat = new Stat("TestStat", 150f, 0f, 100f);

            AssertThat(stat.Value <= 100f);
        }

        [Test]
        public void Constructor_WithMinMax_BelowMin_ClampsToMin()
        {
            var stat = new Stat("TestStat", -50f, 0f, 100f);

            AssertThat(stat.Value >= 0f);
        }

        [Test]
        public void SetValue_EmitsOnValueChanged()
        {
            var stat = new Stat("TestStat", 50f);
            float? oldVal = null;
            float? newVal = null;
            stat.OnValueChanged += (old, newV) =>
            {
                oldVal = old;
                newVal = newV;
            };

            stat.SetValue(75f);

            AssertThat(oldVal == 50f);
            AssertThat(newVal == 75f);
        }

        [Test]
        public void Normalized_AtMinValue_ReturnsZero()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);
            stat.SetValue(0f);

            AssertThat(stat.Normalized == 0f);
        }

        [Test]
        public void Normalized_AtMaxValue_ReturnsOne()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);
            stat.SetValue(100f);

            AssertThat(stat.Normalized == 1f);
        }

        [Test]
        public void Normalized_AtMidValue_ReturnsHalf()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);
            stat.SetValue(50f);

            AssertThat(stat.Normalized == 0.5f);
        }

        [Test]
        public void Modify_IncreasesValue()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);

            stat.Modify(10f);

            AssertThat(stat.Value == 60f);
        }

        [Test]
        public void Modify_WithOverflow_ClampsToMax()
        {
            var stat = new Stat("TestStat", 90f, 0f, 100f);
            stat.Modify(20f);

            AssertThat(stat.Value <= 100f);
        }

        [Test]
        public void Modify_DecreasesValue()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);

            stat.Modify(-10f);

            AssertThat(stat.Value == 40f);
        }

        [Test]
        public void Modify_WithUnderflow_ClampsToMin()
        {
            var stat = new Stat("TestStat", 10f, 0f, 100f);
            stat.Modify(-20f);

            AssertThat(stat.Value >= 0f);
        }

        [Test]
        public void IsEmpty_AtMin_ReturnsTrue()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);
            stat.SetValue(0f);

            AssertThat(stat.IsEmpty);
        }

        [Test]
        public void IsEmpty_AboveMin_ReturnsFalse()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);

            AssertThat(!stat.IsEmpty);
        }

        [Test]
        public void IsFull_AtMax_ReturnsTrue()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);
            stat.SetValue(100f);

            AssertThat(stat.IsFull);
        }

        [Test]
        public void IsFull_BelowMax_ReturnsFalse()
        {
            var stat = new Stat("TestStat", 50f, 0f, 100f);

            AssertThat(!stat.IsFull);
        }
    }
}
