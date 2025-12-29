using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for displaying available quests
    /// Handles quest list rendering, filtering, and selection
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform questListContainer;
        [SerializeField] private GameObject questItemPrefab;
        [SerializeField] private TMP_Text noQuestsText;

        [Header("Quest Details Panel")]
        [SerializeField] private GameObject questDetailsPanel;
        [SerializeField] private TMP_Text questTitleText;
        [SerializeField] private TMP_Text questTypeText;
        [SerializeField] private TMP_Text questDifficultyText;
        [SerializeField] private TMP_Text questDurationText;
        [SerializeField] private TMP_Text questRewardGoldText;
        [SerializeField] private TMP_Text questRequiredStatsText;
        [SerializeField] private TMP_Text questMaterialRewardsText;
        [SerializeField] private Button assignQuestButton;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown typeFilterDropdown;
        [SerializeField] private TMP_Dropdown difficultyFilterDropdown;

        // Internal state
        private QuestService _questService;
        private Quest _selectedQuest;
        private QuestType? _typeFilter;
        private int? _minDifficultyFilter;
        private int? _maxDifficultyFilter;

        // Events
        public System.Action<Quest> OnQuestSelected;
        public System.Action<Quest> OnAssignQuestClicked;

        private void Awake()
        {
            _questService = new QuestService();

            // Setup UI callbacks
            if (assignQuestButton != null)
            {
                assignQuestButton.onClick.AddListener(OnAssignButtonClicked);
            }

            if (typeFilterDropdown != null)
            {
                typeFilterDropdown.onValueChanged.AddListener(OnTypeFilterChanged);
            }

            if (difficultyFilterDropdown != null)
            {
                difficultyFilterDropdown.onValueChanged.AddListener(OnDifficultyFilterChanged);
            }

            // Hide details panel initially
            if (questDetailsPanel != null)
            {
                questDetailsPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to quest events
            EventSystem.Instance.Subscribe<QuestAddedEvent>(OnQuestAdded);
            EventSystem.Instance.Subscribe<QuestRemovedEvent>(OnQuestRemoved);
            EventSystem.Instance.Subscribe<QuestAssignedEvent>(OnQuestAssigned);
            EventSystem.Instance.Subscribe<QuestUnassignedEvent>(OnQuestUnassigned);

            RefreshQuestList();
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventSystem.Instance.Unsubscribe<QuestAddedEvent>(OnQuestAdded);
            EventSystem.Instance.Unsubscribe<QuestRemovedEvent>(OnQuestRemoved);
            EventSystem.Instance.Unsubscribe<QuestAssignedEvent>(OnQuestAssigned);
            EventSystem.Instance.Unsubscribe<QuestUnassignedEvent>(OnQuestUnassigned);
        }

        #region Public Methods

        /// <summary>
        /// Initialize the UI with a quest service instance
        /// </summary>
        public void Initialize(QuestService questService)
        {
            _questService = questService ?? new QuestService();
            RefreshQuestList();
        }

        /// <summary>
        /// Refresh the quest list display
        /// </summary>
        public void RefreshQuestList()
        {
            if (questListContainer == null)
            {
                Debug.LogWarning("Quest list container is not assigned");
                return;
            }

            // Clear existing items
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }

            // Get filtered quests
            List<Quest> quests = GetFilteredQuests();

            // Show no quests message if list is empty
            if (noQuestsText != null)
            {
                noQuestsText.gameObject.SetActive(quests.Count == 0);
            }

            // Create quest items
            foreach (Quest quest in quests)
            {
                CreateQuestItem(quest);
            }
        }

        /// <summary>
        /// Select a specific quest and show its details
        /// </summary>
        public void SelectQuest(Quest quest)
        {
            _selectedQuest = quest;

            if (_selectedQuest != null && questDetailsPanel != null)
            {
                ShowQuestDetails(_selectedQuest);
                questDetailsPanel.SetActive(true);

                OnQuestSelected?.Invoke(_selectedQuest);
            }
            else if (questDetailsPanel != null)
            {
                questDetailsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Clear quest selection
        /// </summary>
        public void ClearSelection()
        {
            _selectedQuest = null;
            if (questDetailsPanel != null)
            {
                questDetailsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Set type filter
        /// </summary>
        public void SetTypeFilter(QuestType? type)
        {
            _typeFilter = type;
            RefreshQuestList();
        }

        /// <summary>
        /// Set difficulty filter
        /// </summary>
        public void SetDifficultyFilter(int? minDifficulty, int? maxDifficulty)
        {
            _minDifficultyFilter = minDifficulty;
            _maxDifficultyFilter = maxDifficulty;
            RefreshQuestList();
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearFilters()
        {
            _typeFilter = null;
            _minDifficultyFilter = null;
            _maxDifficultyFilter = null;
            RefreshQuestList();
        }

        #endregion

        #region Private Methods

        private List<Quest> GetFilteredQuests()
        {
            if (_questService == null)
            {
                return new List<Quest>();
            }

            List<Quest> quests = _questService.GetAvailableQuests();

            // Apply type filter
            if (_typeFilter.HasValue)
            {
                quests = quests.Where(q => q.type == _typeFilter.Value).ToList();
            }

            // Apply difficulty filter
            if (_minDifficultyFilter.HasValue)
            {
                quests = quests.Where(q => q.difficulty >= _minDifficultyFilter.Value).ToList();
            }

            if (_maxDifficultyFilter.HasValue)
            {
                quests = quests.Where(q => q.difficulty <= _maxDifficultyFilter.Value).ToList();
            }

            return quests;
        }

        private void CreateQuestItem(Quest quest)
        {
            if (questItemPrefab == null)
            {
                Debug.LogWarning("Quest item prefab is not assigned");
                return;
            }

            GameObject item = Instantiate(questItemPrefab, questListContainer);

            // Find text components (assuming prefab has specific structure)
            TMP_Text titleText = item.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            TMP_Text typeText = item.transform.Find("TypeText")?.GetComponent<TMP_Text>();
            TMP_Text difficultyText = item.transform.Find("DifficultyText")?.GetComponent<TMP_Text>();
            TMP_Text rewardText = item.transform.Find("RewardText")?.GetComponent<TMP_Text>();
            Button selectButton = item.GetComponent<Button>();

            // Set text values
            if (titleText != null)
            {
                titleText.text = $"Quest {quest.id.Substring(0, Mathf.Min(8, quest.id.Length))}";
            }

            if (typeText != null)
            {
                typeText.text = quest.type.ToString();
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"Difficulty: {quest.difficulty}";
            }

            if (rewardText != null)
            {
                rewardText.text = $"{quest.rewardGold}G";
            }

            // Set up selection callback
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectQuest(quest));
            }
        }

        private void ShowQuestDetails(Quest quest)
        {
            if (questTitleText != null)
            {
                questTitleText.text = $"Quest {quest.id.Substring(0, Mathf.Min(8, quest.id.Length))}";
            }

            if (questTypeText != null)
            {
                questTypeText.text = $"Type: {quest.type}";
            }

            if (questDifficultyText != null)
            {
                questDifficultyText.text = $"Difficulty: {quest.difficulty}/5";
            }

            if (questDurationText != null)
            {
                questDurationText.text = $"Duration: {quest.duration} days";
            }

            if (questRewardGoldText != null)
            {
                questRewardGoldText.text = $"Gold: {quest.rewardGold}G";
            }

            if (questRequiredStatsText != null)
            {
                string statsText = "Required Stats:\n";
                foreach (var stat in quest.requiredStats)
                {
                    statsText += $"{stat.Key}: {stat.Value}\n";
                }
                questRequiredStatsText.text = statsText;
            }

            if (questMaterialRewardsText != null)
            {
                string materialsText = "Material Rewards:\n";
                if (quest.rewardMaterials.Count > 0)
                {
                    foreach (var material in quest.rewardMaterials)
                    {
                        materialsText += $"{material.materialId}: x{material.quantity} ({material.dropChance * 100}% chance)\n";
                    }
                }
                else
                {
                    materialsText += "None";
                }
                questMaterialRewardsText.text = materialsText;
            }

            // Enable/disable assign button based on quest state
            if (assignQuestButton != null)
            {
                assignQuestButton.interactable = quest.state == QuestState.Available;
            }
        }

        #endregion

        #region Event Handlers

        private void OnQuestAdded(QuestAddedEvent evt)
        {
            RefreshQuestList();
        }

        private void OnQuestRemoved(QuestRemovedEvent evt)
        {
            if (_selectedQuest != null && _selectedQuest.id == evt.QuestId)
            {
                ClearSelection();
            }
            RefreshQuestList();
        }

        private void OnQuestAssigned(QuestAssignedEvent evt)
        {
            if (_selectedQuest != null && _selectedQuest.id == evt.QuestId)
            {
                ClearSelection();
            }
            RefreshQuestList();
        }

        private void OnQuestUnassigned(QuestUnassignedEvent evt)
        {
            RefreshQuestList();
        }

        private void OnAssignButtonClicked()
        {
            if (_selectedQuest != null)
            {
                OnAssignQuestClicked?.Invoke(_selectedQuest);
            }
        }

        private void OnTypeFilterChanged(int index)
        {
            if (index == 0)
            {
                _typeFilter = null;
            }
            else
            {
                _typeFilter = (QuestType)(index - 1);
            }
            RefreshQuestList();
        }

        private void OnDifficultyFilterChanged(int index)
        {
            switch (index)
            {
                case 0: // All
                    _minDifficultyFilter = null;
                    _maxDifficultyFilter = null;
                    break;
                case 1: // Easy (1-2)
                    _minDifficultyFilter = 1;
                    _maxDifficultyFilter = 2;
                    break;
                case 2: // Medium (3)
                    _minDifficultyFilter = 3;
                    _maxDifficultyFilter = 3;
                    break;
                case 3: // Hard (4-5)
                    _minDifficultyFilter = 4;
                    _maxDifficultyFilter = 5;
                    break;
            }
            RefreshQuestList();
        }

        #endregion
    }
}
