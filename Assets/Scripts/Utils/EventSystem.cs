using System;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// Event system for decoupled communication between components
    /// </summary>
    public class EventSystem
    {
        private static EventSystem _instance;
        public static EventSystem Instance => _instance ??= new EventSystem();

        private Dictionary<Type, Delegate> _eventCallbacks = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Subscribe to an event
        /// </summary>
        public void Subscribe<T>(Action<T> callback) where T : struct
        {
            var eventType = typeof(T);
            if (_eventCallbacks.ContainsKey(eventType))
            {
                _eventCallbacks[eventType] = Delegate.Combine(_eventCallbacks[eventType], callback);
            }
            else
            {
                _eventCallbacks[eventType] = callback;
            }
        }

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        public void Unsubscribe<T>(Action<T> callback) where T : struct
        {
            var eventType = typeof(T);
            if (_eventCallbacks.ContainsKey(eventType))
            {
                _eventCallbacks[eventType] = Delegate.Remove(_eventCallbacks[eventType], callback);
                if (_eventCallbacks[eventType] == null)
                {
                    _eventCallbacks.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Publish an event
        /// </summary>
        public void Publish<T>(T eventData) where T : struct
        {
            var eventType = typeof(T);
            if (_eventCallbacks.TryGetValue(eventType, out var callback))
            {
                (callback as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Clear all event callbacks
        /// </summary>
        public void Clear()
        {
            _eventCallbacks.Clear();
        }
    }

    // Common game events
    public struct QuestAddedEvent { public string QuestId; }
    public struct QuestRemovedEvent { public string QuestId; }
    public struct QuestAssignedEvent { public string QuestId; public string PartyId; public float EstimatedSuccessRate; }
    public struct QuestUnassignedEvent { public string QuestId; public string PartyId; }
    public struct QuestStartedEvent { public string QuestId; public int StartDay; public int ExpectedCompletionDay; }
    public struct QuestCompletedEvent
    {
        public string QuestId;
        public string PartyId;
        public bool IsSuccess;
        public int CompletionDay;
        public int GoldReward;
        public int ReputationChange;
        public System.Collections.Generic.List<MaterialReward> MaterialRewards;
    }
    public struct QuestReadyEvent { public string QuestId; public int CurrentDay; }
    public struct QuestFailedEvent { public string QuestId; public string PartyId; }
    public struct PartyRecruitedEvent { public string PartyId; public string PartyName; }
    public struct PartyAvailabilityChangedEvent { public string PartyId; public bool IsAvailable; }
    public struct MaterialTradedEvent { public string MaterialId; public int Quantity; public int TotalValue; }
    public struct DebtPaymentEvent { public int Amount; public int RemainingBalance; }
    public struct GameOverEvent { public string Reason; }
    public struct RelationshipChangedEvent { public string CharacterId; public int NewValue; }
    public struct StatChangedEvent { public string EntityId; public StatType StatType; public int NewValue; }
}
