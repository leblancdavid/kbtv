using Chickensoft.GoDotTest;
using Godot;
using KBTV.Persistence;

namespace KBTV.Tests.Unit.Persistence
{
    public class SaveDataTests : KBTVTestClass
    {
        public SaveDataTests(Node testScene) : base(testScene) { }

        [Test]
        public void CreateNew_SetsDefaultValues()
        {
            var saveData = SaveData.CreateNew();

            AssertThat(saveData.Version == 1);
            AssertThat(saveData.CurrentNight == 1);
            AssertThat(saveData.Money == 500);
            AssertThat(saveData.TotalCallersScreened == 0);
            AssertThat(saveData.TotalShowsCompleted == 0);
            AssertThat(saveData.PeakListenersAllTime == 0);
        }

        [Test]
        public void CreateNew_InitializesEquipmentLevels()
        {
            var saveData = SaveData.CreateNew();

            AssertThat(saveData.EquipmentLevels != null);
            AssertThat(saveData.EquipmentLevels.ContainsKey("PhoneLine"));
            AssertThat(saveData.EquipmentLevels.ContainsKey("Broadcast"));
            AssertThat(saveData.EquipmentLevels["PhoneLine"] == 1);
            AssertThat(saveData.EquipmentLevels["Broadcast"] == 1);
        }

        [Test]
        public void CreateNew_InitializesItemQuantities()
        {
            var saveData = SaveData.CreateNew();

            AssertThat(saveData.ItemQuantities != null);
            AssertThat(saveData.ItemQuantities["coffee"] == 3);
            AssertThat(saveData.ItemQuantities["water"] == 3);
            AssertThat(saveData.ItemQuantities["sandwich"] == 3);
        }

        [Test]
        public void Money_CanBeModified()
        {
            var saveData = SaveData.CreateNew();
            saveData.Money = 750;

            AssertThat(saveData.Money == 750);
        }

        [Test]
        public void CurrentNight_CanBeIncremented()
        {
            var saveData = SaveData.CreateNew();
            int initial = saveData.CurrentNight;

            saveData.CurrentNight++;

            AssertThat(saveData.CurrentNight > initial);
        }

        [Test]
        public void EquipmentLevel_CanBeUpgraded()
        {
            var saveData = SaveData.CreateNew();
            saveData.EquipmentLevels["PhoneLine"] = 2;

            AssertThat(saveData.EquipmentLevels["PhoneLine"] == 2);
        }

        [Test]
        public void AddItem_IncreasesQuantity()
        {
            var saveData = SaveData.CreateNew();
            int initial = saveData.ItemQuantities["coffee"];

            saveData.ItemQuantities["coffee"]++;

            AssertThat(saveData.ItemQuantities["coffee"] == initial + 1);
        }

        [Test]
        public void LastSaveTime_IsSet()
        {
            var saveData = SaveData.CreateNew();

            AssertThat(!string.IsNullOrEmpty(saveData.LastSaveTime));
        }
    }
}
