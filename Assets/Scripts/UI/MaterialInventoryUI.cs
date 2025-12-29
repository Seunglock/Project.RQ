using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for player material inventory and synthesis interface
    /// Displays owned materials, allows combination/synthesis, and shows material details
    /// </summary>
    public class MaterialInventoryUI : MonoBehaviour
    {
        private MaterialService materialService;
        private GameManager gameManager;

        // UI Elements
        private VisualElement root;
        private ListView inventoryListView;
        private Label totalValueLabel;
        private Label selectedMaterialLabel;
        private Label materialDetailsLabel;
        private Button combineButton;
        private TextField combineQuantityField;
        private Label combinationCostLabel;
        private Label combinationValueLabel;
        private ListView recipeListView;
        private ListView craftableListView;

        private List<Material> inventoryMaterials;
        private List<Material> craftableMaterials;
        private Material selectedMaterial;
        private int combineQuantity = 1;

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
            inventoryListView = root.Q<ListView>("InventoryList");
            totalValueLabel = root.Q<Label>("TotalValueLabel");
            selectedMaterialLabel = root.Q<Label>("SelectedMaterialLabel");
            materialDetailsLabel = root.Q<Label>("MaterialDetailsLabel");
            combineButton = root.Q<Button>("CombineButton");
            combineQuantityField = root.Q<TextField>("CombineQuantityField");
            combinationCostLabel = root.Q<Label>("CombinationCostLabel");
            combinationValueLabel = root.Q<Label>("CombinationValueLabel");
            recipeListView = root.Q<ListView>("RecipeList");
            craftableListView = root.Q<ListView>("CraftableList");

            // Setup inventory ListView
            if (inventoryListView != null)
            {
                inventoryListView.makeItem = MakeInventoryItem;
                inventoryListView.bindItem = BindInventoryItem;
                inventoryListView.onSelectionChange += OnInventoryItemSelected;
            }

            // Setup craftable ListView
            if (craftableListView != null)
            {
                craftableListView.makeItem = MakeCraftableItem;
                craftableListView.bindItem = BindCraftableItem;
                craftableListView.onSelectionChange += OnCraftableItemSelected;
            }

            // Setup recipe ListView
            if (recipeListView != null)
            {
                recipeListView.makeItem = MakeRecipeItem;
                recipeListView.bindItem = BindRecipeItem;
            }

            // Setup combine quantity field
            if (combineQuantityField != null)
            {
                combineQuantityField.value = "1";
                combineQuantityField.RegisterValueChangedCallback(OnCombineQuantityChanged);
            }

            // Setup combine button
            if (combineButton != null)
            {
                combineButton.clicked += OnCombineClicked;
            }

            Debug.Log("MaterialInventoryUI setup complete");
        }

        private VisualElement MakeInventoryItem()
        {
            var container = new VisualElement();
            container.AddToClassList("inventory-item");

            var nameLabel = new Label();
            nameLabel.name = "NameLabel";
            nameLabel.AddToClassList("material-name");

            var quantityLabel = new Label();
            quantityLabel.name = "QuantityLabel";
            quantityLabel.AddToClassList("material-quantity");

            var valueLabel = new Label();
            valueLabel.name = "ValueLabel";
            valueLabel.AddToClassList("material-value");

            container.Add(nameLabel);
            container.Add(quantityLabel);
            container.Add(valueLabel);

            return container;
        }

        private void BindInventoryItem(VisualElement element, int index)
        {
            if (inventoryMaterials == null || index >= inventoryMaterials.Count)
                return;

            Material material = inventoryMaterials[index];

            var nameLabel = element.Q<Label>("NameLabel");
            var quantityLabel = element.Q<Label>("QuantityLabel");
            var valueLabel = element.Q<Label>("ValueLabel");

            if (nameLabel != null)
                nameLabel.text = material.name;

            if (quantityLabel != null)
                quantityLabel.text = $"x{material.quantity}";

            if (valueLabel != null)
            {
                int totalValue = material.currentValue * material.quantity;
                valueLabel.text = $"{totalValue}g";
            }
        }

        private VisualElement MakeCraftableItem()
        {
            var container = new VisualElement();
            container.AddToClassList("craftable-item");

            var nameLabel = new Label();
            nameLabel.name = "NameLabel";

            var rarityLabel = new Label();
            rarityLabel.name = "RarityLabel";

            var valueLabel = new Label();
            valueLabel.name = "ValueLabel";

            container.Add(nameLabel);
            container.Add(rarityLabel);
            container.Add(valueLabel);

            return container;
        }

        private void BindCraftableItem(VisualElement element, int index)
        {
            if (craftableMaterials == null || index >= craftableMaterials.Count)
                return;

            Material material = craftableMaterials[index];

            var nameLabel = element.Q<Label>("NameLabel");
            var rarityLabel = element.Q<Label>("RarityLabel");
            var valueLabel = element.Q<Label>("ValueLabel");

            if (nameLabel != null)
                nameLabel.text = material.name;

            if (rarityLabel != null)
                rarityLabel.text = material.rarity.ToString();

            if (valueLabel != null)
                valueLabel.text = $"{material.currentValue}g";
        }

        private VisualElement MakeRecipeItem()
        {
            var container = new VisualElement();
            container.AddToClassList("recipe-item");

            var label = new Label();
            label.name = "RecipeLabel";

            container.Add(label);
            return container;
        }

        private void BindRecipeItem(VisualElement element, int index)
        {
            if (selectedMaterial == null || selectedMaterial.combinationRecipe == null)
                return;

            var recipeEntries = selectedMaterial.combinationRecipe.ToList();
            if (index >= recipeEntries.Count)
                return;

            var ingredient = recipeEntries[index];
            var label = element.Q<Label>("RecipeLabel");

            if (label != null)
            {
                // Get material name from registry
                Material ingredientMaterial = materialService.GetMaterial(ingredient.Key);
                string materialName = ingredientMaterial != null ? ingredientMaterial.name : ingredient.Key;
                
                int owned = materialService.GetMaterialQuantity(ingredient.Key);
                int required = ingredient.Value;
                
                label.text = $"{materialName}: {owned}/{required}";
                
                // Color code based on availability
                if (owned >= required)
                {
                    label.style.color = Color.green;
                }
                else
                {
                    label.style.color = Color.red;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnInventoryItemSelected(IEnumerable<object> selection)
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

        private void OnCraftableItemSelected(IEnumerable<object> selection)
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

        private void OnCombineQuantityChanged(ChangeEvent<string> evt)
        {
            if (int.TryParse(evt.newValue, out int quantity))
            {
                combineQuantity = Mathf.Max(1, quantity);
                UpdateCombinationDisplay();
            }
        }

        private void OnCombineClicked()
        {
            if (selectedMaterial == null)
            {
                Debug.LogWarning("No material selected for combination");
                return;
            }

            if (combineQuantity <= 0)
            {
                Debug.LogWarning("Invalid combination quantity");
                return;
            }

            bool success = materialService.CombineMaterials(selectedMaterial.id, combineQuantity);
            if (success)
            {
                Debug.Log($"Successfully combined {combineQuantity}x {selectedMaterial.name}");
                RefreshUI();
            }
            else
            {
                Debug.LogWarning($"Failed to combine {combineQuantity}x {selectedMaterial.name}");
            }
        }

        #endregion

        #region UI Updates

        private void RefreshUI()
        {
            UpdateInventoryList();
            UpdateCraftableList();
            UpdateTotalValue();
            UpdateSelectedMaterialDisplay();
        }

        private void UpdateInventoryList()
        {
            var inventory = materialService.GetPlayerInventory();
            inventoryMaterials = inventory.Values
                .Where(m => m.quantity > 0)
                .OrderBy(m => m.rarity)
                .ThenBy(m => m.name)
                .ToList();

            if (inventoryListView != null)
            {
                inventoryListView.itemsSource = inventoryMaterials;
                inventoryListView.Rebuild();
            }

            Debug.Log($"Updated inventory: {inventoryMaterials.Count} materials");
        }

        private void UpdateCraftableList()
        {
            craftableMaterials = materialService.GetCraftableMaterials();

            if (craftableListView != null)
            {
                craftableListView.itemsSource = craftableMaterials;
                craftableListView.Rebuild();
            }

            Debug.Log($"Craftable materials: {craftableMaterials.Count}");
        }

        private void UpdateTotalValue()
        {
            if (totalValueLabel == null)
                return;

            int totalValue = inventoryMaterials.Sum(m => m.currentValue * m.quantity);
            totalValueLabel.text = $"Total Inventory Value: {totalValue}g";
        }

        private void UpdateSelectedMaterialDisplay()
        {
            if (selectedMaterial == null)
            {
                if (selectedMaterialLabel != null)
                    selectedMaterialLabel.text = "No material selected";
                
                if (materialDetailsLabel != null)
                    materialDetailsLabel.text = "";
                
                if (combineButton != null)
                    combineButton.SetEnabled(false);
                
                return;
            }

            // Update selected material label
            if (selectedMaterialLabel != null)
            {
                selectedMaterialLabel.text = $"{selectedMaterial.name} ({selectedMaterial.rarity})";
            }

            // Update material details
            if (materialDetailsLabel != null)
            {
                int owned = materialService.GetMaterialQuantity(selectedMaterial.id);
                materialDetailsLabel.text = $"Category: {selectedMaterial.category}\n" +
                                          $"Value: {selectedMaterial.currentValue}g\n" +
                                          $"Owned: {owned}";
            }

            // Update recipe display
            if (selectedMaterial.combinationRecipe != null && selectedMaterial.combinationRecipe.Count > 0)
            {
                UpdateRecipeDisplay();
                UpdateCombinationDisplay();
            }
            else
            {
                if (recipeListView != null)
                {
                    recipeListView.itemsSource = null;
                    recipeListView.Rebuild();
                }
                
                if (combineButton != null)
                    combineButton.SetEnabled(false);
            }
        }

        private void UpdateRecipeDisplay()
        {
            if (selectedMaterial == null || selectedMaterial.combinationRecipe == null)
                return;

            if (recipeListView != null)
            {
                recipeListView.itemsSource = selectedMaterial.combinationRecipe.ToList();
                recipeListView.Rebuild();
            }
        }

        private void UpdateCombinationDisplay()
        {
            if (selectedMaterial == null)
                return;

            // Calculate combination value
            int valueGained = materialService.CalculateCombinationValue(selectedMaterial.id, combineQuantity);
            
            if (combinationValueLabel != null)
            {
                combinationValueLabel.text = $"Value Gain: {valueGained}g";
                combinationValueLabel.style.color = valueGained >= 0 ? Color.green : Color.red;
            }

            // Check if can combine
            Dictionary<string, int> scaledRequirements = new Dictionary<string, int>();
            foreach (var ingredient in selectedMaterial.combinationRecipe)
            {
                scaledRequirements[ingredient.Key] = ingredient.Value * combineQuantity;
            }

            bool canCombine = materialService.HasMaterials(scaledRequirements);
            
            if (combineButton != null)
            {
                combineButton.SetEnabled(canCombine);
            }

            if (combinationCostLabel != null)
            {
                combinationCostLabel.text = canCombine ? "Can craft" : "Insufficient materials";
                combinationCostLabel.style.color = canCombine ? Color.green : Color.red;
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Subscribe<MaterialAcquiredEvent>(OnMaterialAcquired);
                EventSystem.Instance.Subscribe<MaterialCombinedEvent>(OnMaterialCombined);
                EventSystem.Instance.Subscribe<MaterialQuantityChangedEvent>(OnMaterialQuantityChanged);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Unsubscribe<MaterialAcquiredEvent>(OnMaterialAcquired);
                EventSystem.Instance.Unsubscribe<MaterialCombinedEvent>(OnMaterialCombined);
                EventSystem.Instance.Unsubscribe<MaterialQuantityChangedEvent>(OnMaterialQuantityChanged);
            }
        }

        private void OnMaterialAcquired(MaterialAcquiredEvent evt)
        {
            Debug.Log($"UI: Material acquired - {evt.MaterialName} x{evt.Quantity}");
            RefreshUI();
        }

        private void OnMaterialCombined(MaterialCombinedEvent evt)
        {
            Debug.Log($"UI: Material combined - {evt.TargetMaterialName} x{evt.Quantity}");
            RefreshUI();
        }

        private void OnMaterialQuantityChanged(MaterialQuantityChangedEvent evt)
        {
            RefreshUI();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Filter inventory by category
        /// </summary>
        public void FilterByCategory(string category)
        {
            var inventory = materialService.GetPlayerInventory();
            
            if (string.IsNullOrEmpty(category))
            {
                inventoryMaterials = inventory.Values
                    .Where(m => m.quantity > 0)
                    .OrderBy(m => m.rarity)
                    .ToList();
            }
            else
            {
                inventoryMaterials = inventory.Values
                    .Where(m => m.quantity > 0 && m.category == category)
                    .OrderBy(m => m.rarity)
                    .ToList();
            }

            if (inventoryListView != null)
            {
                inventoryListView.itemsSource = inventoryMaterials;
                inventoryListView.Rebuild();
            }
        }

        /// <summary>
        /// Sort inventory by value
        /// </summary>
        public void SortByValue(bool descending = true)
        {
            var inventory = materialService.GetPlayerInventory();
            
            if (descending)
            {
                inventoryMaterials = inventory.Values
                    .Where(m => m.quantity > 0)
                    .OrderByDescending(m => m.currentValue * m.quantity)
                    .ToList();
            }
            else
            {
                inventoryMaterials = inventory.Values
                    .Where(m => m.quantity > 0)
                    .OrderBy(m => m.currentValue * m.quantity)
                    .ToList();
            }

            if (inventoryListView != null)
            {
                inventoryListView.itemsSource = inventoryMaterials;
                inventoryListView.Rebuild();
            }
        }

        #endregion
    }
}
