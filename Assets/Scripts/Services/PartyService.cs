using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Service for managing party recruitment, training, and equipment
    /// </summary>
    public class PartyService
    {
        private List<Party> parties;
        private GameManager gameManager;

        public PartyService()
        {
            parties = new List<Party>();
            gameManager = GameManager.Instance;
        }

        /// <summary>
        /// Constructor with explicit GameManager (for testing)
        /// </summary>
        public PartyService(GameManager manager)
        {
            parties = new List<Party>();
            gameManager = manager;
        }

        /// <summary>
        /// Recruit a new party
        /// </summary>
        /// <param name="partyName">Name of the party</param>
        /// <param name="cost">Recruitment cost</param>
        /// <returns>True if recruitment was successful</returns>
        public bool RecruitParty(string partyName, int cost)
        {
            // Check if we've reached max party capacity
            if (parties.Count >= Constants.MAX_PARTIES)
            {
                Debug.LogWarning($"Cannot recruit {partyName}: Maximum party capacity ({Constants.MAX_PARTIES}) reached");
                return false;
            }

            // Check if player has enough gold
            if (gameManager.PlayerGold < cost)
            {
                Debug.LogWarning($"Cannot recruit {partyName}: Insufficient funds (need {cost}, have {gameManager.PlayerGold})");
                return false;
            }

            // Deduct cost
            gameManager.ModifyGold(-cost);

            // Create new party
            Party newParty = new Party(partyName);
            parties.Add(newParty);

            // Publish event
            EventSystem.Instance.Publish(new PartyRecruitedEvent 
            { 
                PartyId = newParty.id, 
                PartyName = partyName,
                Cost = cost
            });

            Debug.Log($"Recruited party: {partyName} for {cost} gold");
            return true;
        }

        /// <summary>
        /// Train a party to improve a specific stat
        /// </summary>
        /// <param name="partyId">ID of the party to train</param>
        /// <param name="statType">Stat type to improve</param>
        /// <param name="cost">Training cost</param>
        /// <returns>True if training was successful</returns>
        public bool TrainParty(string partyId, StatType statType, int cost)
        {
            Party party = GetPartyById(partyId);
            if (party == null)
            {
                Debug.LogWarning($"Cannot train party: Party {partyId} not found");
                return false;
            }

            // Check if stat is already at max
            if (party.stats[statType] >= Constants.MAX_STAT_VALUE)
            {
                Debug.LogWarning($"Cannot train {party.name}: {statType} already at maximum ({Constants.MAX_STAT_VALUE})");
                return false;
            }

            // Check if player has enough gold
            if (gameManager.PlayerGold < cost)
            {
                Debug.LogWarning($"Cannot train {party.name}: Insufficient funds (need {cost}, have {gameManager.PlayerGold})");
                return false;
            }

            // Deduct cost
            gameManager.ModifyGold(-cost);

            // Improve stat based on cost (1 point per 100 gold)
            int statIncrease = Mathf.Max(1, cost / 100);
            int oldStatValue = party.stats[statType];
            party.stats[statType] = Mathf.Min(party.stats[statType] + statIncrease, Constants.MAX_STAT_VALUE);

            // Publish event
            EventSystem.Instance.Publish(new PartyTrainedEvent 
            { 
                PartyId = partyId, 
                StatType = statType,
                OldValue = oldStatValue,
                NewValue = party.stats[statType],
                Cost = cost
            });

            Debug.Log($"Trained {party.name}: {statType} improved from {oldStatValue} to {party.stats[statType]}");
            return true;
        }

        /// <summary>
        /// Purchase equipment for a party
        /// </summary>
        /// <param name="partyId">ID of the party</param>
        /// <param name="equipment">Equipment to purchase</param>
        /// <returns>True if purchase was successful</returns>
        public bool PurchaseEquipment(string partyId, Equipment equipment)
        {
            if (equipment == null)
            {
                Debug.LogWarning("Cannot purchase equipment: Equipment is null");
                return false;
            }

            Party party = GetPartyById(partyId);
            if (party == null)
            {
                Debug.LogWarning($"Cannot purchase equipment: Party {partyId} not found");
                return false;
            }

            // Check if player has enough gold
            if (gameManager.PlayerGold < equipment.cost)
            {
                Debug.LogWarning($"Cannot purchase {equipment.name}: Insufficient funds (need {equipment.cost}, have {gameManager.PlayerGold})");
                return false;
            }

            // Deduct cost
            gameManager.ModifyGold(-equipment.cost);

            // Add equipment to party
            party.AddEquipment(equipment);

            // Publish event
            EventSystem.Instance.Publish(new EquipmentPurchasedEvent 
            { 
                PartyId = partyId,
                EquipmentName = equipment.name,
                Cost = equipment.cost
            });

            Debug.Log($"Purchased {equipment.name} for {party.name} for {equipment.cost} gold");
            return true;
        }

        /// <summary>
        /// Modify party loyalty
        /// </summary>
        /// <param name="partyId">ID of the party</param>
        /// <param name="amount">Amount to modify (positive or negative)</param>
        /// <returns>True if modification was successful</returns>
        public bool ModifyPartyLoyalty(string partyId, int amount)
        {
            Party party = GetPartyById(partyId);
            if (party == null)
            {
                Debug.LogWarning($"Cannot modify loyalty: Party {partyId} not found");
                return false;
            }

            int oldLoyalty = party.loyalty;
            party.ModifyLoyalty(amount);

            // Publish event
            EventSystem.Instance.Publish(new PartyLoyaltyChangedEvent 
            { 
                PartyId = partyId,
                OldLoyalty = oldLoyalty,
                NewLoyalty = party.loyalty
            });

            Debug.Log($"{party.name} loyalty changed from {oldLoyalty} to {party.loyalty}");
            return true;
        }

        /// <summary>
        /// Get party by ID
        /// </summary>
        public Party GetPartyById(string partyId)
        {
            return parties.FirstOrDefault(p => p.id == partyId);
        }

        /// <summary>
        /// Get all parties
        /// </summary>
        public List<Party> GetAllParties()
        {
            return new List<Party>(parties);
        }

        /// <summary>
        /// Get only available parties
        /// </summary>
        public List<Party> GetAvailableParties()
        {
            return parties.Where(p => p.isAvailable).ToList();
        }

        /// <summary>
        /// Remove a party (e.g., when loyalty drops too low permanently)
        /// </summary>
        public bool RemoveParty(string partyId)
        {
            Party party = GetPartyById(partyId);
            if (party == null)
            {
                return false;
            }

            parties.Remove(party);

            EventSystem.Instance.Publish(new PartyRemovedEvent { PartyId = partyId });

            Debug.Log($"Removed party: {party.name}");
            return true;
        }

        /// <summary>
        /// Update all parties' availability based on loyalty
        /// </summary>
        public void UpdatePartyAvailability()
        {
            foreach (var party in parties)
            {
                party.UpdateAvailability();
            }
        }

        /// <summary>
        /// Clear all parties (for testing or game reset)
        /// </summary>
        public void ClearParties()
        {
            parties.Clear();
        }
    }
}
