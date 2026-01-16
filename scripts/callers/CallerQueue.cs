#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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

            var events = ServiceRegistry.Instance.EventAggregator;
            if (events == null)
            {
                GD.Print("WARNING: CallerQueue: EventAggregator not available");
                return;
            }

            events.Subscribe<Core.Events.Screening.ScreeningApproved>(this, OnScreeningApproved);
            events.Subscribe<Core.Events.Screening.ScreeningRejected>(this, OnScreeningRejected);
            events.Subscribe<Core.Events.Screening.ScreeningStarted>(this, OnScreeningStarted);
            events.Subscribe<Core.Events.OnAir.CallerOnAir>(this, OnCallerOnAir);
            events.Subscribe<Core.Events.OnAir.CallerOnAirEnded>(this, OnCallerOnAirEnded);

            GD.Print("CallerQueue: Initialized (legacy wrapper)");
        }

        public override void _ExitTree()
        {
            if (_repository != null)
            {
                _repository.Unsubscribe(this);
            }

            var events = ServiceRegistry.Instance?.EventAggregator;
            if (events == null)
            {
                return;
            }

            events.Unsubscribe<Core.Events.Screening.ScreeningApproved>(this);
            events.Unsubscribe<Core.Events.Screening.ScreeningRejected>(this);
            events.Unsubscribe<Core.Events.Screening.ScreeningStarted>(this);
            events.Unsubscribe<Core.Events.OnAir.CallerOnAir>(this);
            events.Unsubscribe<Core.Events.OnAir.CallerOnAirEnded>(this);
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
                if (caller.UpdateWaitTime(deltaTime))
                {
                    _repository.RemoveCaller(caller);
                }
            }

            _repository.CurrentScreening?.UpdateWaitTime(deltaTime);
        }

        private void OnScreeningApproved(Core.Events.Screening.ScreeningApproved evt)
        {
            if (evt.Caller != null)
            {
                EmitSignal("CallerApproved", evt.Caller);
                EmitSignal("ScreeningChanged");
            }
        }

        private void OnScreeningRejected(Core.Events.Screening.ScreeningRejected evt)
        {
            EmitSignal("CallerRemoved", evt.Caller);
            EmitSignal("ScreeningChanged");
        }

        private void OnScreeningStarted(Core.Events.Screening.ScreeningStarted evt)
        {
            EmitSignal("ScreeningChanged");
        }

        private void OnCallerOnAir(Core.Events.OnAir.CallerOnAir evt)
        {
            if (evt.Caller != null)
            {
                EmitSignal("CallerOnAir", evt.Caller);
            }
        }

        private void OnCallerOnAirEnded(Core.Events.OnAir.CallerOnAirEnded evt)
        {
            if (evt.Caller != null)
            {
                EmitSignal("CallerCompleted", evt.Caller);
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
