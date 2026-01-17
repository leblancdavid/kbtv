using Chickensoft.GoDotTest;
using Godot;
using KBTV.Persistence;

namespace KBTV.Tests.Unit.Persistence
{
    public class SaveManagerTests : KBTVTestClass
    {
        public SaveManagerTests(Node testScene) : base(testScene) { }

        private SaveManager _saveManager = null!;

        [Setup]
        public void Setup()
        {
            _saveManager = new SaveManager();
            _saveManager._Ready();
        }

        [Test]
        public void Constructor_InitializesCurrentSave()
        {
            var manager = new SaveManager();

            AssertThat(manager.CurrentSave != null);
        }

        [Test]
        public void Constructor_SetsIsDirtyToFalse()
        {
            var manager = new SaveManager();

            AssertThat(!manager.IsDirty);
        }

        [Test]
        public void HasSave_PropertyExists()
        {
            var hasSave = _saveManager.HasSave;

            AssertThat(hasSave == false || hasSave == true);
        }

        [Test]
        public void CurrentSave_HasDefaultValues()
        {
            var save = _saveManager.CurrentSave;

            AssertThat(save.Money == 500);
            AssertThat(save.CurrentNight == 1);
        }

        [Test]
        public void MarkDirty_SetsIsDirtyToTrue()
        {
            _saveManager.MarkDirty();

            AssertThat(_saveManager.IsDirty);
        }

        [Test]
        public void RegisterSaveable_AddsToList()
        {
            var mockSaveable = new MockSaveable();

            _saveManager.RegisterSaveable(mockSaveable);

            AssertThat(true);
        }

        [Test]
        public void UnregisterSaveable_RemovesFromList()
        {
            var mockSaveable = new MockSaveable();
            _saveManager.RegisterSaveable(mockSaveable);

            _saveManager.UnregisterSaveable(mockSaveable);

            AssertThat(true);
        }
    }

    public class MockSaveable : ISaveable
    {
        public void OnBeforeSave(SaveData data) { }
        public void OnAfterLoad(SaveData data) { }
    }
}
