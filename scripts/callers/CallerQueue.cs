#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Legacy wrapper for backward compatibility.
    /// Delegates to ICallerRepository and handles patience updates.
    /// Use ServiceRegistry.Instance.CallerRepository for new code.
    /// </summary>
    [Obsolete("Use ICallerRepository from ServiceRegistry instead")]
    [SuppressMessage("csharp", "CS0618")]
    public partial class CallerQueue : Node, ICallerRepositoryObserver
    {
        [Signal] public delegate void CallerAddedEventHandler(Caller caller);
        [Signal] public delegate void CallerRemovedEventHandler(Caller caller);
        [Signal] public delegate void CallerDisconnectedEventHandler(Caller caller);
        [Signal] public delegate void CallerOnAirEventHandler(Caller caller);
        [Signal] public delegate void CallerCompletedEventHandler(Caller caller);
        [Signal] public delegate void CallerApprovedEventHandler(Caller caller);
        [Signal] public delegate void ScreeningChangedEventHandler();

        private ICallerRepository _repository = null!;
        private static int _instanceCount;
        private int _instanceId;

        public bool CanAcceptMoreCallers => _repository.CanAcceptMoreCallers;
        public bool CanPutOnHold => _repository.CanPutOnHold;
        public bool IsScreening => _repository.IsScreening;
        public bool IsOnAir => _repository.IsOnAir;
        public Caller? OnAirCaller => _repository.OnAirCaller;
        public IReadOnlyList<Caller> IncomingCallers => _repository.IncomingCallers;
        public IReadOnlyList<Caller> OnHoldCallers => _repository.OnHoldCallers;
        public Caller? CurrentScreening => _repository.CurrentScreening;
        public bool HasIncomingCallers => _repository.HasIncomingCallers;
        public bool HasOnHoldCallers => _repository.HasOnHoldCallers;

        public override void _Ready()
        {
            _instanceId = ++_instanceCount;
            GD.Print($"CallerQueue: _Ready called (instance #{_instanceId})");
            ServiceRegistry.Instance.RegisterSelf<CallerQueue>(this);

            if (ServiceRegistry.Instance == null)
            {
                GD.PrintErr("CallerQueue: ServiceRegistry not available");
                return;
            }

            _repository = ServiceRegistry.Instance.CallerRepository;

            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: ICallerRepository not available");
                return;
            }

            _repository.Subscribe(this);

            GD.Print($"CallerQueue: Initialized (legacy wrapper, instance #{_instanceId})");
        }

        public override void _ExitTree()
        {
            if (_repository != null)
            {
                _repository.Unsubscribe(this);
            }
        }

        public override void _Process(double delta)
        {
            if (_repository == null)
            {
                return;
            }
            UpdateCallerPatience((float)delta);
        }

        public bool AddCaller(Caller caller)
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return false;
            }
            var result = _repository.AddCaller(caller);
            return result.IsSuccess;
        }

        public Caller? StartScreeningNext()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return null;
            }
            var result = _repository.StartScreeningNext();
            return result.ValueOrDefault();
        }

        public bool StartScreeningCaller(Caller caller)
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return false;
            }
            var result = _repository.StartScreening(caller);
            return result.IsSuccess;
        }

        public bool ReplaceScreeningCaller(Caller caller)
        {
            if (caller == null)
            {
                return false;
            }

            if (IsScreening && CurrentScreening != null)
            {
                _repository.SetCallerState(CurrentScreening, CallerState.Incoming);
            }

            return _repository.StartScreening(caller).IsSuccess;
        }

        public bool ApproveCurrentCaller()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return false;
            }
            var result = _repository.ApproveScreening();
            return result.IsSuccess;
        }

        public bool RejectCurrentCaller()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return false;
            }
            var result = _repository.RejectScreening();
            return result.IsSuccess;
        }

        public Caller? PutNextCallerOnAir()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return null;
            }
            var result = _repository.PutOnAir();
            return result.ValueOrDefault();
        }

        public Caller? EndCurrentCall()
        {
            if (_repository == null)
            {
                GD.PrintErr("CallerQueue: Repository not available");
                return null;
            }
            var result = _repository.EndOnAir();
            return result.ValueOrDefault();
        }

        public void ClearAll()
        {
            if (_repository == null)
            {
                return;
            }
            _repository.ClearAll();
        }

        private void UpdateCallerPatience(float deltaTime)
        {
            foreach (var caller in _repository.IncomingCallers.ToList())
            {
                caller.UpdateWaitTime(deltaTime);
            }

            _repository.CurrentScreening?.UpdateWaitTime(deltaTime);
        }

        public void OnScreeningStarted(Caller caller)
        {
            EmitSignal("ScreeningChanged");
        }

        public void OnScreeningEnded(Caller caller, bool approved)
        {
            EmitSignal("ScreeningChanged");
            if (approved && caller != null)
            {
                EmitSignal("CallerApproved", caller);
            }
            else if (caller != null)
            {
                EmitSignal("CallerRemoved", caller);
            }
        }

        public void OnCallerOnAir(Caller caller)
        {
            if (caller != null)
            {
                EmitSignal("CallerOnAir", caller);
            }
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            if (caller != null)
            {
                EmitSignal("CallerCompleted", caller);
            }
        }

        public void OnCallerAdded(Caller caller)
        {
            EmitSignal("CallerAdded", caller);
        }

        public void OnCallerRemoved(Caller caller)
        {
            EmitSignal("CallerRemoved", caller);
            EmitSignal("CallerDisconnected", caller);
        }

        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState)
        {
            if (newState == CallerState.Disconnected)
            {
                EmitSignal("CallerDisconnected", caller);
            }
        }
    }
}
