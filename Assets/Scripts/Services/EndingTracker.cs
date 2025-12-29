using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GuildReceptionist;

/// <summary>
/// Ending type enumeration
/// </summary>
public enum EndingType
{
    DebtVictory,        // Paid off debt successfully
    DebtFailure,        // Failed to pay debt
    OrderEnding,        // High Order alignment ending
    ChaosEnding,        // High Chaos alignment ending
    RelationshipEnding, // High relationships with NPCs
    WealthEnding,       // Accumulated high wealth
    ReputationEnding,   // Achieved high reputation
    BalancedEnding,     // Balanced approach across all systems
    TrueEnding          // Special ending requiring all conditions
}

/// <summary>
/// Ending condition data
/// </summary>
[Serializable]
public class EndingCondition
{
    public EndingType EndingType;
    public bool DebtPaidOff;
    public bool HasHighOrder;
    public bool HasHighChaos;
    public bool HasHighRelationships;
    public bool HasHighWealth;
    public bool HasHighReputation;
    public int QuestsCompleted;
    public int PartiesRecruited;
    public int MaterialsCombined;

    public EndingCondition()
    {
        DebtPaidOff = false;
        HasHighOrder = false;
        HasHighChaos = false;
        HasHighRelationships = false;
        HasHighWealth = false;
        HasHighReputation = false;
        QuestsCompleted = 0;
        PartiesRecruited = 0;
        MaterialsCombined = 0;
    }
}

/// <summary>
/// Achievement tracking data
/// </summary>
[Serializable]
public class GameAchievement
{
    public string AchievementId;
    public string AchievementName;
    public string Description;
    public bool IsUnlocked;
    public DateTime UnlockedDate;

    public GameAchievement(string id, string name, string description)
    {
        AchievementId = id;
        AchievementName = name;
        Description = description;
        IsUnlocked = false;
    }
}

/// <summary>
/// Tracks player choices, achievements, and ending conditions
/// Used to determine which ending the player receives based on their gameplay
/// </summary>
public class EndingTracker
{
    // Achievement tracking
    private Dictionary<string, GameAchievement> achievements;
    
    // Ending condition tracking
    private EndingCondition currentCondition;
    
    // Player alignment tracking
    private int orderAlignment;
    private int chaosAlignment;
    
    // Relationship tracking
    private Dictionary<string, int> relationshipValues;
    
    // Gameplay statistics
    private int totalQuestsCompleted;
    private int totalPartiesRecruited;
    private int totalMaterialsCombined;
    private int totalGoldEarned;
    private int currentGold;
    private int currentReputation;
    private bool debtPaidOff;
    
    // Thresholds for ending conditions
    private const int HIGH_ALIGNMENT_THRESHOLD = 50;
    private const int HIGH_RELATIONSHIP_THRESHOLD = 70;
    private const int HIGH_WEALTH_THRESHOLD = 50000;
    private const int HIGH_REPUTATION_THRESHOLD = 100;
    private const int HIGH_QUEST_COUNT = 50;
    private const int HIGH_PARTY_COUNT = 10;
    private const int HIGH_MATERIAL_COUNT = 100;

    public EndingTracker()
    {
        achievements = new Dictionary<string, GameAchievement>();
        currentCondition = new EndingCondition();
        relationshipValues = new Dictionary<string, int>();
        orderAlignment = 0;
        chaosAlignment = 0;
        totalQuestsCompleted = 0;
        totalPartiesRecruited = 0;
        totalMaterialsCombined = 0;
        totalGoldEarned = 0;
        currentGold = 0;
        currentReputation = 0;
        debtPaidOff = false;

        InitializeAchievements();
    }

    /// <summary>
    /// Initialize all possible achievements
    /// </summary>
    private void InitializeAchievements()
    {
        achievements.Add("debt_paid", new GameAchievement(
            "debt_paid", "Debt Free", "Successfully paid off all debts"));
        
        achievements.Add("order_master", new GameAchievement(
            "order_master", "Order Master", "Achieved high Order alignment"));
        
        achievements.Add("chaos_master", new GameAchievement(
            "chaos_master", "Chaos Master", "Achieved high Chaos alignment"));
        
        achievements.Add("friend_to_all", new GameAchievement(
            "friend_to_all", "Friend to All", "Built high relationships with all NPCs"));
        
        achievements.Add("wealthy_tycoon", new GameAchievement(
            "wealthy_tycoon", "Wealthy Tycoon", "Accumulated massive wealth"));
        
        achievements.Add("legendary_reputation", new GameAchievement(
            "legendary_reputation", "Legendary", "Achieved legendary reputation"));
        
        achievements.Add("quest_master", new GameAchievement(
            "quest_master", "Quest Master", "Completed 50+ quests"));
        
        achievements.Add("party_leader", new GameAchievement(
            "party_leader", "Party Leader", "Recruited 10+ parties"));
        
        achievements.Add("master_synthesizer", new GameAchievement(
            "master_synthesizer", "Master Synthesizer", "Combined 100+ materials"));
        
        achievements.Add("true_ending", new GameAchievement(
            "true_ending", "True Ending", "Unlocked the true ending"));
    }

    /// <summary>
    /// Track quest completion
    /// </summary>
    public void OnQuestCompleted(Quest quest, bool success, int goldReward)
    {
        if (success)
        {
            totalQuestsCompleted++;
            totalGoldEarned += goldReward;
            currentGold += goldReward;
            
            UpdateAchievement("quest_master", totalQuestsCompleted >= HIGH_QUEST_COUNT);
            UpdateEndingCondition();
        }
    }

    /// <summary>
    /// Track party recruitment
    /// </summary>
    public void OnPartyRecruited(Party party)
    {
        totalPartiesRecruited++;
        UpdateAchievement("party_leader", totalPartiesRecruited >= HIGH_PARTY_COUNT);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track material combination
    /// </summary>
    public void OnMaterialCombined(string materialId, int quantity)
    {
        totalMaterialsCombined += quantity;
        UpdateAchievement("master_synthesizer", totalMaterialsCombined >= HIGH_MATERIAL_COUNT);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track debt payment status
    /// </summary>
    public void OnDebtPaidOff()
    {
        debtPaidOff = true;
        UpdateAchievement("debt_paid", true);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track gold changes
    /// </summary>
    public void OnGoldChanged(int newGoldAmount)
    {
        currentGold = newGoldAmount;
        UpdateAchievement("wealthy_tycoon", currentGold >= HIGH_WEALTH_THRESHOLD);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track reputation changes
    /// </summary>
    public void OnReputationChanged(int newReputation)
    {
        currentReputation = newReputation;
        UpdateAchievement("legendary_reputation", currentReputation >= HIGH_REPUTATION_THRESHOLD);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track alignment changes
    /// </summary>
    public void OnAlignmentChanged(AlignmentFlags alignment, int delta)
    {
        if (alignment == AlignmentFlags.Order)
        {
            orderAlignment += delta;
            UpdateAchievement("order_master", orderAlignment >= HIGH_ALIGNMENT_THRESHOLD);
        }
        else if (alignment == AlignmentFlags.Chaos)
        {
            chaosAlignment += delta;
            UpdateAchievement("chaos_master", chaosAlignment >= HIGH_ALIGNMENT_THRESHOLD);
        }
        
        UpdateEndingCondition();
    }

    /// <summary>
    /// Track relationship changes
    /// </summary>
    public void OnRelationshipChanged(string characterId, int newValue)
    {
        relationshipValues[characterId] = newValue;
        
        // Check if all relationships are high
        bool allHighRelationships = relationshipValues.Values.All(v => v >= HIGH_RELATIONSHIP_THRESHOLD);
        UpdateAchievement("friend_to_all", allHighRelationships);
        UpdateEndingCondition();
    }

    /// <summary>
    /// Update achievement unlock status
    /// </summary>
    private void UpdateAchievement(string achievementId, bool condition)
    {
        if (achievements.ContainsKey(achievementId) && !achievements[achievementId].IsUnlocked && condition)
        {
            achievements[achievementId].IsUnlocked = true;
            achievements[achievementId].UnlockedDate = DateTime.Now;
            Debug.Log($"Achievement Unlocked: {achievements[achievementId].AchievementName}");
            
            // Trigger event for achievement unlock
            if (EventSystem.Instance != null)
            {
                // Note: EventSystem uses Publish<T> with struct events
                Debug.Log($"Achievement event triggered: {achievementId}");
            }
        }
    }

    /// <summary>
    /// Update ending condition based on current game state
    /// </summary>
    private void UpdateEndingCondition()
    {
        currentCondition.DebtPaidOff = debtPaidOff;
        currentCondition.HasHighOrder = orderAlignment >= HIGH_ALIGNMENT_THRESHOLD;
        currentCondition.HasHighChaos = chaosAlignment >= HIGH_ALIGNMENT_THRESHOLD;
        currentCondition.HasHighRelationships = relationshipValues.Values.Any(v => v >= HIGH_RELATIONSHIP_THRESHOLD);
        currentCondition.HasHighWealth = currentGold >= HIGH_WEALTH_THRESHOLD;
        currentCondition.HasHighReputation = currentReputation >= HIGH_REPUTATION_THRESHOLD;
        currentCondition.QuestsCompleted = totalQuestsCompleted;
        currentCondition.PartiesRecruited = totalPartiesRecruited;
        currentCondition.MaterialsCombined = totalMaterialsCombined;
    }

    /// <summary>
    /// Determine which ending the player has earned
    /// </summary>
    public EndingType DetermineEnding()
    {
        UpdateEndingCondition();

        // True Ending: Requires all major achievements
        if (currentCondition.DebtPaidOff &&
            currentCondition.HasHighReputation &&
            currentCondition.HasHighWealth &&
            currentCondition.HasHighRelationships &&
            (currentCondition.HasHighOrder || currentCondition.HasHighChaos))
        {
            UpdateAchievement("true_ending", true);
            currentCondition.EndingType = EndingType.TrueEnding;
            return EndingType.TrueEnding;
        }

        // Debt Failure: Failed to pay debt
        if (!currentCondition.DebtPaidOff)
        {
            currentCondition.EndingType = EndingType.DebtFailure;
            return EndingType.DebtFailure;
        }

        // Order Ending: High Order alignment
        if (currentCondition.HasHighOrder && orderAlignment > chaosAlignment)
        {
            currentCondition.EndingType = EndingType.OrderEnding;
            return EndingType.OrderEnding;
        }

        // Chaos Ending: High Chaos alignment
        if (currentCondition.HasHighChaos && chaosAlignment > orderAlignment)
        {
            currentCondition.EndingType = EndingType.ChaosEnding;
            return EndingType.ChaosEnding;
        }

        // Relationship Ending: High relationships
        if (currentCondition.HasHighRelationships)
        {
            currentCondition.EndingType = EndingType.RelationshipEnding;
            return EndingType.RelationshipEnding;
        }

        // Wealth Ending: High wealth
        if (currentCondition.HasHighWealth)
        {
            currentCondition.EndingType = EndingType.WealthEnding;
            return EndingType.WealthEnding;
        }

        // Reputation Ending: High reputation
        if (currentCondition.HasHighReputation)
        {
            currentCondition.EndingType = EndingType.ReputationEnding;
            return EndingType.ReputationEnding;
        }

        // Balanced Ending: Paid debt but no dominant path
        if (currentCondition.DebtPaidOff)
        {
            currentCondition.EndingType = EndingType.BalancedEnding;
            return EndingType.BalancedEnding;
        }

        // Default: Debt Victory (basic win condition)
        currentCondition.EndingType = EndingType.DebtVictory;
        return EndingType.DebtVictory;
    }

    /// <summary>
    /// Get ending description based on type
    /// </summary>
    public string GetEndingDescription(EndingType endingType)
    {
        switch (endingType)
        {
            case EndingType.TrueEnding:
                return "Congratulations! You've achieved the True Ending by mastering all aspects of guild management. " +
                       "You paid off your debt, built legendary reputation, accumulated great wealth, and forged strong bonds with all characters.";
            
            case EndingType.DebtVictory:
                return "Success! You've paid off your family's debt and secured financial freedom. " +
                       "Though your journey was focused, you've proven yourself as a capable guild receptionist.";
            
            case EndingType.DebtFailure:
                return "Game Over. Unable to meet the quarterly debt payment, you've lost the guild and failed your family. " +
                       "Perhaps next time, better quest management and financial planning will lead to success.";
            
            case EndingType.OrderEnding:
                return "The Order Ending: Through lawful conduct and structured management, you've brought order to the guild. " +
                       "Your reputation as a fair and principled receptionist is known throughout the land.";
            
            case EndingType.ChaosEnding:
                return "The Chaos Ending: By embracing unconventional methods and risk-taking, you've revolutionized guild operations. " +
                       "Your unpredictable approach has become legendary among adventurers.";
            
            case EndingType.RelationshipEnding:
                return "The Friendship Ending: Through kindness and empathy, you've built deep connections with every party and NPC. " +
                       "The guild has become a place of camaraderie and trust, and everyone considers you family.";
            
            case EndingType.WealthEnding:
                return "The Wealth Ending: Your business acumen and material trading expertise have made you incredibly wealthy. " +
                       "Not only is the debt paid, but you've built a fortune that will last generations.";
            
            case EndingType.ReputationEnding:
                return "The Legend Ending: Your legendary reputation precedes you. The guild is now the most prestigious in the realm, " +
                       "and every adventurer dreams of working under your management.";
            
            case EndingType.BalancedEnding:
                return "The Balanced Ending: You've succeeded through steady, balanced management. " +
                       "While not exceptional in any single area, your well-rounded approach has proven effective.";
            
            default:
                return "Ending reached. Thank you for playing!";
        }
    }

    /// <summary>
    /// Get all unlocked achievements
    /// </summary>
    public List<GameAchievement> GetUnlockedAchievements()
    {
        return achievements.Values.Where(a => a.IsUnlocked).ToList();
    }

    /// <summary>
    /// Get all achievements (unlocked and locked)
    /// </summary>
    public List<GameAchievement> GetAllAchievements()
    {
        return achievements.Values.ToList();
    }

    /// <summary>
    /// Get current ending condition
    /// </summary>
    public EndingCondition GetCurrentCondition()
    {
        UpdateEndingCondition();
        return currentCondition;
    }

    /// <summary>
    /// Get gameplay statistics summary
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            { "QuestsCompleted", totalQuestsCompleted },
            { "PartiesRecruited", totalPartiesRecruited },
            { "MaterialsCombined", totalMaterialsCombined },
            { "TotalGoldEarned", totalGoldEarned },
            { "CurrentGold", currentGold },
            { "CurrentReputation", currentReputation },
            { "OrderAlignment", orderAlignment },
            { "ChaosAlignment", chaosAlignment },
            { "DebtPaidOff", debtPaidOff },
            { "AchievementsUnlocked", GetUnlockedAchievements().Count },
            { "TotalAchievements", achievements.Count }
        };
    }

    /// <summary>
    /// Reset all tracking (for new game)
    /// </summary>
    public void Reset()
    {
        achievements.Clear();
        InitializeAchievements();
        currentCondition = new EndingCondition();
        relationshipValues.Clear();
        orderAlignment = 0;
        chaosAlignment = 0;
        totalQuestsCompleted = 0;
        totalPartiesRecruited = 0;
        totalMaterialsCombined = 0;
        totalGoldEarned = 0;
        currentGold = 0;
        currentReputation = 0;
        debtPaidOff = false;
    }
}
