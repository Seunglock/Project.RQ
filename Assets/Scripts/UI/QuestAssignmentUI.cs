using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for assigning quests to parties
    /// Handles party selection, success rate display, and assignment confirmation
    /// </summary>
    public class QuestAssignmentUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject assignmentPanel;
        [SerializeField] private TMP_Text questInfoText;
        [SerializeField] private Transform partyListContainer;
        [SerializeField] private GameObject partyItemPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text noPartiesText;

        [Header("Party Details Panel")]
        [SerializeField] private GameObject partyDetailsPanel;
        [SerializeField] private TMP_Text partyNameText;
        [SerializeField] private TMP_Text partyStatsText;
        [SerializeField] private TMP_Text partyLoyaltyText;
        [SerializeField] private TMP_Text partyAvailabilityText;
        [SerializeField] private TMP_Text successRateText;

        // Internal state
        private QuestService _questService;
        private Quest _currentQuest;
        private Party _selectedParty;
        private List<Party> _availableParties;

        // Events
        public System.Action<Quest, Party> OnQuestAssigned;
        public System.Action OnAssignmentCancelled;

        private void Awake()
        {
            _questService = new QuestService();
            _availableParties = new List<Party>();

            // Setup UI callbacks
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            // Hide panels initially
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(false);
            }

            if (partyDetailsPanel != null)
            {
                partyDetailsPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to party events
            EventSystem.Instance.Subscribe<PartyRecruitedEvent>(OnPartyRecruited);
            EventSystem.Instance.Subscribe<PartyAvailabilityChangedEvent>(OnPartyAvailabilityChanged);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventSystem.Instance.Unsubscribe<PartyRecruitedEvent>(OnPartyRecruited);
            EventSystem.Instance.Unsubscribe<PartyAvailabilityChangedEvent>(OnPartyAvailabilityChanged);
        }

        #region Public Methods

        /// <summary>
        /// Initialize the UI with service instances
        /// </summary>
        public void Initialize(QuestService questService, List<Party> parties)
        {
            _questService = questService ?? new QuestService();
            _availableParties = parties ?? new List<Party>();
        }

        /// <summary>
        /// Show the assignment UI for a specific quest
        /// </summary>
        public void ShowAssignmentUI(Quest quest, List<Party> availableParties)
        {
            if (quest == null)
            {
                Debug.LogError("Cannot show assignment UI with null quest");
                return;
            }

            _currentQuest = quest;
            _availableParties = availableParties ?? new List<Party>();
            _selectedParty = null;

            // Show quest info
            ShowQuestInfo();

            // Populate party list
            RefreshPartyList();

            // Show panel
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(true);
            }

            // Disable confirm button until a party is selected
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }

        /// <summary>
        /// Hide the assignment UI
        /// </summary>
        public void HideAssignmentUI()
        {
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(false);
            }

            if (partyDetailsPanel != null)
            {
                partyDetailsPanel.SetActive(false);
            }

            _currentQuest = null;
            _selectedParty = null;
        }

        /// <summary>
        /// Select a party for assignment
        /// </summary>
        public void SelectParty(Party party)
        {
            _selectedParty = party;

            if (_selectedParty != null && partyDetailsPanel != null)
            {
                ShowPartyDetails(_selectedParty);
                partyDetailsPanel.SetActive(true);

                // Enable confirm button
                if (confirmButton != null)
                {
                    confirmButton.interactable = _selectedParty.isAvailable;
                }
            }
            else if (partyDetailsPanel != null)
            {
                partyDetailsPanel.SetActive(false);

                if (confirmButton != null)
                {
                    confirmButton.interactable = false;
                }
            }
        }

        /// <summary>
        /// Clear party selection
        /// </summary>
        public void ClearSelection()
        {
            _selectedParty = null;

            if (partyDetailsPanel != null)
            {
                partyDetailsPanel.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }

        #endregion

        #region Private Methods

        private void ShowQuestInfo()
        {
            if (questInfoText != null && _currentQuest != null)
            {
                string info = $"<b>Quest Assignment</b>\n\n";
                info += $"Quest: {_currentQuest.id.Substring(0, Mathf.Min(8, _currentQuest.id.Length))}\n";
                info += $"Type: {_currentQuest.type}\n";
                info += $"Difficulty: {_currentQuest.difficulty}/5\n";
                info += $"Duration: {_currentQuest.duration} days\n";
                info += $"Reward: {_currentQuest.rewardGold}G\n\n";
                info += "Select a party to assign this quest.";

                questInfoText.text = info;
            }
        }

        private void RefreshPartyList()
        {
            if (partyListContainer == null)
            {
                Debug.LogWarning("Party list container is not assigned");
                return;
            }

            // Clear existing items
            foreach (Transform child in partyListContainer)
            {
                Destroy(child.gameObject);
            }

            // Filter available parties
            List<Party> availablePartiesOnly = _availableParties
                .Where(p => p.isAvailable)
                .ToList();

            // Show no parties message if list is empty
            if (noPartiesText != null)
            {
                noPartiesText.gameObject.SetActive(availablePartiesOnly.Count == 0);
            }

            // Create party items
            foreach (Party party in availablePartiesOnly)
            {
                CreatePartyItem(party);
            }
        }

        private void CreatePartyItem(Party party)
        {
            if (partyItemPrefab == null)
            {
                Debug.LogWarning("Party item prefab is not assigned");
                return;
            }

            GameObject item = Instantiate(partyItemPrefab, partyListContainer);

            // Find text components (assuming prefab has specific structure)
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text statsText = item.transform.Find("StatsText")?.GetComponent<TMP_Text>();
            TMP_Text successRateText = item.transform.Find("SuccessRateText")?.GetComponent<TMP_Text>();
            Button selectButton = item.GetComponent<Button>();

            // Set text values
            if (nameText != null)
            {
                nameText.text = party.name;
            }

            if (statsText != null)
            {
                string stats = $"C:{party.stats[StatType.Combat]} E:{party.stats[StatType.Exploration]} A:{party.stats[StatType.Admin]}";
                statsText.text = stats;
            }

            if (successRateText != null && _currentQuest != null && _questService != null)
            {
                float successRate = _questService.CalculateSuccessRate(_currentQuest, party);
                successRateText.text = $"{(successRate * 100):F0}%";

                // Color code success rate
                if (successRate >= 0.7f)
                {
                    successRateText.color = Color.green;
                }
                else if (successRate >= 0.4f)
                {
                    successRateText.color = Color.yellow;
                }
                else
                {
                    successRateText.color = Color.red;
                }
            }

            // Set up selection callback
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectParty(party));
            }
        }

        private void ShowPartyDetails(Party party)
        {
            if (partyNameText != null)
            {
                partyNameText.text = party.name;
            }

            if (partyStatsText != null)
            {
                string stats = "<b>Stats:</b>\n";
                foreach (var stat in party.stats)
                {
                    stats += $"{stat.Key}: {stat.Value}\n";
                }
                partyStatsText.text = stats;
            }

            if (partyLoyaltyText != null)
            {
                partyLoyaltyText.text = $"Loyalty: {party.loyalty}/100";
            }

            if (partyAvailabilityText != null)
            {
                partyAvailabilityText.text = party.isAvailable ? "Available" : "Unavailable";
            }

            // Calculate and display success rate
            if (successRateText != null && _currentQuest != null && _questService != null)
            {
                float successRate = _questService.CalculateSuccessRate(_currentQuest, party);
                successRateText.text = $"<b>Success Rate:</b> {(successRate * 100):F1}%";

                // Color code success rate
                if (successRate >= 0.7f)
                {
                    successRateText.color = Color.green;
                }
                else if (successRate >= 0.4f)
                {
                    successRateText.color = Color.yellow;
                }
                else
                {
                    successRateText.color = Color.red;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnConfirmButtonClicked()
        {
            if (_currentQuest == null || _selectedParty == null)
            {
                Debug.LogWarning("Cannot confirm assignment: quest or party is null");
                return;
            }

            if (!_selectedParty.isAvailable)
            {
                Debug.LogWarning("Cannot assign quest to unavailable party");
                return;
            }

            // Attempt to assign quest
            if (_questService != null && _questService.AssignQuest(_currentQuest.id, _selectedParty))
            {
                Debug.Log($"Quest {_currentQuest.id} assigned to party {_selectedParty.name}");

                OnQuestAssigned?.Invoke(_currentQuest, _selectedParty);
                HideAssignmentUI();
            }
            else
            {
                Debug.LogError($"Failed to assign quest {_currentQuest.id} to party {_selectedParty.name}");
            }
        }

        private void OnCancelButtonClicked()
        {
            OnAssignmentCancelled?.Invoke();
            HideAssignmentUI();
        }

        private void OnPartyRecruited(PartyRecruitedEvent evt)
        {
            RefreshPartyList();
        }

        private void OnPartyAvailabilityChanged(PartyAvailabilityChangedEvent evt)
        {
            RefreshPartyList();
        }

        #endregion
    }
}
