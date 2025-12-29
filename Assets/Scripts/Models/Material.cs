using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Material entity representing tradable items
    /// </summary>
    [Serializable]
    public class Material
    {
        public string id;
        public string name;
        public MaterialRarity rarity;
        public int baseValue;
        public int currentValue;
        public int quantity;
        public Dictionary<string, int> combinationRecipe;
        public string category;

        public Material()
        {
            id = Guid.NewGuid().ToString();
            combinationRecipe = new Dictionary<string, int>();
        }

        public Material(string materialName, MaterialRarity materialRarity, int value) : this()
        {
            name = materialName;
            rarity = materialRarity;
            baseValue = value;
            currentValue = value;
            quantity = 0;
        }

        /// <summary>
        /// Validate material data
        /// </summary>
        public bool IsValid()
        {
            if (quantity < 0)
            {
                Debug.LogError($"Material {name}: Invalid quantity {quantity}");
                return false;
            }

            if (baseValue < 0 || currentValue < 0)
            {
                Debug.LogError($"Material {name}: Invalid value");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add quantity
        /// </summary>
        public void AddQuantity(int amount)
        {
            quantity += amount;
            EventSystem.Instance.Publish(new MaterialQuantityChangedEvent 
            { 
                MaterialId = id, 
                Change = amount, 
                NewQuantity = quantity 
            });
        }

        /// <summary>
        /// Remove quantity
        /// </summary>
        public bool RemoveQuantity(int amount)
        {
            if (quantity < amount) return false;
            
            quantity -= amount;
            EventSystem.Instance.Publish(new MaterialQuantityChangedEvent 
            { 
                MaterialId = id, 
                Change = -amount, 
                NewQuantity = quantity 
            });
            return true;
        }

        /// <summary>
        /// Update market value with fluctuation
        /// </summary>
        public void UpdateMarketValue(float fluctuationPercent)
        {
            currentValue = Mathf.RoundToInt(baseValue * (1f + fluctuationPercent));
            currentValue = Mathf.Max(1, currentValue); // Minimum value of 1
        }
    }

    /// <summary>
    /// Character entity representing NPCs and player
    /// </summary>
    [Serializable]
    public class Character
    {
        public string id;
        public string name;
        public CharacterType type;
        public Dictionary<StatType, int> stats;
        public AlignmentFlags alignment;
        public Dictionary<string, int> relationships;
        public bool isPlayer;

        public Character()
        {
            id = Guid.NewGuid().ToString();
            stats = new Dictionary<StatType, int>();
            relationships = new Dictionary<string, int>();
        }

        public Character(string characterName, CharacterType characterType) : this()
        {
            name = characterName;
            type = characterType;
            isPlayer = (type == CharacterType.Player);
            alignment = AlignmentFlags.Neutral;
        }

        /// <summary>
        /// Validate character data
        /// </summary>
        public bool IsValid()
        {
            // Check stat ranges
            foreach (var stat in stats)
            {
                if (stat.Value < Constants.MIN_STAT_VALUE || stat.Value > Constants.MAX_STAT_VALUE)
                {
                    Debug.LogError($"Character {name}: Invalid stat value {stat.Key}={stat.Value}");
                    return false;
                }
            }

            // Check relationship ranges
            foreach (var rel in relationships)
            {
                if (rel.Value < Constants.MIN_RELATIONSHIP || rel.Value > Constants.MAX_RELATIONSHIP)
                {
                    Debug.LogError($"Character {name}: Invalid relationship value with {rel.Key}={rel.Value}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get relationship value with another character
        /// </summary>
        public int GetRelationship(string characterId)
        {
            return relationships.ContainsKey(characterId) ? relationships[characterId] : 0;
        }

        /// <summary>
        /// Modify relationship with another character
        /// </summary>
        public void ModifyRelationship(string characterId, int change)
        {
            if (!relationships.ContainsKey(characterId))
            {
                relationships[characterId] = 0;
            }

            relationships[characterId] = Mathf.Clamp(
                relationships[characterId] + change,
                Constants.MIN_RELATIONSHIP,
                Constants.MAX_RELATIONSHIP
            );

            EventSystem.Instance.Publish(new RelationshipChangedEvent 
            { 
                CharacterId = characterId, 
                NewValue = relationships[characterId] 
            });
        }

        /// <summary>
        /// Set alignment
        /// </summary>
        public void SetAlignment(AlignmentFlags newAlignment)
        {
            alignment = newAlignment;
        }
    }

    /// <summary>
    /// Debt entity representing financial obligations
    /// </summary>
    [Serializable]
    public class Debt
    {
        public int currentBalance;
        public int quarterlyPayment;
        public float interestRate;
        public DateTime dueDate;
        public List<PaymentRecord> paymentHistory;
        public DebtState state;

        public Debt()
        {
            paymentHistory = new List<PaymentRecord>();
            state = DebtState.Active;
        }

        public Debt(int initialBalance, int payment, float rate) : this()
        {
            currentBalance = initialBalance;
            quarterlyPayment = payment;
            interestRate = rate;
            dueDate = DateTime.Now.AddDays(Constants.DAYS_PER_QUARTER);
        }

        /// <summary>
        /// Validate debt data
        /// </summary>
        public bool IsValid()
        {
            if (currentBalance < 0)
            {
                Debug.LogError($"Debt: Invalid balance {currentBalance}");
                return false;
            }

            if (interestRate <= 0)
            {
                Debug.LogError($"Debt: Invalid interest rate {interestRate}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Make a payment
        /// </summary>
        public bool MakePayment(int amount)
        {
            if (amount <= 0) return false;
            if (amount > currentBalance) amount = currentBalance;

            currentBalance -= amount;
            paymentHistory.Add(new PaymentRecord 
            { 
                amount = amount, 
                date = DateTime.Now, 
                remainingBalance = currentBalance 
            });

            if (currentBalance == 0)
            {
                state = DebtState.Paid;
            }

            EventSystem.Instance.Publish(new DebtPaymentEvent 
            { 
                Amount = amount, 
                RemainingBalance = currentBalance 
            });

            return true;
        }

        /// <summary>
        /// Check if payment is due
        /// </summary>
        public bool IsPaymentDue(int currentDay)
        {
            // Payment due every quarter (90 days)
            return currentDay % Constants.DAYS_PER_QUARTER == 0;
        }

        /// <summary>
        /// Process quarterly payment requirement
        /// </summary>
        public bool ProcessQuarterlyPayment(int currentDay)
        {
            if (!IsPaymentDue(currentDay)) return true;

            if (currentBalance < quarterlyPayment)
            {
                // Failed to make payment - game over condition
                state = DebtState.Overdue;
                EventSystem.Instance.Publish(new GameOverEvent { Reason = "Failed to make quarterly debt payment" });
                return false;
            }

            return MakePayment(quarterlyPayment);
        }

        /// <summary>
        /// Apply interest to balance
        /// </summary>
        public void ApplyInterest()
        {
            int interestAmount = Mathf.RoundToInt(currentBalance * interestRate / 4f); // Quarterly rate
            currentBalance += interestAmount;
        }
    }

    /// <summary>
    /// Payment record for debt tracking
    /// </summary>
    [Serializable]
    public class PaymentRecord
    {
        public int amount;
        public DateTime date;
        public int remainingBalance;
    }

    // Material event
    public struct MaterialQuantityChangedEvent { public string MaterialId; public int Change; public int NewQuantity; }
}
