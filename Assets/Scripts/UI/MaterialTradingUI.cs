using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for material trading market interface
    /// Handles buying, selling, and market price display
    /// </summary>
    public class MaterialTradingUI : MonoBehaviour
    {
        private MaterialService materialService;
        private GameManager gameManager;

        // UI Elements
        private VisualElement root;
        private ListView materialListView;
        private Label goldLabel;
        private Button buyButton;
        private Button sellButton;
        private TextField quantityField;
        private Label selectedMaterialLabel;
        private Label priceLabel;
        private Label totalCostLabel;

        private List<Material> availableMaterials;
        private Material selectedMaterial;
        private int selectedQuantity = 1;

        private void Start()
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("GameManager not found");
                return;
            }

            // Initialize MaterialService if not already created
            if (gameManager.materialService == null)
            {
                gameManager.materialService = new MaterialService(gameManager);
            }
            materialService = gameManager.materialService;

            SetupUI();
            SubscribeToEvents();
            RefreshUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region UI Setup

        private void SetupUI()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found");
                return;
            }

            root = uiDocument.rootVisualElement;

            // Get UI elements
            goldLabel = root.Q<Label>("GoldLabel");
            materialListView = root.Q<ListView>("MaterialList");
            selectedMaterialLabel = root.Q<Label>("SelectedMaterialLabel");
            priceLabel = root.Q<Label>("PriceLabel");
            quantityField = root.Q<TextField>("QuantityField");
            totalCostLabel = root.Q<Label>("TotalCostLabel");
            buyButton = root.Q<Button>("BuyButton");
            sellButton = root.Q<Button>("SellButton");

            // Setup ListView
            if (materialListView != null)
            {
                materialListView.makeItem = MakeMaterialItem;
                materialListView.bindItem = BindMaterialItem;
                materialListView.onSelectionChange += OnMaterialSelected;
            }

            // Setup quantity field
            if (quantityField != null)
            {
                quantityField.value = "1";
                quantityField.RegisterValueChangedCallback(OnQuantityChanged);
            }

            // Setup buttons
            if (buyButton != null)
            {
                buyButton.clicked += OnBuyClicked;
            }

            if (sellButton != null)
            {
                sellButton.clicked += OnSellClicked;
            }

            Debug.Log("MaterialTradingUI setup complete");
        }

        private VisualElement MakeMaterialItem()
        {
            var container = new VisualElement();
            container.AddToClassList("material-item");

            var nameLabel = new Label();
            nameLabel.name = "NameLabel";
            nameLabel.AddToClassList("material-name");

            var rarityLabel = new Label();
            rarityLabel.name = "RarityLabel";
            rarityLabel.AddToClassList("material-rarity");

            var priceLabel = new Label();
            priceLabel.name = "PriceLabel";
            priceLabel.AddToClassList("material-price");

            var quantityLabel = new Label();
            quantityLabel.name = "QuantityLabel";
            quantityLabel.AddToClassList("material-quantity");

            container.Add(nameLabel);
            container.Add(rarityLabel);
            container.Add(priceLabel);
            container.Add(quantityLabel);

            return container;
        }

        private void BindMaterialItem(VisualElement element, int index)
        {
            if (availableMaterials == null || index >= availableMaterials.Count)
                return;

            Material material = availableMaterials[index];

            var nameLabel = element.Q<Label>("NameLabel");
            var rarityLabel = element.Q<Label>("RarityLabel");
            var priceLabel = element.Q<Label>("PriceLabel");
            var quantityLabel = element.Q<Label>("QuantityLabel");

            if (nameLabel != null)
                nameLabel.text = material.name;

            if (rarityLabel != null)
                rarityLabel.text = material.rarity.ToString();

            if (priceLabel != null)
                priceLabel.text = $"{material.currentValue}g";

            if (quantityLabel != null)
            {
                int owned = materialService.GetMaterialQuantity(material.id);
                quantityLabel.text = $"Owned: {owned}";
            }
        }

        #endregion

        #region Event Handlers

        private void OnMaterialSelected(IEnumerable<object> selection)
        {
            foreach (var item in selection)
            {
                if (item is Material material)
                {
                    selectedMaterial = material;
                    UpdateSelectedMaterialDisplay();
                    break;
                }
            }
        }

        private void OnQuantityChanged(ChangeEvent<string> evt)
        {
            if (int.TryParse(evt.newValue, out int quantity))
            {
                selectedQuantity = Mathf.Max(1, quantity);
                UpdateTotalCost();
            }
        }

        private void OnBuyClicked()
        {
            if (selectedMaterial == null)
            {
                Debug.LogWarning("No material selected");
                return;
            }

            if (selectedQuantity <= 0)
            {
                Debug.LogWarning("Invalid quantity");
                return;
            }

            bool success = materialService.BuyMaterial(selectedMaterial.id, selectedQuantity);
            if (success)
            {
                Debug.Log($"Successfully bought {selectedQuantity}x {selectedMaterial.name}");
                RefreshUI();
            }
            else
            {
                Debug.LogWarning($"Failed to buy {selectedQuantity}x {selectedMaterial.name}");
            }
        }

        private void OnSellClicked()
        {
            if (selectedMaterial == null)
            {
                Debug.LogWarning("No material selected");
                return;
            }

            if (selectedQuantity <= 0)
            {
                Debug.LogWarning("Invalid quantity");
                return;
            }

            bool success = materialService.SellMaterial(selectedMaterial.id, selectedQuantity);
            if (success)
            {
                Debug.Log($"Successfully sold {selectedQuantity}x {selectedMaterial.name}");
                RefreshUI();
            }
            else
            {
                Debug.LogWarning($"Failed to sell {selectedQuantity}x {selectedMaterial.name}");
            }
        }

        #endregion

        #region UI Updates

        private void RefreshUI()
        {
            UpdateGoldDisplay();
            UpdateMaterialList();
            UpdateSelectedMaterialDisplay();
        }

        private void UpdateGoldDisplay()
        {
            if (goldLabel != null && gameManager != null)
            {
                goldLabel.text = $"Gold: {gameManager.gold}";
            }
        }

        private void UpdateMaterialList()
        {
            availableMaterials = materialService.GetAllMaterials();
            
            if (materialListView != null)
            {
                materialListView.itemsSource = availableMaterials;
                materialListView.Rebuild();
            }

            Debug.Log($"Updated material list: {availableMaterials.Count} materials");
        }

        private void UpdateSelectedMaterialDisplay()
        {
            if (selectedMaterial == null)
            {
                if (selectedMaterialLabel != null)
                    selectedMaterialLabel.text = "No material selected";
                if (priceLabel != null)
                    priceLabel.text = "Price: --";
                return;
            }

            if (selectedMaterialLabel != null)
            {
                selectedMaterialLabel.text = $"{selectedMaterial.name} ({selectedMaterial.rarity})";
            }

            if (priceLabel != null)
            {
                priceLabel.text = $"Price: {selectedMaterial.currentValue}g";
            }

            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            if (selectedMaterial == null || totalCostLabel == null)
                return;

            int total = selectedMaterial.currentValue * selectedQuantity;
            totalCostLabel.text = $"Total: {total}g";

            // Update button states
            if (buyButton != null)
            {
                bool canAfford = gameManager != null && gameManager.gold >= total;
                buyButton.SetEnabled(canAfford);
            }

            if (sellButton != null)
            {
                int owned = materialService.GetMaterialQuantity(selectedMaterial.id);
                bool hasEnough = owned >= selectedQuantity;
                sellButton.SetEnabled(hasEnough);
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Subscribe<MaterialPurchasedEvent>(OnMaterialPurchased);
                EventSystem.Instance.Subscribe<MaterialSoldEvent>(OnMaterialSold);
                EventSystem.Instance.Subscribe<MarketFluctuationEvent>(OnMarketFluctuation);
                EventSystem.Instance.Subscribe<GoldChangedEvent>(OnGoldChanged);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Unsubscribe<MaterialPurchasedEvent>(OnMaterialPurchased);
                EventSystem.Instance.Unsubscribe<MaterialSoldEvent>(OnMaterialSold);
                EventSystem.Instance.Unsubscribe<MarketFluctuationEvent>(OnMarketFluctuation);
                EventSystem.Instance.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            }
        }

        private void OnMaterialPurchased(MaterialPurchasedEvent evt)
        {
            Debug.Log($"UI: Material purchased - {evt.MaterialName} x{evt.Quantity}");
            RefreshUI();
        }

        private void OnMaterialSold(MaterialSoldEvent evt)
        {
            Debug.Log($"UI: Material sold - {evt.MaterialName} x{evt.Quantity}");
            RefreshUI();
        }

        private void OnMarketFluctuation(MarketFluctuationEvent evt)
        {
            Debug.Log($"UI: Market fluctuation - {evt.MaterialName} price changed");
            RefreshUI();
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            UpdateGoldDisplay();
            UpdateTotalCost(); // Update button states based on new gold amount
        }

        #endregion

        #region Public API

        /// <summary>
        /// Apply random market fluctuations
        /// </summary>
        public void ApplyMarketFluctuations()
        {
            materialService.ApplyRandomMarketFluctuations();
            RefreshUI();
        }

        /// <summary>
        /// Filter materials by category
        /// </summary>
        public void FilterByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                availableMaterials = materialService.GetAllMaterials();
            }
            else
            {
                availableMaterials = materialService.GetMaterialsByCategory(category);
            }

            if (materialListView != null)
            {
                materialListView.itemsSource = availableMaterials;
                materialListView.Rebuild();
            }
        }

        /// <summary>
        /// Filter materials by rarity
        /// </summary>
        public void FilterByRarity(MaterialRarity rarity)
        {
            availableMaterials = materialService.GetMaterialsByRarity(rarity);

            if (materialListView != null)
            {
                materialListView.itemsSource = availableMaterials;
                materialListView.Rebuild();
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearFilters()
        {
            availableMaterials = materialService.GetAllMaterials();

            if (materialListView != null)
            {
                materialListView.itemsSource = availableMaterials;
                materialListView.Rebuild();
            }
        }

        #endregion
    }
}
