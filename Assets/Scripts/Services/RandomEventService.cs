using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Types of random events that can occur
    /// </summary>
    public enum RandomEventType
    {
        MaterialPriceSpike,    // Material prices increase
        MaterialPriceDropped,  // Material prices decrease
        QuestBonusReward,      // Extra rewards for completing quests
        PartyLoyaltyBoost,     // All parties gain loyalty
        PartyLoyaltyDrop,      // All parties lose loyalty
        ReputationBoost,       // Player gains reputation
        ReputationDrop,        // Player loses reputation
        DebtInterestWaived,    // Interest payment skipped this quarter
        UnexpectedCost,        // Player loses gold
        BonusIncome            // Player gains gold
    }

    /// <summary>
    /// Random event data
    /// </summary>
    [Serializable]
    public class RandomEvent
    {
        public RandomEventType type;
        public string title;
        public string description;
        public int value; // Numeric value for the event effect
        public bool isPositive; // Whether this is a positive or negative event

        public RandomEvent(RandomEventType type, string title, string description, int value, bool isPositive)
        {
            this.type = type;
            this.title = title;
            this.description = description;
            this.value = value;
            this.isPositive = isPositive;
        }
    }

    /// <summary>
    /// Service that manages random events occurring during gameplay
    /// </summary>
    public class RandomEventService
    {
        private static RandomEventService _instance;
        public static RandomEventService Instance => _instance ??= new RandomEventService();

        private List<RandomEvent> eventHistory = new List<RandomEvent>();
        private float timeSinceLastEvent = 0f;
        private const float MIN_EVENT_INTERVAL = 30f; // Minimum 30 seconds between events
        private const float MAX_EVENT_INTERVAL = 120f; // Maximum 120 seconds between events
        private float nextEventTime;

        private RandomEventService()
        {
            nextEventTime = UnityEngine.Random.Range(MIN_EVENT_INTERVAL, MAX_EVENT_INTERVAL);
            
            // Subscribe to day advanced events for quest-based random events
            EventSystem.Instance.Subscribe<DayAdvancedEvent>(OnDayAdvanced);
            EventSystem.Instance.Subscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
        }

        /// <summary>
        /// Update the random event system (should be called from GameManager Update)
        /// </summary>
        public void Update(float deltaTime)
        {
            timeSinceLastEvent += deltaTime;

            if (timeSinceLastEvent >= nextEventTime)
            {
                TriggerRandomEvent();
                timeSinceLastEvent = 0f;
                nextEventTime = UnityEngine.Random.Range(MIN_EVENT_INTERVAL, MAX_EVENT_INTERVAL);
            }
        }

        /// <summary>
        /// Manually trigger a random event (for testing or specific triggers)
        /// </summary>
        public void TriggerRandomEvent()
        {
            // Select a random event type
            Array eventTypes = Enum.GetValues(typeof(RandomEventType));
            RandomEventType selectedType = (RandomEventType)eventTypes.GetValue(UnityEngine.Random.Range(0, eventTypes.Length));

            RandomEvent randomEvent = CreateEvent(selectedType);
            ApplyEvent(randomEvent);
            
            // Add to history
            eventHistory.Add(randomEvent);
            
            // Publish event for UI to display
            EventSystem.Instance.Publish(new RandomEventOccurredEvent { Event = randomEvent });
            
            Debug.Log($"Random Event: {randomEvent.title} - {randomEvent.description}");
        }

        /// <summary>
        /// Create a random event based on type
        /// </summary>
        private RandomEvent CreateEvent(RandomEventType type)
        {
            switch (type)
            {
                case RandomEventType.MaterialPriceSpike:
                    return new RandomEvent(
                        type,
                        "Market Boom!",
                        "Material prices have increased by 30%!",
                        30,
                        true
                    );

                case RandomEventType.MaterialPriceDropped:
                    return new RandomEvent(
                        type,
                        "Market Crash",
                        "Material prices have decreased by 20%.",
                        -20,
                        false
                    );

                case RandomEventType.QuestBonusReward:
                    return new RandomEvent(
                        type,
                        "Generous Client",
                        "Next quest will provide double rewards!",
                        100,
                        true
                    );

                case RandomEventType.PartyLoyaltyBoost:
                    return new RandomEvent(
                        type,
                        "Festival Day",
                        "All parties are in high spirits! Loyalty increased by 10.",
                        10,
                        true
                    );

                case RandomEventType.PartyLoyaltyDrop:
                    return new RandomEvent(
                        type,
                        "Bad Weather",
                        "Parties are feeling down. Loyalty decreased by 5.",
                        -5,
                        false
                    );

                case RandomEventType.ReputationBoost:
                    return new RandomEvent(
                        type,
                        "Positive News",
                        "Your guild's reputation has improved! +10 reputation.",
                        10,
                        true
                    );

                case RandomEventType.ReputationDrop:
                    return new RandomEvent(
                        type,
                        "Scandal",
                        "A scandal has damaged your reputation. -5 reputation.",
                        -5,
                        false
                    );

                case RandomEventType.DebtInterestWaived:
                    return new RandomEvent(
                        type,
                        "Tax Relief",
                        "The kingdom waived interest this quarter!",
                        0,
                        true
                    );

                case RandomEventType.UnexpectedCost:
                    int cost = UnityEngine.Random.Range(100, 500);
                    return new RandomEvent(
                        type,
                        "Unexpected Expense",
                        $"You had to pay {cost} gold for repairs.",
                        -cost,
                        false
                    );

                case RandomEventType.BonusIncome:
                    int income = UnityEngine.Random.Range(200, 800);
                    return new RandomEvent(
                        type,
                        "Windfall!",
                        $"You received {income} gold from a generous donor!",
                        income,
                        true
                    );

                default:
                    return new RandomEvent(type, "Event", "Something happened", 0, true);
            }
        }

        /// <summary>
        /// Apply the effects of a random event
        /// </summary>
        private void ApplyEvent(RandomEvent evt)
        {
            if (GameManager.Instance == null) return;

            switch (evt.type)
            {
                case RandomEventType.MaterialPriceSpike:
                case RandomEventType.MaterialPriceDropped:
                    // Material price changes are handled by MaterialService
                    if (GameManager.Instance.materialService != null)
                    {
                        // This would require MaterialService to have a method to adjust all prices
                        // For now, just log it
                        Debug.Log($"Material prices affected: {evt.value}%");
                    }
                    break;

                case RandomEventType.QuestBonusReward:
                    // Quest reward bonus is handled when quests complete
                    // Store flag for next quest completion
                    Debug.Log("Next quest will have bonus rewards");
                    break;

                case RandomEventType.PartyLoyaltyBoost:
                case RandomEventType.PartyLoyaltyDrop:
                    // Party loyalty changes would require PartyService integration
                    Debug.Log($"Party loyalty affected: {evt.value}");
                    break;

                case RandomEventType.ReputationBoost:
                case RandomEventType.ReputationDrop:
                    GameManager.Instance.ModifyReputation(evt.value);
                    break;

                case RandomEventType.DebtInterestWaived:
                    // Debt interest waiver would require DebtService integration
                    Debug.Log("Debt interest waived for this quarter");
                    break;

                case RandomEventType.UnexpectedCost:
                case RandomEventType.BonusIncome:
                    GameManager.Instance.ModifyGold(evt.value);
                    break;
            }
        }

        /// <summary>
        /// Get all event history
        /// </summary>
        public List<RandomEvent> GetEventHistory()
        {
            return new List<RandomEvent>(eventHistory);
        }

        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearHistory()
        {
            eventHistory.Clear();
        }

        private void OnDayAdvanced(DayAdvancedEvent evt)
        {
            // Small chance for events on day change
            if (UnityEngine.Random.value < 0.1f) // 10% chance per day
            {
                TriggerRandomEvent();
            }
        }

        private void OnQuarterAdvanced(QuarterAdvancedEvent evt)
        {
            // Higher chance for events on quarter change
            if (UnityEngine.Random.value < 0.5f) // 50% chance per quarter
            {
                TriggerRandomEvent();
            }
        }
    }

    // Event for when a random event occurs
    public struct RandomEventOccurredEvent
    {
        public RandomEvent Event;
    }
}
