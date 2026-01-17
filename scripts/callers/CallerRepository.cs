#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Unified caller storage with state-based indexing.
    /// Replaces separate _incomingCallers and _onHoldCallers lists.
    /// </summary>
    public partial class CallerRepository : ICallerRepository
    {
        private readonly Dictionary<string, Caller> _callers = new();
        private readonly Dictionary<CallerState, HashSet<string>> _stateIndex = new();
        private string? _currentScreeningId;
        private string? _onAirCallerId;
        private readonly List<ICallerRepositoryObserver> _observers = new();
        private readonly Dictionary<Caller, Action> _disconnectHandlers = new();

        private const int MAX_INCOMING = 10;
        private const int MAX_ON_HOLD = 10;

        public CallerRepository()
        {
            InitializeStateIndex();
        }

        private void InitializeStateIndex()
        {
            foreach (CallerState state in Enum.GetValues(typeof(CallerState)))
            {
                _stateIndex[state] = new HashSet<string>();
            }
        }

        public IReadOnlyList<Caller> IncomingCallers => GetCallersByState(CallerState.Incoming);
        public IReadOnlyList<Caller> OnHoldCallers => GetCallersByState(CallerState.OnHold);
        public Caller? CurrentScreening => _currentScreeningId != null ? GetCaller(_currentScreeningId) : null;
        public Caller? OnAirCaller => _onAirCallerId != null ? GetCaller(_onAirCallerId) : null;

        public bool HasIncomingCallers => _stateIndex[CallerState.Incoming].Count > 0;
        public bool HasOnHoldCallers => _stateIndex[CallerState.OnHold].Count > 0;
        public bool IsScreening => _currentScreeningId != null;
        public bool IsOnAir => _onAirCallerId != null;
        public bool CanAcceptMoreCallers => _stateIndex[CallerState.Incoming].Count < MAX_INCOMING;
        public bool CanPutOnHold => _stateIndex[CallerState.OnHold].Count < MAX_ON_HOLD;

        public Result<Caller> AddCaller(Caller caller)
        {
            if (caller == null)
            {
                return Result<Caller>.Fail("Caller cannot be null", "NULL_CALLER");
            }

            if (!CanAcceptMoreCallers)
            {
                return Result<Caller>.Fail("Incoming queue is full", "QUEUE_FULL");
            }

            if (_callers.ContainsKey(caller.Id))
            {
                return Result<Caller>.Fail("Caller already exists", "DUPLICATE_CALLER");
            }

            _callers[caller.Id] = caller;
            UpdateStateIndex(caller.Id, CallerState.Incoming);
            caller.SetState(CallerState.Incoming);

            var handler = new Action(() => HandleCallerDisconnected(caller));
            _disconnectHandlers[caller] = handler;
            caller.OnDisconnected += handler;

            NotifyObservers(o => o.OnCallerAdded(caller));
            PublishEvent(new Core.Events.Queue.CallerAdded { Caller = caller });

            return Result<Caller>.Ok(caller);
        }

        public Result<Caller> StartScreening(Caller caller)
        {
            if (caller == null)
            {
                return Result<Caller>.Fail("Caller cannot be null", "NULL_CALLER");
            }

            if (!_callers.ContainsKey(caller.Id))
            {
                return Result<Caller>.Fail("Caller not found", "CALLER_NOT_FOUND");
            }

            if (IsScreening && _currentScreeningId != null && _currentScreeningId != caller.Id)
            {
                var currentCaller = GetCaller(_currentScreeningId);
                if (currentCaller != null)
                {
                    PublishEvent(new Core.Events.Screening.ScreeningEnded { Caller = currentCaller });
                }
                _currentScreeningId = null;
            }

            _currentScreeningId = caller.Id;

            var screeningController = Core.ServiceRegistry.Instance?.ScreeningController;
            screeningController?.Start(caller);

            PublishEvent(new Core.Events.Screening.ScreeningStarted { Caller = caller });

            return Result<Caller>.Ok(caller);
        }

        public Result<Caller> StartScreeningNext()
        {
            if (IsScreening)
            {
                return Result<Caller>.Fail("Already screening a caller", "SCREENING_BUSY");
            }

            var incoming = _stateIndex[CallerState.Incoming].FirstOrDefault();
            if (incoming == null)
            {
                return Result<Caller>.Fail("No incoming callers", "NO_INCOMING");
            }

            var caller = GetCaller(incoming);
            if (caller == null)
            {
                return Result<Caller>.Fail("Caller not found", "CALLER_NOT_FOUND");
            }
            return StartScreening(caller);
        }

        public Result<Caller> ApproveScreening()
        {
            if (!IsScreening || _currentScreeningId == null)
            {
                return Result<Caller>.Fail("No caller being screened", "NO_SCREENING");
            }

            if (!CanPutOnHold)
            {
                return Result<Caller>.Fail("On-hold queue is full", "HOLD_QUEUE_FULL");
            }

            var caller = GetCaller(_currentScreeningId);
            if (caller == null)
            {
                return Result<Caller>.Fail("Screening caller not found", "CALLER_NOT_FOUND");
            }

            var previousState = caller.State;
            SetCallerState(caller, CallerState.OnHold);

            PublishEvent(new Core.Events.Screening.ScreeningApproved { Caller = caller });

            _currentScreeningId = null;
            return Result<Caller>.Ok(caller);
        }

        public Result<Caller> RejectScreening()
        {
            if (!IsScreening || _currentScreeningId == null)
            {
                return Result<Caller>.Fail("No caller being screened", "NO_SCREENING");
            }

            var caller = GetCaller(_currentScreeningId);
            if (caller == null)
            {
                return Result<Caller>.Fail("Screening caller not found", "CALLER_NOT_FOUND");
            }

            SetCallerState(caller, CallerState.Rejected);

            PublishEvent(new Core.Events.Screening.ScreeningRejected { Caller = caller });

            RemoveCaller(caller);
            _currentScreeningId = null;

            return Result<Caller>.Ok(caller);
        }

        public Result<Caller> PutOnAir()
        {
            if (IsOnAir)
            {
                return Result<Caller>.Fail("Already have a caller on air", "ON_AIR_BUSY");
            }

            var onHold = _stateIndex[CallerState.OnHold].FirstOrDefault();
            if (onHold == null)
            {
                return Result<Caller>.Fail("No on-hold callers", "NO_ON_HOLD");
            }

            var caller = GetCaller(onHold);
            if (caller == null)
            {
                return Result<Caller>.Fail("On-hold caller not found", "CALLER_NOT_FOUND");
            }

            _onAirCallerId = caller.Id;
            SetCallerState(caller, CallerState.OnAir);

            PublishEvent(new Core.Events.OnAir.CallerOnAir { Caller = caller });

            return Result<Caller>.Ok(caller);
        }

        public Result<Caller> EndOnAir()
        {
            if (!IsOnAir || _onAirCallerId == null)
            {
                return Result<Caller>.Fail("No caller on air", "NO_ON_AIR");
            }

            var caller = GetCaller(_onAirCallerId);
            if (caller == null)
            {
                return Result<Caller>.Fail("On-air caller not found", "CALLER_NOT_FOUND");
            }

            SetCallerState(caller, CallerState.Completed);

            PublishEvent(new Core.Events.OnAir.CallerOnAirEnded { Caller = caller });

            RemoveCaller(caller);
            _onAirCallerId = null;

            return Result<Caller>.Ok(caller);
        }

        public bool SetCallerState(Caller caller, CallerState newState)
        {
            if (caller == null || !_callers.ContainsKey(caller.Id))
            {
                return false;
            }

            var oldState = caller.State;
            if (oldState == newState)
            {
                return false;
            }

            UpdateStateIndex(caller.Id, newState);
            caller.SetState(newState);

            NotifyObservers(o => o.OnCallerStateChanged(caller, oldState, newState));
            PublishEvent(new Core.Events.Queue.CallerStateChanged { Caller = caller, OldState = oldState, NewState = newState });

            return true;
        }

        public bool RemoveCaller(Caller caller)
        {
            if (caller == null || !_callers.ContainsKey(caller.Id))
            {
                return false;
            }

            UnsubscribeCaller(caller);
            _stateIndex[caller.State].Remove(caller.Id);
            _callers.Remove(caller.Id);

            if (_currentScreeningId == caller.Id)
            {
                _currentScreeningId = null;
            }
            if (_onAirCallerId == caller.Id)
            {
                _onAirCallerId = null;
            }

            NotifyObservers(o => o.OnCallerRemoved(caller));
            PublishEvent(new Core.Events.Queue.CallerRemoved { Caller = caller });

            return true;
        }

        public void ClearAll()
        {
            foreach (var caller in _callers.Values.ToList())
            {
                UnsubscribeCaller(caller);
            }

            _callers.Clear();
            InitializeStateIndex();
            _currentScreeningId = null;
            _onAirCallerId = null;

            GD.Print("CallerRepository: Cleared all callers");
        }

        public void Subscribe(ICallerRepositoryObserver observer)
        {
            if (observer != null && !_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void Unsubscribe(ICallerRepositoryObserver observer)
        {
            _observers.Remove(observer);
        }

        private void UpdateStateIndex(string callerId, CallerState newState)
        {
            var caller = GetCaller(callerId);
            if (caller != null)
            {
                _stateIndex[caller.State].Remove(callerId);
            }
            _stateIndex[newState].Add(callerId);
        }

        private IReadOnlyList<Caller> GetCallersByState(CallerState state)
        {
            return _stateIndex[state]
                .Select(GetCaller)
                .Where(c => c != null)
                .ToList()!;
        }

        private Caller? GetCaller(string callerId)
        {
            return _callers.TryGetValue(callerId, out var caller) ? caller : null;
        }

        private void UnsubscribeCaller(Caller caller)
        {
            if (caller == null)
            {
                return;
            }

            if (_disconnectHandlers.TryGetValue(caller, out var handler))
            {
                caller.OnDisconnected -= handler;
                _disconnectHandlers.Remove(caller);
            }
        }

        private void HandleCallerDisconnected(Caller caller)
        {
            SetCallerState(caller, CallerState.Disconnected);
            RemoveCaller(caller);

            PublishEvent(new Core.Events.Queue.CallerRemoved { Caller = caller });
        }

        private void NotifyObservers(Action<ICallerRepositoryObserver> action)
        {
            foreach (var observer in _observers.ToList())
            {
                action(observer);
            }
        }

        private void PublishEvent<TEvent>(TEvent eventData)
        {
            try
            {
                ServiceRegistry.Instance?.EventAggregator?.Publish(eventData);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"CallerRepository: Failed to publish event: {ex.Message}");
            }
        }
    }
}
