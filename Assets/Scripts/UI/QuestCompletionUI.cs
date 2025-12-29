using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for quest completion and reward display
    /// Handles completion notification, reward display, and party feedback
    /// </summary>
    public class QuestCompletionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionTitleText;
        [SerializeField] private TMP_Text questInfoText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private GameObject rewardsPanel;
        [SerializeField] private TMP_Text goldRewardText;
        [SerializeField] private TMP_Text reputationRewardText;
        [SerializeField] private Transform materialRewardsContainer;
        [SerializeField] private GameObject materialRewardItemPrefab;
        [SerializeField] private Button continueButton;

        [Header("Success/Failure Visuals")]
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color failureColor = Color.red;
        [SerializeField] private Image resultBackgroundImage;

        // Internal state
        private QuestService _questService;

        // Events
        public System.Action OnCompletionAcknowledged;

        private void Awake()
        {
            _questService = new QuestService();

            // Setup UI callbacks
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
            }

            // Hide panel initially
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }
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

        #region Public Methods

        /// <summary>
        /// Initialize the UI with a quest service instance
        /// </summary>
        public void Initialize(QuestService questService)
        {
            _questService = questService ?? new QuestService();
        }

        /// <summary>
        /// Show quest completion UI with rewards
        /// </summary>
        public void ShowCompletionUI(Quest quest, bool isSuccess, QuestRewards rewards)
        {
            if (quest == null)
            {
                Debug.LogError("Cannot show completion UI with null quest");
                return;
            }

            // Set title
            if (completionTitleText != null)
            {
                completionTitleText.text = isSuccess ? "Quest Completed!" : "Quest Failed";
                completionTitleText.color = isSuccess ? successColor : failureColor;
            }

            // Set background color
            if (resultBackgroundImage != null)
            {
                resultBackgroundImage.color = isSuccess ?
                    new Color(successColor.r, successColor.g, successColor.b, 0.3f) :
                    new Color(failureColor.r, failureColor.g, failureColor.b, 0.3f);
            }

            // Show quest info
            ShowQuestInfo(quest);

            // Show result message
            ShowResultMessage(isSuccess);

            // Show rewards
            ShowRewards(rewards, isSuccess);

            // Show panel
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide completion UI
        /// </summary>
        public void HideCompletionUI()
        {
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }

            ClearMaterialRewards();
        }

        #endregion

        #region Private Methods

        private void ShowQuestInfo(Quest quest)
        {
            if (questInfoText != null)
            {
                string info = $"<b>Quest:</b> {quest.id.Substring(0, Mathf.Min(8, quest.id.Length))}\n";
                info += $"<b>Type:</b> {quest.type}\n";
                info += $"<b>Difficulty:</b> {quest.difficulty}/5\n";
                info += $"<b>Duration:</b> {quest.duration} days";

                questInfoText.text = info;
            }
        }

        private void ShowResultMessage(bool isSuccess)
        {
            if (resultText != null)
            {
                if (isSuccess)
                {
                    resultText.text = "The party successfully completed the quest and returned with rewards!";
                }
                else
                {
                    resultText.text = "The party was unable to complete the quest and returned empty-handed.";
                }
            }
        }

        private void ShowRewards(QuestRewards rewards, bool isSuccess)
        {
            if (rewardsPanel == null)
            {
                return;
            }

            if (!isSuccess || rewards == null)
            {
                rewardsPanel.SetActive(false);
                return;
            }

            rewardsPanel.SetActive(true);

            // Show gold reward
            if (goldRewardText != null)
            {
                goldRewardText.text = $"+{rewards.Gold}G";
            }

            // Show reputation reward
            if (reputationRewardText != null)
            {
                string reputationText = rewards.Reputation >= 0 ?
                    $"+{rewards.Reputation}" :
                    $"{rewards.Reputation}";
                reputationRewardText.text = reputationText;
                reputationRewardText.color = rewards.Reputation >= 0 ? successColor : failureColor;
            }

            // Show material rewards
            ShowMaterialRewards(rewards.Materials);
        }

        private void ShowMaterialRewards(List<MaterialDrop> materials)
        {
            ClearMaterialRewards();

            if (materialRewardsContainer == null || materials == null || materials.Count == 0)
            {
                return;
            }

            foreach (var material in materials)
            {
                CreateMaterialRewardItem(material);
            }
        }

        private void CreateMaterialRewardItem(MaterialDrop material)
        {
            if (materialRewardItemPrefab == null)
            {
                Debug.LogWarning("Material reward item prefab is not assigned");
                return;
            }

            GameObject item = Instantiate(materialRewardItemPrefab, materialRewardsContainer);

            // Find text components
            TMP_Text materialNameText = item.transform.Find("MaterialNameText")?.GetComponent<TMP_Text>();
            TMP_Text materialQuantityText = item.transform.Find("MaterialQuantityText")?.GetComponent<TMP_Text>();

            // Set text values
            if (materialNameText != null)
            {
                materialNameText.text = material.MaterialId;
            }

            if (materialQuantityText != null)
            {
                materialQuantityText.text = $"x{material.Quantity}";
            }
        }

        private void ClearMaterialRewards()
        {
            if (materialRewardsContainer == null)
            {
                return;
            }

            foreach (Transform child in materialRewardsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        #endregion

        #region Event Handlers

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            // Get quest details
            Quest quest = _questService?.GetQuestById(evt.QuestId);
            if (quest == null)
            {
                Debug.LogWarning($"Cannot show completion UI for unknown quest {evt.QuestId}");
                return;
            }

            // Process rewards
            QuestRewards rewards = new QuestRewards
            {
                Gold = evt.GoldReward,
                Reputation = evt.ReputationChange,
                Materials = new List<MaterialDrop>()
            };

            // Convert MaterialReward to MaterialDrop
            if (evt.MaterialRewards != null)
            {
                foreach (var materialReward in evt.MaterialRewards)
                {
                    rewards.Materials.Add(new MaterialDrop
                    {
                        MaterialId = materialReward.materialId,
                        Quantity = materialReward.quantity
                    });
                }
            }

            // Show completion UI
            ShowCompletionUI(quest, evt.IsSuccess, rewards);
        }

        private void OnContinueButtonClicked()
        {
            OnCompletionAcknowledged?.Invoke();
            HideCompletionUI();
        }

        #endregion
    }
}
