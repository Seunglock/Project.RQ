using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryListContainer;
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private TMP_Text totalValueLabel;
        [SerializeField] private ScrollRect inventoryScrollView;

        [Header("Selected Material Details")]
        [SerializeField] private TMP_Text selectedMaterialLabel;
        [SerializeField] private TMP_Text materialDetailsLabel;
        [SerializeField] private Transform recipeListContainer;
        [SerializeField] private GameObject recipeItemPrefab;

        [Header("Combination UI")]
        [SerializeField] private TMP_InputField combineQuantityField;
        [SerializeField] private Button combineButton;
        [SerializeField] private TMP_Text combinationCostLabel;
        [SerializeField] private TMP_Text combinationValueLabel;

        [Header("Craftable Materials")]
        [SerializeField] private Transform craftableListContainer;
        [SerializeField] private GameObject craftableItemPrefab;
        [SerializeField] private ScrollRect craftableScrollView;

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
            // Setup combine quantity field
            if (combineQuantityField != null)
            {
                combineQuantityField.text = "1";
                combineQuantityField.onValueChanged.AddListener(OnCombineQuantityChanged);
            }

            // Setup combine button
            if (combineButton != null)
            {
                combineButton.onClick.AddListener(OnCombineClicked);
            }

            Debug.Log("MaterialInventoryUI setup complete");
        }

        #endregion

        #region Event Handlers

        private void OnInventoryItemSelected(Material material)
        {
            selectedMaterial = material;
            UpdateSelectedMaterialDisplay();
        }

        private void OnCraftableItemSelected(Material material)
        {
            selectedMaterial = material;
            UpdateSelectedMaterialDisplay();
        }

        private void OnCombineQuantityChanged(string value)
        {
            if (int.TryParse(value, out int quantity))
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
            if (inventoryListContainer == null) return;

            // Clear existing items
            foreach (Transform child in inventoryListContainer)
            {
                Destroy(child.gameObject);
            }

            var inventory = materialService.GetPlayerInventory();
            inventoryMaterials = inventory.Values
                .Where(m => m.quantity > 0)
                .OrderBy(m => m.rarity)
                .ThenBy(m => m.name)
                .ToList();

            // Create inventory items
            foreach (var material in inventoryMaterials)
            {
                CreateInventoryItem(material);
            }

            Debug.Log($"Updated inventory: {inventoryMaterials.Count} materials");
        }

        private void CreateInventoryItem(Material material)
        {
            GameObject item;

            if (inventoryItemPrefab != null)
            {
                item = Instantiate(inventoryItemPrefab, inventoryListContainer);
            }
            else
            {
                // Create item programmatically
                item = new GameObject("InventoryItem");
                item.transform.SetParent(inventoryListContainer, false);

                var layoutGroup = item.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = 10f;

                // Name
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(item.transform, false);
                TMP_Text nameText = nameObj.AddComponent<TMP_Text>();
                nameText.text = material.name;
                nameText.fontSize = 14;

                // Quantity
                GameObject quantityObj = new GameObject("Quantity");
                quantityObj.transform.SetParent(item.transform, false);
                TMP_Text quantityText = quantityObj.AddComponent<TMP_Text>();
                quantityText.text = $"x{material.quantity}";
                quantityText.fontSize = 14;

                // Value
                GameObject valueObj = new GameObject("Value");
                valueObj.transform.SetParent(item.transform, false);
                TMP_Text valueText = valueObj.AddComponent<TMP_Text>();
                int totalValue = material.currentValue * material.quantity;
                valueText.text = $"{totalValue}g";
                valueText.fontSize = 14;
                valueText.color = Color.yellow;
            }

            // Add button for selection
            var button = item.GetComponent<Button>();
            if (button == null)
            {
                button = item.AddComponent<Button>();
            }
            button.onClick.AddListener(() => OnInventoryItemSelected(material));
        }

        private void UpdateCraftableList()
        {
            if (craftableListContainer == null) return;

            // Clear existing items
            foreach (Transform child in craftableListContainer)
            {
                Destroy(child.gameObject);
            }

            craftableMaterials = materialService.GetCraftableMaterials();

            // Create craftable items
            foreach (var material in craftableMaterials)
            {
                CreateCraftableItem(material);
            }

            Debug.Log($"Craftable materials: {craftableMaterials.Count}");
        }

        private void CreateCraftableItem(Material material)
        {
            GameObject item;

            if (craftableItemPrefab != null)
            {
                item = Instantiate(craftableItemPrefab, craftableListContainer);
            }
            else
            {
                // Create item programmatically
                item = new GameObject("CraftableItem");
                item.transform.SetParent(craftableListContainer, false);

                var layoutGroup = item.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = 10f;

                // Name
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(item.transform, false);
                TMP_Text nameText = nameObj.AddComponent<TMP_Text>();
                nameText.text = material.name;
                nameText.fontSize = 14;

                // Rarity
                GameObject rarityObj = new GameObject("Rarity");
                rarityObj.transform.SetParent(item.transform, false);
                TMP_Text rarityText = rarityObj.AddComponent<TMP_Text>();
                rarityText.text = material.rarity.ToString();
                rarityText.fontSize = 14;

                // Value
                GameObject valueObj = new GameObject("Value");
                valueObj.transform.SetParent(item.transform, false);
                TMP_Text valueText = valueObj.AddComponent<TMP_Text>();
                valueText.text = $"{material.currentValue}g";
                valueText.fontSize = 14;
                valueText.color = Color.yellow;
            }

            // Add button for selection
            var button = item.GetComponent<Button>();
            if (button == null)
            {
                button = item.AddComponent<Button>();
            }
            button.onClick.AddListener(() => OnCraftableItemSelected(material));
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
                    combineButton.interactable = false;

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
                if (recipeListContainer != null)
                {
                    foreach (Transform child in recipeListContainer)
                    {
                        Destroy(child.gameObject);
                    }
                }

                if (combineButton != null)
                    combineButton.interactable = false;
            }
        }

        private void UpdateRecipeDisplay()
        {
            if (selectedMaterial == null || selectedMaterial.combinationRecipe == null)
                return;

            if (recipeListContainer == null)
                return;

            // Clear existing recipe items
            foreach (Transform child in recipeListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create recipe items
            foreach (var ingredient in selectedMaterial.combinationRecipe)
            {
                CreateRecipeItem(ingredient.Key, ingredient.Value);
            }
        }

        private void CreateRecipeItem(string materialId, int requiredQuantity)
        {
            GameObject item;

            if (recipeItemPrefab != null)
            {
                item = Instantiate(recipeItemPrefab, recipeListContainer);
            }
            else
            {
                // Create item programmatically
                item = new GameObject("RecipeItem");
                item.transform.SetParent(recipeListContainer, false);

                TMP_Text recipeText = item.AddComponent<TMP_Text>();
                recipeText.fontSize = 14;

                // Get material name from registry
                Material ingredientMaterial = materialService.GetMaterial(materialId);
                string materialName = ingredientMaterial != null ? ingredientMaterial.name : materialId;

                int owned = materialService.GetMaterialQuantity(materialId);

                recipeText.text = $"{materialName}: {owned}/{requiredQuantity}";

                // Color code based on availability
                if (owned >= requiredQuantity)
                {
                    recipeText.color = Color.green;
                }
                else
                {
                    recipeText.color = Color.red;
                }
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
                combinationValueLabel.color = valueGained >= 0 ? Color.green : Color.red;
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
                combineButton.interactable = canCombine;
            }

            if (combinationCostLabel != null)
            {
                combinationCostLabel.text = canCombine ? "Can craft" : "Insufficient materials";
                combinationCostLabel.color = canCombine ? Color.green : Color.red;
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

            UpdateInventoryList();
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

            UpdateInventoryList();
        }

        #endregion
    }
}
