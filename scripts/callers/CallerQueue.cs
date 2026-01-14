using System;
using System.Collections.Generic;
using Godot;
using KBTV.Core;
// TODO: Add when ported - using KBTV.Ads;
// TODO: Add when ported - using KBTV.Audio;

namespace KBTV.Callers
{
    /// <summary>
    /// Manages the queue of incoming callers during a live show.
    /// Handles caller lifecycle from incoming -> screening -> on-hold -> on-air.
    /// </summary>
    public partial class CallerQueue : SingletonNode<CallerQueue>
    {
        [Export] private int _maxQueueSize = 3;
        [Export] private int _maxOnHold = 3;

        // Caller lists by state
        private System.Collections.Generic.List<Caller> _incomingCallers = new System.Collections.Generic.List<Caller>();
        private System.Collections.Generic.List<Caller> _onHoldCallers = new System.Collections.Generic.List<Caller>();
        private Caller _currentScreening;
        private Caller _onAirCaller;

        // Track event handlers to prevent memory leaks
        private Dictionary<Caller, Action> _disconnectHandlers = new Dictionary<Caller, Action>();

        // Properties
        public System.Collections.Generic.IReadOnlyList<Caller> IncomingCallers => _incomingCallers;
        public System.Collections.Generic.IReadOnlyList<Caller> OnHoldCallers => _onHoldCallers;
        public Caller CurrentScreening => _currentScreening;
        public Caller OnAirCaller => _onAirCaller;
        public int TotalWaiting => _incomingCallers.Count + _onHoldCallers.Count;
        public bool HasIncomingCallers => _incomingCallers.Count > 0;
        public bool HasOnHoldCallers => _onHoldCallers.Count > 0;
        public bool IsScreening => _currentScreening != null;
        public bool IsOnAir => _onAirCaller != null;
        public bool CanAcceptMoreCallers => _incomingCallers.Count < _maxQueueSize;
        public bool CanPutOnHold => _onHoldCallers.Count < _maxOnHold;

        // Events
        public event Action<Caller> OnCallerAdded;
        public event Action<Caller> OnCallerRemoved;
        public event Action<Caller> OnCallerDisconnected;
        public event Action<Caller> OnCallerOnAir;
        public event Action<Caller> OnCallerCompleted;
        public event Action<Caller> OnCallerApproved;  // Fired when caller moves to on-hold

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

            OnCallerAdded?.Invoke(caller);
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
            OnCallerApproved?.Invoke(approved);
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
            OnCallerRemoved?.Invoke(_currentScreening);
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

            OnCallerOnAir?.Invoke(_onAirCaller);
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
            OnCallerCompleted?.Invoke(_onAirCaller);
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
            OnCallerDisconnected?.Invoke(caller);
        }
    }
}