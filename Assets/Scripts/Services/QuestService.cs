using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Service managing quest operations: creation, assignment, execution, and completion
    /// </summary>
    public class QuestService
    {
        private Dictionary<string, Quest> _quests;
        private Dictionary<string, Party> _assignedParties; // Track parties by quest ID

        public QuestService()
        {
            _quests = new Dictionary<string, Quest>();
            _assignedParties = new Dictionary<string, Party>();
        }

        #region Quest Management

        /// <summary>
        /// Add a new quest to the available pool
        /// </summary>
        public bool AddQuest(Quest quest)
        {
            if (quest == null)
            {
                Debug.LogError("Cannot add null quest");
                return false;
            }

            if (!quest.IsValid())
            {
                Debug.LogError($"Quest {quest.id} failed validation");
                return false;
            }

            if (_quests.ContainsKey(quest.id))
            {
                Debug.LogWarning($"Quest {quest.id} already exists");
                return false;
            }

            _quests[quest.id] = quest;
            EventSystem.Instance.Publish(new QuestAddedEvent { QuestId = quest.id });
            return true;
        }

        /// <summary>
        /// Get quest by ID
        /// </summary>
        public Quest GetQuestById(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            return _quests.ContainsKey(questId) ? _quests[questId] : null;
        }

        /// <summary>
        /// Remove quest from the system
        /// </summary>
        public bool RemoveQuest(string questId)
        {
            if (_quests.ContainsKey(questId))
            {
                Quest quest = _quests[questId];
                if (quest.state == QuestState.InProgress)
                {
                    Debug.LogWarning($"Cannot remove quest {questId} while in progress");
                    return false;
                }

                _quests.Remove(questId);
                EventSystem.Instance.Publish(new QuestRemovedEvent { QuestId = questId });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all quests (for testing)
        /// </summary>
        public void ClearAllQuests()
        {
            _quests.Clear();
        }

        #endregion

        #region Quest Filtering

        /// <summary>
        /// Get all available quests
        /// </summary>
        public List<Quest> GetAvailableQuests()
        {
            return _quests.Values
                .Where(q => q.state == QuestState.Available)
                .ToList();
        }

        /// <summary>
        /// Get all assigned quests
        /// </summary>
        public List<Quest> GetAssignedQuests()
        {
            return _quests.Values
                .Where(q => q.state == QuestState.Assigned)
                .ToList();
        }

        /// <summary>
        /// Get all in-progress quests
        /// </summary>
        public List<Quest> GetInProgressQuests()
        {
            return _quests.Values
                .Where(q => q.state == QuestState.InProgress)
                .ToList();
        }

        /// <summary>
        /// Get quests by type
        /// </summary>
        public List<Quest> GetQuestsByType(QuestType type)
        {
            return _quests.Values
                .Where(q => q.type == type)
                .ToList();
        }

        /// <summary>
        /// Get quests by difficulty range
        /// </summary>
        public List<Quest> GetQuestsByDifficulty(int minDifficulty, int maxDifficulty)
        {
            return _quests.Values
                .Where(q => q.difficulty >= minDifficulty && q.difficulty <= maxDifficulty)
                .ToList();
        }

        #endregion

        #region Quest Assignment

        /// <summary>
        /// Assign quest to a party
        /// </summary>
        public bool AssignQuest(string questId, Party party)
        {
            if (string.IsNullOrEmpty(questId))
            {
                Debug.LogError("Quest ID cannot be null or empty");
                return false;
            }

            if (party == null)
            {
                Debug.LogError("Party cannot be null");
                return false;
            }

            Quest quest = GetQuestById(questId);
            if (quest == null)
            {
                Debug.LogError($"Quest {questId} not found");
                return false;
            }

            if (quest.state != QuestState.Available)
            {
                Debug.LogWarning($"Quest {questId} is not available (current state: {quest.state})");
                return false;
            }

            if (!party.isAvailable)
            {
                Debug.LogWarning($"Party {party.name} is not available");
                return false;
            }

            // Assign quest
            if (quest.AssignToParty(party.id))
            {
                party.isAvailable = false;
                party.lastQuestDate = DateTime.Now;

                // Track the party for this quest
                _assignedParties[quest.id] = party;

                EventSystem.Instance.Publish(new QuestAssignedEvent
                {
                    QuestId = quest.id,
                    PartyId = party.id,
                    EstimatedSuccessRate = CalculateSuccessRate(quest, party)
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unassign quest from party
        /// </summary>
        public bool UnassignQuest(string questId)
        {
            Quest quest = GetQuestById(questId);
            if (quest == null)
            {
                return false;
            }

            if (quest.state != QuestState.Assigned)
            {
                Debug.LogWarning($"Quest {questId} cannot be unassigned (current state: {quest.state})");
                return false;
            }

            string previousPartyId = quest.assignedPartyId;
            quest.assignedPartyId = null;
            quest.state = QuestState.Available;

            EventSystem.Instance.Publish(new QuestUnassignedEvent 
            { 
                QuestId = quest.id, 
                PartyId = previousPartyId 
            });

            return true;
        }

        #endregion

        #region Quest Execution

        /// <summary>
        /// Start an assigned quest
        /// </summary>
        public bool StartQuest(string questId, int currentDay)
        {
            Quest quest = GetQuestById(questId);
            if (quest == null)
            {
                return false;
            }

            if (quest.StartQuest(currentDay))
            {
                EventSystem.Instance.Publish(new QuestStartedEvent 
                { 
                    QuestId = quest.id, 
                    StartDay = currentDay,
                    ExpectedCompletionDay = currentDay + quest.duration
                });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculate success rate for a party on a quest
        /// </summary>
        public float CalculateSuccessRate(Quest quest, Party party)
        {
            if (quest == null || party == null)
            {
                return 0f;
            }

            // Use party's built-in calculation method
            return party.CalculateSuccessRate(quest);
        }

        /// <summary>
        /// Complete a quest (success or failure)
        /// </summary>
        public bool CompleteQuest(string questId, int currentDay, bool isSuccess)
        {
            Quest quest = GetQuestById(questId);
            if (quest == null)
            {
                return false;
            }

            if (quest.state != QuestState.InProgress)
            {
                Debug.LogWarning($"Quest {questId} is not in progress");
                return false;
            }

            // Get the assigned party from tracking dictionary
            Party assignedParty = null;
            if (_assignedParties.ContainsKey(questId))
            {
                assignedParty = _assignedParties[questId];
            }

            // Transition to appropriate final state
            QuestState finalState = isSuccess ? QuestState.Completed : QuestState.Failed;
            if (!quest.TransitionTo(finalState))
            {
                return false;
            }

            // Mark party as available again and remove from tracking
            if (assignedParty != null)
            {
                assignedParty.isAvailable = true;
                _assignedParties.Remove(questId);
            }

            // Publish completion event
            EventSystem.Instance.Publish(new QuestCompletedEvent
            {
                QuestId = quest.id,
                PartyId = quest.assignedPartyId,
                IsSuccess = isSuccess,
                CompletionDay = currentDay,
                GoldReward = isSuccess ? quest.rewardGold : 0,
                ReputationChange = isSuccess ? quest.reputationImpact : -quest.reputationImpact / 2,
                MaterialRewards = isSuccess ? quest.rewardMaterials : new List<MaterialReward>()
            });

            return true;
        }

        /// <summary>
        /// Process quest rewards and consequences
        /// </summary>
        public QuestRewards ProcessQuestRewards(Quest quest, bool isSuccess)
        {
            if (quest == null)
            {
                return null;
            }

            var rewards = new QuestRewards();

            if (isSuccess)
            {
                // Award gold
                rewards.Gold = quest.rewardGold;

                // Process material rewards with drop chances
                foreach (var materialReward in quest.rewardMaterials)
                {
                    float roll = UnityEngine.Random.value;
                    if (roll <= materialReward.dropChance)
                    {
                        rewards.Materials.Add(new MaterialDrop
                        {
                            MaterialId = materialReward.materialId,
                            Quantity = materialReward.quantity
                        });
                    }
                }

                // Award reputation
                rewards.Reputation = quest.reputationImpact;
            }
            else
            {
                // Penalty for failure
                rewards.Gold = 0;
                rewards.Materials = new List<MaterialDrop>();
                rewards.Reputation = -quest.reputationImpact / 2; // Half penalty
            }

            return rewards;
        }

        #endregion

        #region Quest Progression

        /// <summary>
        /// Update all in-progress quests (called once per day/turn)
        /// </summary>
        public void UpdateQuests(int currentDay)
        {
            var inProgressQuests = GetInProgressQuests();

            foreach (var quest in inProgressQuests)
            {
                if (quest.IsReadyToComplete(currentDay))
                {
                    EventSystem.Instance.Publish(new QuestReadyEvent 
                    { 
                        QuestId = quest.id,
                        CurrentDay = currentDay 
                    });
                }
            }
        }

        /// <summary>
        /// Get quests ready for completion
        /// </summary>
        public List<Quest> GetReadyQuests(int currentDay)
        {
            return _quests.Values
                .Where(q => q.state == QuestState.InProgress && q.IsReadyToComplete(currentDay))
                .ToList();
        }

        #endregion

        #region Quest Generation

        /// <summary>
        /// Generate a random quest based on difficulty
        /// </summary>
        public Quest GenerateQuest(int difficulty, QuestType type)
        {
            difficulty = Mathf.Clamp(difficulty, Constants.MIN_DIFFICULTY, Constants.MAX_DIFFICULTY);

            var quest = new Quest
            {
                type = type,
                difficulty = difficulty,
                duration = UnityEngine.Random.Range(Constants.MIN_QUEST_DURATION, Constants.MAX_QUEST_DURATION + 1),
                rewardGold = difficulty * Constants.GOLD_PER_DIFFICULTY * UnityEngine.Random.Range(80, 121) / 100,
                reputationImpact = difficulty * 5,
                successRate = 0.5f + (difficulty * 0.05f)
            };

            // Distribute stat requirements based on quest type
            int totalStatPoints = difficulty * Constants.STAT_POINTS_PER_DIFFICULTY;
            
            switch (type)
            {
                case QuestType.Combat:
                    quest.requiredStats[StatType.Combat] = (int)(totalStatPoints * 0.7f);
                    quest.requiredStats[StatType.Exploration] = (int)(totalStatPoints * 0.3f);
                    break;
                case QuestType.Exploration:
                    quest.requiredStats[StatType.Exploration] = (int)(totalStatPoints * 0.7f);
                    quest.requiredStats[StatType.Combat] = (int)(totalStatPoints * 0.3f);
                    break;
                case QuestType.Admin:
                    quest.requiredStats[StatType.Admin] = (int)(totalStatPoints * 0.7f);
                    quest.requiredStats[StatType.Exploration] = (int)(totalStatPoints * 0.3f);
                    break;
            }

            // Add material rewards based on difficulty
            int numMaterials = difficulty / 2 + 1;
            for (int i = 0; i < numMaterials; i++)
            {
                quest.rewardMaterials.Add(new MaterialReward(
                    $"material-{Guid.NewGuid().ToString().Substring(0, 8)}",
                    UnityEngine.Random.Range(1, difficulty + 2),
                    UnityEngine.Random.Range(0.3f, 0.9f)
                ));
            }

            return quest;
        }

        #endregion
    }

    #region Quest Rewards

    /// <summary>
    /// Rewards granted from quest completion
    /// </summary>
    public class QuestRewards
    {
        public int Gold { get; set; }
        public int Reputation { get; set; }
        public List<MaterialDrop> Materials { get; set; }

        public QuestRewards()
        {
            Materials = new List<MaterialDrop>();
        }
    }

    /// <summary>
    /// Material drop from quest
    /// </summary>
    public class MaterialDrop
    {
        public string MaterialId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}
