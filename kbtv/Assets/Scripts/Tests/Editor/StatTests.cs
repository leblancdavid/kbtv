using NUnit.Framework;
using KBTV.Data;

namespace KBTV.Tests
{
    /// <summary>
    /// Unit tests for the Stat class.
    /// </summary>
    public class StatTests
    {
        [Test]
        public void Constructor_SetsInitialValueClamped()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            Assert.AreEqual(50f, stat.Value);
            Assert.AreEqual("Test", stat.Name);
        }

        [Test]
        public void Constructor_ClampsValueAboveMax()
        {
            var stat = new Stat("Test", 150f, 0f, 100f);
            Assert.AreEqual(100f, stat.Value);
        }

        [Test]
        public void Constructor_ClampsValueBelowMin()
        {
            var stat = new Stat("Test", -50f, 0f, 100f);
            Assert.AreEqual(0f, stat.Value);
        }

        [Test]
        public void SetValue_ClampsToMax()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            stat.SetValue(200f);
            Assert.AreEqual(100f, stat.Value);
        }

        [Test]
        public void SetValue_ClampsToMin()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            stat.SetValue(-100f);
            Assert.AreEqual(0f, stat.Value);
        }

        [Test]
        public void Modify_AddsDeltaCorrectly()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            stat.Modify(25f);
            Assert.AreEqual(75f, stat.Value);
        }

        [Test]
        public void Modify_SubtractsDeltaCorrectly()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            stat.Modify(-30f);
            Assert.AreEqual(20f, stat.Value);
        }

        [Test]
        public void Modify_ClampsResult()
        {
            var stat = new Stat("Test", 90f, 0f, 100f);
            stat.Modify(50f);
            Assert.AreEqual(100f, stat.Value);
        }

        [Test]
        public void OnValueChanged_FiresWhenValueChanges()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            float capturedOldValue = -1f;
            float capturedNewValue = -1f;
            int callCount = 0;

            stat.OnValueChanged += (oldVal, newVal) =>
            {
                capturedOldValue = oldVal;
                capturedNewValue = newVal;
                callCount++;
            };

            stat.SetValue(75f);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(50f, capturedOldValue);
            Assert.AreEqual(75f, capturedNewValue);
        }

        [Test]
        public void OnValueChanged_DoesNotFireWhenValueUnchanged()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            int callCount = 0;

            stat.OnValueChanged += (oldVal, newVal) => callCount++;

            stat.SetValue(50f);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void OnValueChanged_DoesNotFireWhenClampedToSameValue()
        {
            var stat = new Stat("Test", 100f, 0f, 100f);
            int callCount = 0;

            stat.OnValueChanged += (oldVal, newVal) => callCount++;

            stat.SetValue(150f); // Should clamp to 100, same as current

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Normalized_ReturnsCorrectValue()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            Assert.AreEqual(0.5f, stat.Normalized, 0.001f);
        }

        [Test]
        public void Normalized_ReturnsZeroAtMin()
        {
            var stat = new Stat("Test", 0f, 0f, 100f);
            Assert.AreEqual(0f, stat.Normalized, 0.001f);
        }

        [Test]
        public void Normalized_ReturnsOneAtMax()
        {
            var stat = new Stat("Test", 100f, 0f, 100f);
            Assert.AreEqual(1f, stat.Normalized, 0.001f);
        }

        [Test]
        public void Normalized_WorksWithCustomRange()
        {
            var stat = new Stat("Test", 75f, 50f, 100f);
            Assert.AreEqual(0.5f, stat.Normalized, 0.001f);
        }

        [Test]
        public void IsEmpty_ReturnsTrueAtMin()
        {
            var stat = new Stat("Test", 0f, 0f, 100f);
            Assert.IsTrue(stat.IsEmpty);
        }

        [Test]
        public void IsEmpty_ReturnsFalseAboveMin()
        {
            var stat = new Stat("Test", 1f, 0f, 100f);
            Assert.IsFalse(stat.IsEmpty);
        }

        [Test]
        public void IsFull_ReturnsTrueAtMax()
        {
            var stat = new Stat("Test", 100f, 0f, 100f);
            Assert.IsTrue(stat.IsFull);
        }

        [Test]
        public void IsFull_ReturnsFalseBelowMax()
        {
            var stat = new Stat("Test", 99f, 0f, 100f);
            Assert.IsFalse(stat.IsFull);
        }

        [Test]
        public void Reset_SetsValueAndFiresEvent()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            int callCount = 0;
            stat.OnValueChanged += (oldVal, newVal) => callCount++;

            stat.Reset(75f);

            Assert.AreEqual(75f, stat.Value);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Reset_ClampsValue()
        {
            var stat = new Stat("Test", 50f, 0f, 100f);
            stat.Reset(200f);
            Assert.AreEqual(100f, stat.Value);
        }
    }
}
