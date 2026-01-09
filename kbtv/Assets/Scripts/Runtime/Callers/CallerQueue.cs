using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Manages the queue of incoming callers during a live show.
    /// Handles caller lifecycle from incoming -> screening -> on-hold -> on-air.
    /// </summary>
    public class CallerQueue : SingletonMonoBehaviour<CallerQueue>
    {
        [Header("Queue Settings")]
        [Tooltip("Maximum callers that can be waiting")]
        [SerializeField] private int _maxQueueSize = 10;

        [Tooltip("Maximum callers on hold (approved, waiting for air)")]
        [SerializeField] private int _maxOnHold = 3;

        // Caller lists by state
        private List<Caller> _incomingCallers = new List<Caller>();
        private List<Caller> _onHoldCallers = new List<Caller>();
        private Caller _currentScreening;
        private Caller _onAirCaller;

        // Track event handlers to prevent memory leaks
        private Dictionary<Caller, Action> _disconnectHandlers = new Dictionary<Caller, Action>();

        // Properties
        public IReadOnlyList<Caller> IncomingCallers => _incomingCallers;
        public IReadOnlyList<Caller> OnHoldCallers => _onHoldCallers;
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

        private void Update()
        {
            UpdateCallerPatience(Time.deltaTime);
        }

        /// <summary>
        /// Add a new incoming caller to the queue.
        /// </summary>
        public bool AddCaller(Caller caller)
        {
            if (!CanAcceptMoreCallers)
            {
                Debug.Log("CallerQueue: Queue is full, rejecting caller");
                return false;
            }

            caller.SetState(CallerState.Incoming);
            
            // Track handler to prevent memory leak
            Action handler = () => HandleCallerDisconnected(caller);
            _disconnectHandlers[caller] = handler;
            caller.OnDisconnected += handler;
            
            _incomingCallers.Add(caller);

            Debug.Log($"CallerQueue: Added caller {caller.Name}");
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
                Debug.LogWarning("CallerQueue: Already screening a caller");
                return null;
            }

            if (_incomingCallers.Count == 0)
            {
                Debug.Log("CallerQueue: No incoming callers to screen");
                return null;
            }

            _currentScreening = _incomingCallers[0];
            _incomingCallers.RemoveAt(0);
            _currentScreening.SetState(CallerState.Screening);

            Debug.Log($"CallerQueue: Now screening {_currentScreening.Name}");
            return _currentScreening;
        }

        /// <summary>
        /// Approve the current caller and put them on hold.
        /// </summary>
        public bool ApproveCurrentCaller()
        {
            if (_currentScreening == null)
            {
                Debug.LogWarning("CallerQueue: No caller being screened");
                return false;
            }

            if (!CanPutOnHold)
            {
                Debug.LogWarning("CallerQueue: On-hold queue is full");
                return false;
            }

            _currentScreening.SetState(CallerState.OnHold);
            _onHoldCallers.Add(_currentScreening);
            
            Debug.Log($"CallerQueue: Approved {_currentScreening.Name}, now on hold");
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
                Debug.LogWarning("CallerQueue: No caller being screened");
                return false;
            }

            _currentScreening.SetState(CallerState.Rejected);
            
            Debug.Log($"CallerQueue: Rejected {_currentScreening.Name}");
            UnsubscribeCaller(_currentScreening);
            OnCallerRemoved?.Invoke(_currentScreening);
            _currentScreening = null;
            return true;
        }

        /// <summary>
        /// Put the next on-hold caller on air with Vern.
        /// </summary>
        public Caller PutNextCallerOnAir()
        {
            if (_onAirCaller != null)
            {
                Debug.LogWarning("CallerQueue: Already have a caller on air");
                return null;
            }

            if (_onHoldCallers.Count == 0)
            {
                Debug.Log("CallerQueue: No callers on hold");
                return null;
            }

            _onAirCaller = _onHoldCallers[0];
            _onHoldCallers.RemoveAt(0);
            _onAirCaller.SetState(CallerState.OnAir);

            Debug.Log($"CallerQueue: {_onAirCaller.Name} is now ON AIR");
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
                Debug.LogWarning("CallerQueue: No caller on air");
                return null;
            }

            _onAirCaller.SetState(CallerState.Completed);
            Caller completed = _onAirCaller;
            
            Debug.Log($"CallerQueue: {_onAirCaller.Name} call completed");
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
            Debug.Log("CallerQueue: Cleared all callers");
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

            // Update on-hold callers (they're more patient, so slower decay)
            for (int i = _onHoldCallers.Count - 1; i >= 0; i--)
            {
                if (_onHoldCallers[i].UpdateWaitTime(deltaTime * 0.5f))
                {
                    // Caller hung up
                    var caller = _onHoldCallers[i];
                    UnsubscribeCaller(caller);
                    OnCallerDisconnected?.Invoke(caller);
                    _onHoldCallers.RemoveAt(i);
                }
            }

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
            Debug.Log($"CallerQueue: {caller.Name} disconnected (ran out of patience)");
            OnCallerDisconnected?.Invoke(caller);
        }
    }
}
