using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for party recruitment interface
    /// </summary>
    public class PartyRecruitmentUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private Transform partyListContainer;
        [SerializeField] private GameObject partyListItemPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private ScrollRect partyScrollView;

        private PartyService partyService;
        private GameManager gameManager;

        private void Awake()
        {
            partyService = new PartyService();
            gameManager = GameManager.Instance;

            // Subscribe to events
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Subscribe<PartyRecruitedEvent>(OnPartyRecruited);
                EventSystem.Instance.Subscribe<GoldChangedEvent>(OnGoldChanged);
            }

            // Setup button handlers
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(RefreshPartyList);
            }
        }

        private void Start()
        {
            // Initial refresh
            RefreshPartyList();
            UpdateGoldDisplay();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Unsubscribe<PartyRecruitedEvent>(OnPartyRecruited);
                EventSystem.Instance.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            }
        }

        /// <summary>
        /// Refresh the party recruitment options
        /// </summary>
        private void RefreshPartyList()
        {
            if (partyListContainer == null) return;

            // Clear existing items
            foreach (Transform child in partyListContainer)
            {
                Destroy(child.gameObject);
            }

            // Get available parties for recruitment
            var availableParties = GenerateRecruitmentOptions();

            // Create party items
            foreach (var option in availableParties)
            {
                CreatePartyListItem(option);
            }
        }

        /// <summary>
        /// Generate recruitment options (placeholder data)
        /// </summary>
        private List<PartyRecruitmentOption> GenerateRecruitmentOptions()
        {
            var options = new List<PartyRecruitmentOption>();

            // Generate 3 random recruitment options
            var names = new[] { "Iron Wolves", "Silver Hawks", "Golden Dragons", "Crimson Blades", "Azure Shields" };
            var costs = new[] { 300, 500, 700, 1000 };

            for (int i = 0; i < 3; i++)
            {
                options.Add(new PartyRecruitmentOption
                {
                    name = names[Random.Range(0, names.Length)],
                    cost = costs[Random.Range(0, costs.Length)],
                    explorationStat = Random.Range(3, 8),
                    combatStat = Random.Range(3, 8),
                    adminStat = Random.Range(3, 8)
                });
            }

            return options;
        }

        /// <summary>
        /// Create a list item visual element
        /// </summary>
        private void CreatePartyListItem(PartyRecruitmentOption option)
        {
            GameObject item;

            if (partyListItemPrefab != null)
            {
                item = Instantiate(partyListItemPrefab, partyListContainer);
            }
            else
            {
                // Create item programmatically
                item = new GameObject("PartyListItem");
                item.transform.SetParent(partyListContainer, false);

                var layoutGroup = item.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = 10f;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;

                // Info Container
                GameObject infoContainer = new GameObject("InfoContainer");
                infoContainer.transform.SetParent(item.transform, false);

                var infoLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
                infoLayout.spacing = 5f;

                // Name
                GameObject nameObj = new GameObject("PartyName");
                nameObj.transform.SetParent(infoContainer.transform, false);
                TMP_Text nameText = nameObj.AddComponent<TMP_Text>();
                nameText.text = option.name;
                nameText.fontSize = 16;
                nameText.fontStyle = FontStyles.Bold;

                // Stats
                GameObject statsObj = new GameObject("PartyStats");
                statsObj.transform.SetParent(infoContainer.transform, false);
                TMP_Text statsText = statsObj.AddComponent<TMP_Text>();
                statsText.text = $"Exp: {option.explorationStat} | Combat: {option.combatStat} | Admin: {option.adminStat}";
                statsText.fontSize = 14;

                // Cost
                GameObject costObj = new GameObject("PartyCost");
                costObj.transform.SetParent(infoContainer.transform, false);
                TMP_Text costText = costObj.AddComponent<TMP_Text>();
                costText.text = $"Cost: {option.cost} gold";
                costText.fontSize = 14;
                costText.color = Color.yellow;

                // Recruit Button
                GameObject buttonObj = new GameObject("RecruitButton");
                buttonObj.transform.SetParent(item.transform, false);
                Button recruitButton = buttonObj.AddComponent<Button>();

                // Button background
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.6f, 0.2f);

                // Button text
                GameObject buttonTextObj = new GameObject("ButtonText");
                buttonTextObj.transform.SetParent(buttonObj.transform, false);
                TMP_Text buttonText = buttonTextObj.AddComponent<TMP_Text>();
                buttonText.text = "Recruit";
                buttonText.fontSize = 14;
                buttonText.alignment = TextAlignmentOptions.Center;

                recruitButton.onClick.AddListener(() => RecruitParty(option));
            }
        }

        /// <summary>
        /// Recruit a party
        /// </summary>
        private void RecruitParty(PartyRecruitmentOption option)
        {
            bool success = partyService.RecruitParty(option.name, option.cost);

            if (success)
            {
                Debug.Log($"Successfully recruited {option.name}");
                RefreshPartyList();
            }
            else
            {
                Debug.LogWarning($"Failed to recruit {option.name}");
            }
        }

        /// <summary>
        /// Update gold display
        /// </summary>
        private void UpdateGoldDisplay()
        {
            if (goldLabel != null && gameManager != null)
            {
                goldLabel.text = $"Gold: {gameManager.PlayerGold}";
            }
        }

        /// <summary>
        /// Handle party recruited event
        /// </summary>
        private void OnPartyRecruited(PartyRecruitedEvent evt)
        {
            Debug.Log($"Party recruited: {evt.PartyName}");
            RefreshPartyList();
        }

        /// <summary>
        /// Handle gold changed event
        /// </summary>
        private void OnGoldChanged(GoldChangedEvent evt)
        {
            UpdateGoldDisplay();
        }
    }

    /// <summary>
    /// Data structure for party recruitment options
    /// </summary>
    public class PartyRecruitmentOption
    {
        public string name;
        public int cost;
        public int explorationStat;
        public int combatStat;
        public int adminStat;
    }
}
