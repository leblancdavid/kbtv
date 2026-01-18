#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.Callers
{
    /// <summary>
    /// Unified caller storage using caller state as the source of truth.
    /// Caller's State property determines which list they belong to.
    /// </summary>
    public partial class CallerRepository : ICallerRepository
    {
        private readonly Dictionary<string, Caller> _callers = new();
        private string? _currentScreeningId;
        private string? _onAirCallerId;
        private readonly List<ICallerRepositoryObserver> _observers = new();
        private readonly Dictionary<Caller, Action> _disconnectHandlers = new();
        private IArcRepository? _arcRepository;

        private const int MAX_INCOMING = 10;
        private const int MAX_ON_HOLD = 10;

        public IReadOnlyList<Caller> IncomingCallers => 
            _callers.Values.Where(c => c.State == CallerState.Incoming).ToList();
        
        public IReadOnlyList<Caller> OnHoldCallers => 
            _callers.Values.Where(c => c.State == CallerState.OnHold).ToList();
        
        public Caller? CurrentScreening => 
            _currentScreeningId != null ? GetCaller(_currentScreeningId) : null;
        
        public Caller? OnAirCaller => 
            _onAirCallerId != null ? GetCaller(_onAirCallerId) : null;

        public bool HasIncomingCallers => IncomingCallers.Count > 0;
        public bool HasOnHoldCallers => OnHoldCallers.Count > 0;
        public bool IsScreening => _currentScreeningId != null;
        public bool IsOnAir => _onAirCallerId != null;
        public bool CanAcceptMoreCallers => IncomingCallers.Count < MAX_INCOMING;
        public bool CanPutOnHold => OnHoldCallers.Count < MAX_ON_HOLD;

        private IArcRepository GetArcRepository()
        {
            if (_arcRepository == null && ServiceRegistry.IsInitialized)
            {
                _arcRepository = ServiceRegistry.Instance?.ArcRepository;
            }
            return _arcRepository!;
        }

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

            if (caller.State == CallerState.Screening || caller.State == CallerState.OnHold)
            {
                return Result<Caller>.Fail("Caller already in another state", "CALLER_BUSY");
            }

            _callers[caller.Id] = caller;
            caller.SetState(CallerState.Incoming);

            var handler = new Action(() => HandleCallerDisconnected(caller));
            _disconnectHandlers[caller] = handler;
            caller.OnDisconnected += handler;

            NotifyObservers(o => o.OnCallerAdded(caller));

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

            var previousScreening = _currentScreeningId;
            _currentScreeningId = caller.Id;

            if (previousScreening != null && previousScreening != caller.Id)
            {
                var prev = GetCaller(previousScreening);
                if (prev != null)
                {
                    prev.SetState(CallerState.Incoming);
                }
            }

            var screeningController = Core.ServiceRegistry.Instance?.ScreeningController;
            screeningController?.Start(caller);

            NotifyObservers(o => o.OnScreeningStarted(caller));

            return Result<Caller>.Ok(caller);
        }
        
        public Result<Caller> StartScreeningNext()
        {
            if (IsScreening)
            {
                return Result<Caller>.Fail("Already screening a caller", "SCREENING_BUSY");
            }

            var incoming = IncomingCallers.FirstOrDefault();
            if (incoming == null)
            {
                return Result<Caller>.Fail("No incoming callers", "NO_INCOMING");
            }

            return StartScreening(incoming);
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

            var arcRepo = GetArcRepository();
            if (arcRepo != null)
            {
                var topic = GetCallerTopic(caller);
                var legitimacy = GetCallerLegitimacy(caller);
                var arc = arcRepo.GetRandomArc(topic, legitimacy);
                caller.SetArc(arc);
            }

            caller.SetState(CallerState.OnHold);
            _currentScreeningId = null;

            NotifyObservers(o => o.OnScreeningEnded(caller, approved: true));

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

            caller.SetState(CallerState.Rejected);
            RemoveCaller(caller);
            _currentScreeningId = null;

            NotifyObservers(o => o.OnScreeningEnded(caller, approved: false));

            return Result<Caller>.Ok(caller);
        }
        
        public Result<Caller> PutOnAir()
        {
            if (IsOnAir)
            {
                return Result<Caller>.Fail("Already have a caller on air", "ON_AIR_BUSY");
            }

            var onHold = OnHoldCallers.FirstOrDefault();
            if (onHold == null)
            {
                return Result<Caller>.Fail("No on-hold callers", "NO_ON_HOLD");
            }

            _onAirCallerId = onHold.Id;
            onHold.SetState(CallerState.OnAir);

            NotifyObservers(o => o.OnCallerOnAir(onHold));

            return Result<Caller>.Ok(onHold);
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

            caller.SetState(CallerState.Completed);
            RemoveCaller(caller);
            _onAirCallerId = null;

            NotifyObservers(o => o.OnCallerOnAirEnded(caller));

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

            caller.SetState(newState);
            NotifyObservers(o => o.OnCallerStateChanged(caller, oldState, newState));

            return true;
        }
        
        public bool RemoveCaller(Caller caller)
        {
            if (caller == null || !_callers.ContainsKey(caller.Id))
            {
                return false;
            }

            UnsubscribeCaller(caller);
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

            return true;
        }
        
        public void ClearAll()
        {
            foreach (var caller in _callers.Values.ToList())
            {
                UnsubscribeCaller(caller);
            }

            _callers.Clear();
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
            caller.SetState(CallerState.Disconnected);
            RemoveCaller(caller);
        }

        private void NotifyObservers(Action<ICallerRepositoryObserver> action)
        {
            foreach (var observer in _observers.ToList())
            {
                action(observer);
            }
        }

        public Caller? GetCaller(string callerId)
        {
            return _callers.TryGetValue(callerId, out var caller) ? caller : null;
        }

        private static string GetCallerTopic(Caller caller)
        {
            return string.IsNullOrEmpty(caller.ClaimedTopic) ? caller.ActualTopic : caller.ClaimedTopic;
        }

        private static CallerLegitimacy GetCallerLegitimacy(Caller caller)
        {
            return caller.Legitimacy;
        }
    }
}
