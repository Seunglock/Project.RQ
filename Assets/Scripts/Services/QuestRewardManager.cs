using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Manages quest reward processing and integration with game systems
    /// Listens to quest completion events and applies rewards to player
    /// </summary>
    public class QuestRewardManager : MonoBehaviour
    {
        private static QuestRewardManager _instance;
        public static QuestRewardManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("QuestRewardManager");
                    _instance = go.AddComponent<QuestRewardManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Subscribe to quest completion events
            EventSystem.Instance.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventSystem.Instance.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        }

        /// <summary>
        /// Handle quest completion and apply rewards
        /// </summary>
        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (evt.IsSuccess)
            {
                Debug.Log($"Quest {evt.QuestId} completed successfully by party {evt.PartyId}");

                // Apply gold reward
                if (evt.GoldReward > 0)
                {
                    GameManager.Instance.ModifyGold(evt.GoldReward);
                    Debug.Log($"Awarded {evt.GoldReward} gold");
                }

                // Apply reputation change
                if (evt.ReputationChange != 0)
                {
                    GameManager.Instance.ModifyReputation(evt.ReputationChange);
                    Debug.Log($"Reputation changed by {evt.ReputationChange}");
                }

                // Process material rewards
                if (evt.MaterialRewards != null && evt.MaterialRewards.Count > 0)
                {
                    foreach (var materialReward in evt.MaterialRewards)
                    {
                        // Roll for drop chance
                        float roll = Random.value;
                        if (roll <= materialReward.dropChance)
                        {
                            Debug.Log($"Material dropped: {materialReward.materialId} x{materialReward.quantity}");

                            // TODO: Add to player's material inventory
                            // This will be implemented in Phase 6 (User Story 4)
                            EventSystem.Instance.Publish(new MaterialAcquiredEvent
                            {
                                MaterialId = materialReward.materialId,
                                Quantity = materialReward.quantity
                            });
                        }
                        else
                        {
                            Debug.Log($"Material {materialReward.materialId} did not drop (roll: {roll}, chance: {materialReward.dropChance})");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"Quest {evt.QuestId} failed by party {evt.PartyId}");

                // Apply reputation penalty
                if (evt.ReputationChange != 0)
                {
                    GameManager.Instance.ModifyReputation(evt.ReputationChange);
                    Debug.Log($"Reputation penalty: {evt.ReputationChange}");
                }
            }

            // Publish reward processed event for UI updates
            EventSystem.Instance.Publish(new QuestRewardsProcessedEvent
            {
                QuestId = evt.QuestId,
                IsSuccess = evt.IsSuccess,
                GoldAwarded = evt.GoldReward,
                ReputationChange = evt.ReputationChange,
                MaterialCount = evt.MaterialRewards?.Count ?? 0
            });
        }
    }

    #region Quest Reward Events

    /// <summary>
    /// Event fired when quest rewards have been processed and applied
    /// </summary>
    public struct QuestRewardsProcessedEvent
    {
        public string QuestId;
        public bool IsSuccess;
        public int GoldAwarded;
        public int ReputationChange;
        public int MaterialCount;
    }

    /// <summary>
    /// Event fired when a material is acquired
    /// </summary>
    public struct MaterialAcquiredEvent
    {
        public string MaterialId;
        public int Quantity;
    }

    #endregion
}
