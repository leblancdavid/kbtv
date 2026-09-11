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

    [Test]
    public void SetPlayerLocation_DisablesBoundsDetectionInProcess()
    {
        var roomState = new RoomStateManager();
        roomState.SetControlRoomBounds(new Rect2(0, 0, 10, 10));
        roomState.SetStudioBounds(new Rect2(20, 20, 10, 10));

        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InControlRoom);
        roomState.SetPlayerLocation(RoomStateManager.PlayerLocation.InStudio);

        // Bounds would resolve to InControlRoom, but manual reporting must win.
        roomState._Process(0.016);

        AssertThat(roomState.CurrentLocation == RoomStateManager.PlayerLocation.InStudio);
    }
}