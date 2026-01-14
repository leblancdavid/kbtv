using System;
using Godot;

namespace KBTV.UI.Controllers
{
    public partial class TabDefinition
    {
        public string Name { get; set; }
        public Action<Control> PopulateContent { get; set; }
        public Action OnTabSelected { get; set; }
        public object UserData { get; set; }
    }
}