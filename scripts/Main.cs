using System.Reflection;
using Godot;
using KBTV.Core;

namespace KBTV
{
    public partial class Main : Node2D
    {
        public override void _Ready()
        {
            GD.Print("Main: Game scene loaded");
        }
    }
}
