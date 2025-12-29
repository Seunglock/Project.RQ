using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for party recruitment interface
    /// </summary>
    public class PartyRecruitmentUI : MonoBehaviour
    {
        private PartyService partyService;
        private GameManager gameManager;

        private VisualElement rootElement;
        private Label goldLabel;
        private ListView partyListView;
        private Button refreshButton;

        private void Awake()
        {
            partyService = new PartyService();
            gameManager = GameManager.Instance;

            // Subscribe to events
            EventSystem.Instance.Subscribe<PartyRecruitedEvent>(OnPartyRecruited);
            EventSystem.Instance.Subscribe<GoldChangedEvent>(OnGoldChanged);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            EventSystem.Instance.Unsubscribe<PartyRecruitedEvent>(OnPartyRecruited);
            EventSystem.Instance.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }

        /// <summary>
        /// Initialize UI elements
        /// </summary>
        public void InitializeUI(VisualElement root)
        {
            rootElement = root;

            // Get UI elements
            goldLabel = rootElement.Q<Label>("GoldLabel");
            partyListView = rootElement.Q<ListView>("AvailablePartiesList");
            refreshButton = rootElement.Q<Button>("RefreshButton");

            // Setup button handlers
            if (refreshButton != null)
            {
                refreshButton.clicked += RefreshPartyList;
            }

            // Initial refresh
            RefreshPartyList();
            UpdateGoldDisplay();
        }

        /// <summary>
        /// Refresh the party recruitment options
        /// </summary>
        private void RefreshPartyList()
        {
            if (partyListView == null) return;

            // Get available parties for recruitment
            var availableParties = GenerateRecruitmentOptions();

            partyListView.itemsSource = availableParties;
            partyListView.makeItem = MakePartyListItem;
            partyListView.bindItem = BindPartyListItem;
            partyListView.Rebuild();
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
        private VisualElement MakePartyListItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.paddingBottom = 5;
            container.style.paddingTop = 5;

            var infoContainer = new VisualElement();
            var nameLabel = new Label();
            nameLabel.name = "PartyName";
            var statsLabel = new Label();
            statsLabel.name = "PartyStats";
            var costLabel = new Label();
            costLabel.name = "PartyCost";

            infoContainer.Add(nameLabel);
            infoContainer.Add(statsLabel);
            infoContainer.Add(costLabel);

            var recruitButton = new Button();
            recruitButton.name = "RecruitButton";
            recruitButton.text = "Recruit";

            container.Add(infoContainer);
            container.Add(recruitButton);

            return container;
        }

        /// <summary>
        /// Bind data to a list item
        /// </summary>
        private void BindPartyListItem(VisualElement element, int index)
        {
            var options = partyListView.itemsSource as List<PartyRecruitmentOption>;
            if (options == null || index >= options.Count) return;

            var option = options[index];

            var nameLabel = element.Q<Label>("PartyName");
            var statsLabel = element.Q<Label>("PartyStats");
            var costLabel = element.Q<Label>("PartyCost");
            var recruitButton = element.Q<Button>("RecruitButton");

            if (nameLabel != null)
                nameLabel.text = option.name;

            if (statsLabel != null)
                statsLabel.text = $"Exp: {option.explorationStat} | Combat: {option.combatStat} | Admin: {option.adminStat}";

            if (costLabel != null)
                costLabel.text = $"Cost: {option.cost} gold";

            if (recruitButton != null)
            {
                recruitButton.clicked += () => RecruitParty(option);
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
