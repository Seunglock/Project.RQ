using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Party entity representing adventurer groups
    /// </summary>
    [Serializable]
    public class Party
    {
        public string id;
        public string name;
        public Dictionary<StatType, int> stats;
        public int loyalty;
        public List<Equipment> equipment;
        public int experience;
        public List<string> specializations;
        public bool isAvailable;
        public DateTime lastQuestDate;

        public Party()
        {
            id = Guid.NewGuid().ToString();
            stats = new Dictionary<StatType, int>
            {
                { StatType.Exploration, Constants.MIN_STAT_VALUE },
                { StatType.Combat, Constants.MIN_STAT_VALUE },
                { StatType.Admin, Constants.MIN_STAT_VALUE }
            };
            equipment = new List<Equipment>();
            specializations = new List<string>();
            loyalty = 50; // Start at mid-level
            isAvailable = true;
            lastQuestDate = DateTime.MinValue;
        }

        public Party(string partyName) : this()
        {
            name = partyName;
        }

        /// <summary>
        /// Validate party data
        /// </summary>
        public bool IsValid()
        {
            // Check stat ranges
            foreach (var stat in stats)
            {
                if (stat.Value < Constants.MIN_STAT_VALUE || stat.Value > Constants.MAX_STAT_VALUE)
                {
                    Debug.LogError($"Party {name}: Invalid stat value {stat.Key}={stat.Value}");
                    return false;
                }
            }

            // Check loyalty range
            if (loyalty < Constants.MIN_LOYALTY || loyalty > Constants.MAX_LOYALTY)
            {
                Debug.LogError($"Party {name}: Invalid loyalty {loyalty}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get total stat value including equipment bonuses
        /// </summary>
        public int GetEffectiveStat(StatType statType)
        {
            int baseStat = stats.ContainsKey(statType) ? stats[statType] : 0;
            int equipmentBonus = 0;

            foreach (var eq in equipment)
            {
                if (eq.statBonuses.ContainsKey(statType))
                {
                    equipmentBonus += eq.statBonuses[statType];
                }
            }

            return Mathf.Clamp(baseStat + equipmentBonus, Constants.MIN_STAT_VALUE, Constants.MAX_STAT_VALUE);
        }

        /// <summary>
        /// Check if party meets quest requirements
        /// </summary>
        public bool MeetsRequirements(Dictionary<StatType, int> requiredStats)
        {
            foreach (var requirement in requiredStats)
            {
                if (GetEffectiveStat(requirement.Key) < requirement.Value)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculate success rate for a quest
        /// </summary>
        public float CalculateSuccessRate(Quest quest)
        {
            if (quest == null || quest.requiredStats == null) return 0f;

            float totalMatch = 0f;
            float totalRequired = 0f;

            foreach (var requirement in quest.requiredStats)
            {
                int effectiveStat = GetEffectiveStat(requirement.Key);
                totalMatch += Mathf.Min(effectiveStat, requirement.Value);
                totalRequired += requirement.Value;
            }

            if (totalRequired == 0) return 1f;

            float baseSuccessRate = totalMatch / totalRequired;
            
            // Apply loyalty modifier
            float loyaltyModifier = loyalty / 100f;
            float finalRate = baseSuccessRate * (0.5f + loyaltyModifier * 0.5f);

            return Mathf.Clamp01(finalRate);
        }

        /// <summary>
        /// Add equipment to party
        /// </summary>
        public void AddEquipment(Equipment eq)
        {
            if (eq != null)
            {
                equipment.Add(eq);
                EventSystem.Instance.Publish(new EquipmentAddedEvent { PartyId = id, EquipmentName = eq.name });
            }
        }

        /// <summary>
        /// Remove equipment from party
        /// </summary>
        public bool RemoveEquipment(string equipmentId)
        {
            int index = equipment.FindIndex(e => e.id == equipmentId);
            if (index >= 0)
            {
                equipment.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add experience and potentially improve stats
        /// </summary>
        public void AddExperience(int amount)
        {
            experience += amount;
            
            // Every 100 exp grants 1 stat point randomly
            int statPointsEarned = experience / 100;
            if (statPointsEarned > 0)
            {
                ImproveRandomStat(statPointsEarned);
                experience %= 100; // Reset to remainder
            }
        }

        /// <summary>
        /// Improve a random stat
        /// </summary>
        private void ImproveRandomStat(int points)
        {
            var statTypes = new List<StatType> { StatType.Exploration, StatType.Combat, StatType.Admin };
            
            for (int i = 0; i < points; i++)
            {
                StatType randomStat = statTypes[UnityEngine.Random.Range(0, statTypes.Count)];
                if (stats[randomStat] < Constants.MAX_STAT_VALUE)
                {
                    stats[randomStat]++;
                    EventSystem.Instance.Publish(new StatChangedEvent 
                    { 
                        EntityId = id, 
                        StatType = randomStat, 
                        NewValue = stats[randomStat] 
                    });
                }
            }
        }

        /// <summary>
        /// Modify loyalty
        /// </summary>
        public void ModifyLoyalty(int amount)
        {
            loyalty = Mathf.Clamp(loyalty + amount, Constants.MIN_LOYALTY, Constants.MAX_LOYALTY);
            
            // Check if party becomes unavailable
            if (loyalty < Constants.LOYALTY_UNAVAILABLE_THRESHOLD)
            {
                isAvailable = false;
            }
        }

        /// <summary>
        /// Update availability based on loyalty and time
        /// </summary>
        public void UpdateAvailability()
        {
            if (loyalty >= Constants.LOYALTY_UNAVAILABLE_THRESHOLD)
            {
                isAvailable = true;
            }
            else
            {
                isAvailable = false;
            }
        }
    }

    /// <summary>
    /// Equipment item providing stat bonuses
    /// </summary>
    [Serializable]
    public class Equipment
    {
        public string id;
        public string name;
        public Dictionary<StatType, int> statBonuses;
        public int cost;

        public Equipment()
        {
            id = Guid.NewGuid().ToString();
            statBonuses = new Dictionary<StatType, int>();
        }

        public Equipment(string itemName, int itemCost) : this()
        {
            name = itemName;
            cost = itemCost;
        }
    }

    // Equipment event
    public struct EquipmentAddedEvent { public string PartyId; public string EquipmentName; }
}
