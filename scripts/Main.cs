using System.Reflection;
using Godot;
using KBTV.Core;

#if DEBUG
using Chickensoft.GoDotTest;
#endif

namespace KBTV
{
    public partial class Main : Node2D
    {
#if DEBUG
        private TestEnvironment _environment = default!;
#endif

        public override void _Ready()
        {
#if DEBUG
            _environment = TestEnvironment.From(OS.GetCmdlineArgs());
            if (_environment.ShouldRunTests)
            {
                CallDeferred(nameof(RunTests));
                return;
            }
#endif
            GD.Print("Main: Game scene loaded");
        }

#if DEBUG
        private void RunTests()
            => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, _environment);
#endif
    }
}
