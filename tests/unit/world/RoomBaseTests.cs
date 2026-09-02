using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests.Unit.World
{
    public partial class RoomBaseTests : KBTVTestClass
    {
        public RoomBaseTests(Node testScene) : base(testScene) { }

        private sealed partial class TestRoom : RoomBase
        {
            protected override void ConfigureRoom()
            {
                GridAnchor = new Vector2(128, 256);
                GridWidth = 3;
                GridHeight = 2;
            }

            protected override void OnRoomReady()
            {
                var debug = new RoomDebug { DebugEnabled = false };
                AddChild(debug);
                DebugNode = debug;
                debug.Initialize(this, null, null, null, null, null);
            }
        }

        private TestRoom CreateReadyRoom()
        {
            var room = new TestRoom();
            TestScene.AddChild(room);
            return room;
        }

        [Test]
        public void Ready_CreatesAllRoomLayers()
        {
            var room = CreateReadyRoom();

            AssertThat(room.FloorLayer != null, "FloorLayer should be created");
            AssertThat(room.DoorLayer != null, "DoorLayer should be created");
            AssertThat(room.GridDebugLayer != null, "GridDebugLayer should be created");
            AssertThat(room.PropSort != null, "PropSort should be created");
            AssertThat(room.PropSort.YSortEnabled, "PropSort should Y-sort props");

            room.Free();
        }

        [Test]
        public void Ready_SetsGridOffsetToAnchor()
        {
            var room = CreateReadyRoom();

            AssertThat(room.GridOffset == new Vector2(128, 256), "GridOffset should match GridAnchor");

            room.Free();
        }

        [Test]
        public void GetFloorBounds_MatchesConfiguredGrid()
        {
            var room = CreateReadyRoom();

            var bounds = room.GetFloorBounds();
            AssertAreEqual(new Vector2(3 * RoomBase.TileSize, 2 * RoomBase.TileSize), bounds.Size, "Floor bounds size should be grid tiles x TileSize");
            AssertAreEqual(room.GridToWorld(Vector2I.Zero), bounds.Position, "Floor bounds position should be the grid origin in world space");

            room.Free();
        }

        [Test]
        public void GridToWorld_AdjacentCells_StepByTileSize()
        {
            var room = CreateReadyRoom();

            var a = room.GridToWorld(new Vector2I(0, 0));
            var b = room.GridToWorld(new Vector2I(1, 0));
            var c = room.GridToWorld(new Vector2I(0, 1));

            AssertAreEqual(RoomBase.TileSize, b.X - a.X, $"X step should be one tile ({RoomBase.TileSize})");
            AssertAreEqual(RoomBase.TileSize, c.Y - a.Y, $"Y step should be one tile ({RoomBase.TileSize})");

            room.Free();
        }

        [Test]
        public void WorldToGrid_GridToWorld_RoundTrip_ReturnsOriginalCell()
        {
            var room = CreateReadyRoom();

            foreach (var cell in new[] { new Vector2I(0, 0), new Vector2I(2, 0), new Vector2I(0, 1), new Vector2I(2, 1) })
            {
                var world = room.GridToWorld(cell);
                var roundTrip = room.WorldToGrid(world);
                AssertAreEqual(cell, roundTrip, $"Round trip should return cell {cell}");
            }

            room.Free();
        }

        [Test]
        public void SetPlayer_TracksPlayer()
        {
            var room = CreateReadyRoom();
            var player = new CharacterBody2D();
            TestScene.AddChild(player);

            room.SetPlayer(player);

            AssertThat(room.Player == player, "SetPlayer should store the player reference");

            player.Free();
            room.Free();
        }

        [Test]
        public void GridDebugLayer_HiddenByDefault()
        {
            var room = CreateReadyRoom();

            AssertThat(!room.GridDebugLayer.Visible, "Grid debug layer should start hidden");
            room.ToggleDebug();
            AssertThat(room.GridDebugLayer.Visible, "ToggleDebug should reveal the grid debug layer");

            room.Free();
        }
    }
}