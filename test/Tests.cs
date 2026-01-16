using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Chickensoft.GoDotTest;

namespace KBTV.Tests
{
    public partial class Tests : Node2D
    {
        public override async void _Ready()
        {
            await GoTest.RunTests(Assembly.GetExecutingAssembly(), this);
        }
    }
}
