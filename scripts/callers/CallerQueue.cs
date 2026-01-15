using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;
// TODO: Add when ported - using KBTV.Ads;
// TODO: Add when ported - using KBTV.Audio;

namespace KBTV.Callers
{
	public partial class CallerQueue : Node
	{
		[Signal] public delegate void CallerAddedEventHandler(Caller caller);
		[Signal] public delegate void CallerRemovedEventHandler(Caller caller);
		[Signal] public delegate void CallerDisconnectedEventHandler(Caller caller);
		[Signal] public delegate void CallerOnAirEventHandler(Caller caller);
		[Signal] public delegate void CallerCompletedEventHandler(Caller caller);
		[Signal] public delegate void CallerApprovedEventHandler(Caller caller);

		public static CallerQueue Instance => (CallerQueue)((SceneTree)Engine.GetMainLoop()).Root.GetNode("/root/CallerQueue");

		private Dictionary<Caller, Action> _disconnectHandlers = new();
		private List<Caller> _incomingCallers = new();
		private List<Caller> _onHoldCallers = new();
		private Caller _currentScreening;
		private Caller _onAirCaller;

		public bool CanAcceptMoreCallers => _incomingCallers.Count < 10; // Max 10 incoming callers
		public bool CanPutOnHold => _onHoldCallers.Count < 10; // Max 10 on-hold callers
		public bool IsScreening => _currentScreening != null;
		public bool IsOnAir => _onAirCaller != null;
		public Caller OnAirCaller => _onAirCaller;
		public IReadOnlyList<Caller> IncomingCallers => _incomingCallers;
		public IReadOnlyList<Caller> OnHoldCallers => _onHoldCallers;
		public Caller CurrentScreening => _currentScreening;
		public bool HasIncomingCallers => _incomingCallers.Count > 0;
		public bool HasOnHoldCallers => _onHoldCallers.Count > 0;

        public override void _Process(double delta)
        {
            UpdateCallerPatience((float)delta);
        }

        /// <summary>
        /// Add a new incoming caller to the queue.
        /// </summary>
        public bool AddCaller(Caller caller)
        {
            if (!CanAcceptMoreCallers)
            {
                return false;
            }

            caller.SetState(CallerState.Incoming);

            // Track handler to prevent memory leak
            Action handler = () => HandleCallerDisconnected(caller);
            _disconnectHandlers[caller] = handler;
            caller.OnDisconnected += handler;

            _incomingCallers.Add(caller);

            EmitSignal("CallerAdded", caller);
            return true;
        }

        /// <summary>
        /// Start screening the next incoming caller.
        /// </summary>
        public Caller StartScreeningNext()
        {
            if (_currentScreening != null)
            {
                GD.Print("CallerQueue: Already screening a caller");
                return null;
            }

            if (_incomingCallers.Count == 0)
            {
                return null;
            }

            _currentScreening = _incomingCallers[0];
            _incomingCallers.RemoveAt(0);
            _currentScreening.SetState(CallerState.Screening);

            return _currentScreening;
        }

        /// <summary>
        /// Approve the current caller and put them on hold.
        /// </summary>
        public bool ApproveCurrentCaller()
        {
            if (_currentScreening == null)
            {
                GD.Print("CallerQueue: No caller being screened");
                return false;
            }

            if (!CanPutOnHold)
            {
                GD.Print("CallerQueue: On-hold queue is full");
                return false;
            }

            _currentScreening.SetState(CallerState.OnHold);
            _onHoldCallers.Add(_currentScreening);

            Caller approved = _currentScreening;
            _currentScreening = null;
            EmitSignal("CallerApproved", approved);
            return true;
        }

        /// <summary>
        /// Reject the current caller.
        /// </summary>
        public bool RejectCurrentCaller()
        {
            if (_currentScreening == null)
            {
                GD.Print("CallerQueue: No caller being screened");
                return false;
            }

            _currentScreening.SetState(CallerState.Rejected);

            UnsubscribeCaller(_currentScreening);
            EmitSignal("CallerRemoved", _currentScreening);
            _currentScreening = null;
            return true;
        }

        /// <summary>
        /// Put the next on-hold caller on air with Vern.
        /// Blocked during bumpers and ad breaks to prevent audio interruption.
        /// </summary>
        public Caller PutNextCallerOnAir()
        {
            // TODO: Block during bumpers and ad breaks
            // if (AdManager.Instance?.IsInBreak == true)
            // {
            //     GD.Print("CallerQueue: Cannot put caller on air during ad break");
            //     return null;
            // }
            // if (AudioManager.Instance?.IsPlayingBumper == true)
            // {
            //     GD.Print("CallerQueue: Cannot put caller on air during bumper");
            //     return null;
            // }

            if (_onAirCaller != null)
            {
                GD.Print("CallerQueue: Already have a caller on air");
                return null;
            }

            if (_onHoldCallers.Count == 0)
            {
                return null;
            }

            _onAirCaller = _onHoldCallers[0];
            _onHoldCallers.RemoveAt(0);
            _onAirCaller.SetState(CallerState.OnAir);

            EmitSignal("CallerOnAir", _onAirCaller);
            return _onAirCaller;
        }

        /// <summary>
        /// End the current on-air call.
        /// </summary>
        public Caller EndCurrentCall()
        {
            if (_onAirCaller == null)
            {
                GD.Print("CallerQueue: No caller on air");
                return null;
            }

            _onAirCaller.SetState(CallerState.Completed);
            Caller completed = _onAirCaller;

            UnsubscribeCaller(_onAirCaller);
            EmitSignal("CallerCompleted", _onAirCaller);
            _onAirCaller = null;
            return completed;
        }

        /// <summary>
        /// Clear all callers (used when show ends).
        /// </summary>
        public void ClearAll()
        {
            // Unsubscribe from all callers
            foreach (var caller in _incomingCallers)
                UnsubscribeCaller(caller);
            foreach (var caller in _onHoldCallers)
                UnsubscribeCaller(caller);
            if (_currentScreening != null)
                UnsubscribeCaller(_currentScreening);
            if (_onAirCaller != null)
                UnsubscribeCaller(_onAirCaller);

            _incomingCallers.Clear();
            _onHoldCallers.Clear();
            _currentScreening = null;
            _onAirCaller = null;
        }

        private void UpdateCallerPatience(float deltaTime)
        {
            // Update incoming callers
            for (int i = _incomingCallers.Count - 1; i >= 0; i--)
            {
                if (_incomingCallers[i].UpdateWaitTime(deltaTime))
                {
                    // Caller hung up
                    var caller = _incomingCallers[i];
                    UnsubscribeCaller(caller);
                    _incomingCallers.RemoveAt(i);
                }
            }

            // Note: OnHold callers don't tick - they're committed once approved
            // Their patience check is skipped in Caller.UpdateWaitTime()

            // Update screening caller
            _currentScreening?.UpdateWaitTime(deltaTime);
        }

        /// <summary>
        /// Unsubscribe from a caller's events and clean up handler reference.
        /// </summary>
        private void UnsubscribeCaller(Caller caller)
        {
            if (_disconnectHandlers.TryGetValue(caller, out Action handler))
            {
                caller.OnDisconnected -= handler;
                _disconnectHandlers.Remove(caller);
            }
        }

        private void HandleCallerDisconnected(Caller caller)
        {
            EmitSignal("CallerDisconnected", caller);
        }
    }
}