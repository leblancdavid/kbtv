using System;
using Godot;
using KBTV.Callers;

namespace KBTV.Callers
{
    public interface ICallerGenerator
    {
        bool IsGenerating { get; }

        void StartGenerating();
        void StopGenerating();
        Caller SpawnCaller();
    }
}
