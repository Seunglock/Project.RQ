using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Quest entity representing available missions
    /// </summary>
    [Serializable]
    public class Quest
    {
        public string id;
        public QuestType type;
        public int difficulty;
        public Dictionary<StatType, int> requiredStats;
        public int duration;
        public int rewardGold;
        public List<MaterialReward> rewardMaterials;
        public float successRate;
        public int reputationImpact;
        public QuestState state;
        public string assignedPartyId;
        public int startDay;

        public Quest()
        {
            id = Guid.NewGuid().ToString();
            requiredStats = new Dictionary<StatType, int>();
            rewardMaterials = new List<MaterialReward>();
            state = QuestState.Available;
        }

        /// <summary>
        /// Validate quest data according to rules
        /// </summary>
        public bool IsValid()
        {
            if (difficulty < Constants.MIN_DIFFICULTY || difficulty > Constants.MAX_DIFFICULTY)
            {
                Debug.LogError($"Quest {id}: Invalid difficulty {difficulty}");
                return false;
            }

            int totalStatRequirements = 0;
            foreach (var stat in requiredStats.Values)
            {
                totalStatRequirements += stat;
            }

            int expectedTotal = difficulty * Constants.STAT_POINTS_PER_DIFFICULTY;
            if (totalStatRequirements != expectedTotal)
            {
                Debug.LogWarning($"Quest {id}: Stat requirements ({totalStatRequirements}) don't match difficulty ({expectedTotal})");
                // Not a hard failure, just a warning
            }

            if (duration <= 0)
            {
                Debug.LogError($"Quest {id}: Invalid duration {duration}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Transition quest to a new state
        /// </summary>
        public bool TransitionTo(QuestState newState)
        {
            // Validate state transitions
            bool isValidTransition = (state, newState) switch
            {
                (QuestState.Available, QuestState.Assigned) => true,
                (QuestState.Assigned, QuestState.InProgress) => true,
                (QuestState.InProgress, QuestState.Completed) => true,
                (QuestState.InProgress, QuestState.Failed) => true,
                _ => false
            };

            if (isValidTransition)
            {
                state = newState;
                return true;
            }

            Debug.LogWarning($"Quest {id}: Invalid state transition from {state} to {newState}");
            return false;
        }

        /// <summary>
        /// Assign quest to a party
        /// </summary>
        public bool AssignToParty(string partyId)
        {
            if (state != QuestState.Available)
            {
                Debug.LogWarning($"Quest {id} is not available for assignment");
                return false;
            }

            assignedPartyId = partyId;
            return TransitionTo(QuestState.Assigned);
        }

        /// <summary>
        /// Start quest execution
        /// </summary>
        public bool StartQuest(int currentDay)
        {
            if (string.IsNullOrEmpty(assignedPartyId))
            {
                Debug.LogWarning($"Quest {id} has no assigned party");
                return false;
            }

            startDay = currentDay;
            return TransitionTo(QuestState.InProgress);
        }

        /// <summary>
        /// Calculate days remaining until quest completion
        /// </summary>
        public int GetDaysRemaining(int currentDay)
        {
            if (state != QuestState.InProgress) return -1;
            
            int elapsedDays = currentDay - startDay;
            return Mathf.Max(0, duration - elapsedDays);
        }

        /// <summary>
        /// Check if quest is ready to complete
        /// </summary>
        public bool IsReadyToComplete(int currentDay)
        {
            return state == QuestState.InProgress && GetDaysRemaining(currentDay) <= 0;
        }
    }

    /// <summary>
    /// Material reward structure
    /// </summary>
    [Serializable]
    public class MaterialReward
    {
        public string materialId;
        public int quantity;
        public float dropChance; // 0.0 to 1.0

        public MaterialReward(string id, int qty, float chance = 1.0f)
        {
            materialId = id;
            quantity = qty;
            dropChance = Mathf.Clamp01(chance);
        }
    }
}
