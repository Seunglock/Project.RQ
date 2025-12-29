using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for party management interface (training, equipment, etc.)
    /// </summary>
    public class PartyManagementUI : MonoBehaviour
    {
        private PartyService partyService;
        private GameManager gameManager;

        private VisualElement rootElement;
        private Label goldLabel;
        private ListView partyListView;
        private VisualElement partyDetailPanel;
        private Party selectedParty;

        private void Awake()
        {
            partyService = new PartyService();
            gameManager = GameManager.Instance;

            // Subscribe to events
            EventSystem.Instance.Subscribe<PartyTrainedEvent>(OnPartyTrained);
            EventSystem.Instance.Subscribe<EquipmentPurchasedEvent>(OnEquipmentPurchased);
            EventSystem.Instance.Subscribe<PartyLoyaltyChangedEvent>(OnPartyLoyaltyChanged);
            EventSystem.Instance.Subscribe<GoldChangedEvent>(OnGoldChanged);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            EventSystem.Instance.Unsubscribe<PartyTrainedEvent>(OnPartyTrained);
            EventSystem.Instance.Unsubscribe<EquipmentPurchasedEvent>(OnEquipmentPurchased);
            EventSystem.Instance.Unsubscribe<PartyLoyaltyChangedEvent>(OnPartyLoyaltyChanged);
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
            partyListView = rootElement.Q<ListView>("PartiesList");
            partyDetailPanel = rootElement.Q<VisualElement>("PartyDetailPanel");

            // Setup list view
            if (partyListView != null)
            {
                partyListView.selectionChanged += OnPartySelected;
            }

            // Initial refresh
            RefreshPartyList();
            UpdateGoldDisplay();
        }

        /// <summary>
        /// Refresh the party list
        /// </summary>
        private void RefreshPartyList()
        {
            if (partyListView == null) return;

            var parties = partyService.GetAllParties();

            partyListView.itemsSource = parties;
            partyListView.makeItem = MakePartyListItem;
            partyListView.bindItem = BindPartyListItem;
            partyListView.Rebuild();
        }

        /// <summary>
        /// Create a list item visual element
        /// </summary>
        private VisualElement MakePartyListItem()
        {
            var container = new VisualElement();
            container.style.paddingBottom = 5;
            container.style.paddingTop = 5;

            var nameLabel = new Label();
            nameLabel.name = "PartyName";

            var statusLabel = new Label();
            statusLabel.name = "PartyStatus";

            container.Add(nameLabel);
            container.Add(statusLabel);

            return container;
        }

        /// <summary>
        /// Bind data to a list item
        /// </summary>
        private void BindPartyListItem(VisualElement element, int index)
        {
            var parties = partyListView.itemsSource as List<Party>;
            if (parties == null || index >= parties.Count) return;

            var party = parties[index];

            var nameLabel = element.Q<Label>("PartyName");
            var statusLabel = element.Q<Label>("PartyStatus");

            if (nameLabel != null)
                nameLabel.text = party.name;

            if (statusLabel != null)
            {
                string availability = party.isAvailable ? "Available" : "Unavailable";
                statusLabel.text = $"Loyalty: {party.loyalty} | {availability}";
            }
        }

        /// <summary>
        /// Handle party selection
        /// </summary>
        private void OnPartySelected(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                selectedParty = item as Party;
                DisplayPartyDetails();
                break;
            }
        }

        /// <summary>
        /// Display detailed party information
        /// </summary>
        private void DisplayPartyDetails()
        {
            if (partyDetailPanel == null || selectedParty == null) return;

            partyDetailPanel.Clear();

            // Party info
            var titleLabel = new Label($"Party: {selectedParty.name}");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var loyaltyLabel = new Label($"Loyalty: {selectedParty.loyalty}");
            var availabilityLabel = new Label($"Status: {(selectedParty.isAvailable ? "Available" : "Unavailable")}");

            partyDetailPanel.Add(titleLabel);
            partyDetailPanel.Add(loyaltyLabel);
            partyDetailPanel.Add(availabilityLabel);

            // Stats section
            var statsTitle = new Label("Stats");
            statsTitle.style.fontSize = 16;
            statsTitle.style.marginTop = 10;
            partyDetailPanel.Add(statsTitle);

            foreach (var stat in selectedParty.stats)
            {
                var statContainer = new VisualElement();
                statContainer.style.flexDirection = FlexDirection.Row;
                statContainer.style.justifyContent = Justify.SpaceBetween;

                var statLabel = new Label($"{stat.Key}: {stat.Value}");
                var trainButton = new Button();
                trainButton.text = "Train (+200g)";
                trainButton.clicked += () => TrainStat(stat.Key);

                statContainer.Add(statLabel);
                statContainer.Add(trainButton);
                partyDetailPanel.Add(statContainer);
            }

            // Equipment section
            var equipmentTitle = new Label("Equipment");
            equipmentTitle.style.fontSize = 16;
            equipmentTitle.style.marginTop = 10;
            partyDetailPanel.Add(equipmentTitle);

            if (selectedParty.equipment.Count == 0)
            {
                partyDetailPanel.Add(new Label("No equipment"));
            }
            else
            {
                foreach (var eq in selectedParty.equipment)
                {
                    var eqLabel = new Label($"- {eq.name}");
                    partyDetailPanel.Add(eqLabel);
                }
            }

            var buyEquipmentButton = new Button();
            buyEquipmentButton.text = "Buy Equipment";
            buyEquipmentButton.clicked += ShowEquipmentShop;
            buyEquipmentButton.style.marginTop = 10;
            partyDetailPanel.Add(buyEquipmentButton);
        }

        /// <summary>
        /// Train a party's stat
        /// </summary>
        private void TrainStat(StatType statType)
        {
            if (selectedParty == null) return;

            bool success = partyService.TrainParty(selectedParty.id, statType, 200);

            if (success)
            {
                Debug.Log($"Trained {selectedParty.name}'s {statType}");
                DisplayPartyDetails(); // Refresh details
            }
            else
            {
                Debug.LogWarning($"Failed to train {statType}");
            }
        }

        /// <summary>
        /// Show equipment shop (placeholder)
        /// </summary>
        private void ShowEquipmentShop()
        {
            if (selectedParty == null) return;

            // Create a simple equipment purchase
            var equipment = new Equipment("Basic Sword", 250);
            equipment.statBonuses[StatType.Combat] = 2;

            bool success = partyService.PurchaseEquipment(selectedParty.id, equipment);

            if (success)
            {
                Debug.Log($"Purchased {equipment.name} for {selectedParty.name}");
                DisplayPartyDetails(); // Refresh details
            }
            else
            {
                Debug.LogWarning("Failed to purchase equipment");
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
        /// Handle party trained event
        /// </summary>
        private void OnPartyTrained(PartyTrainedEvent evt)
        {
            Debug.Log($"Party trained: {evt.StatType} improved to {evt.NewValue}");
            RefreshPartyList();
            if (selectedParty != null && selectedParty.id == evt.PartyId)
            {
                DisplayPartyDetails();
            }
        }

        /// <summary>
        /// Handle equipment purchased event
        /// </summary>
        private void OnEquipmentPurchased(EquipmentPurchasedEvent evt)
        {
            Debug.Log($"Equipment purchased: {evt.EquipmentName}");
            RefreshPartyList();
            if (selectedParty != null && selectedParty.id == evt.PartyId)
            {
                DisplayPartyDetails();
            }
        }

        /// <summary>
        /// Handle party loyalty changed event
        /// </summary>
        private void OnPartyLoyaltyChanged(PartyLoyaltyChangedEvent evt)
        {
            Debug.Log($"Party loyalty changed: {evt.OldLoyalty} -> {evt.NewLoyalty}");
            RefreshPartyList();
            if (selectedParty != null && selectedParty.id == evt.PartyId)
            {
                DisplayPartyDetails();
            }
        }

        /// <summary>
        /// Handle gold changed event
        /// </summary>
        private void OnGoldChanged(GoldChangedEvent evt)
        {
            UpdateGoldDisplay();
        }
    }
}
