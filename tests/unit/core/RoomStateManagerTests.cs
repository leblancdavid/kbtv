#nullable enable

using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;

namespace KBTV.Tests.Unit.Core;

public class RoomStateManagerTests : KBTVTestClass
{
    public RoomStateManagerTests(Node testScene) : base(testScene) { }

    [Test]
    public void SetPlayerLocation_UpdatesCurrentLocation()
    {
        var roomState = new RoomStateManager();

        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InControlRoom);

        AssertThat(roomState.CurrentLocation == RoomStateManager.PlayerLocation.InControlRoom);
    }

    [Test]
    public void SetPlayerLocation_EmitOnlyOnChange()
    {
        var roomState = new RoomStateManager();
        var changeCount = 0;
        roomState.PlayerLocationChanged += _ => changeCount++;

        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InControlRoom);
        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InControlRoom);
        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InStudio);

        AssertAreEqual(2, changeCount);
    }

}
