using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Service for managing material trading, synthesis, and inventory
    /// Handles buy/sell operations, material combinations, and market fluctuations
    /// </summary>
    public class MaterialService
    {
        private GameManager gameManager;
        private Dictionary<string, Material> materialRegistry;
        private Dictionary<string, Material> playerInventory;
        private System.Random random;

        public MaterialService(GameManager manager)
        {
            gameManager = manager;
            materialRegistry = new Dictionary<string, Material>();
            playerInventory = new Dictionary<string, Material>();
            random = new System.Random();
        }

        #region Material Registration

        /// <summary>
        /// Register a material type in the system
        /// </summary>
        public void RegisterMaterial(Material material)
        {
            if (material == null || !material.IsValid())
            {
                Debug.LogError("Cannot register invalid material");
                return;
            }

            if (materialRegistry.ContainsKey(material.id))
            {
                Debug.LogWarning($"Material {material.name} already registered");
                return;
            }

            materialRegistry[material.id] = material;
            Debug.Log($"Registered material: {material.name} (ID: {material.id})");
        }

        /// <summary>
        /// Get material from registry by ID
        /// </summary>
        public Material GetMaterial(string materialId)
        {
            return materialRegistry.ContainsKey(materialId) ? materialRegistry[materialId] : null;
        }

        /// <summary>
        /// Get all registered materials
        /// </summary>
        public List<Material> GetAllMaterials()
        {
            return materialRegistry.Values.ToList();
        }

        #endregion

        #region Inventory Management

        /// <summary>
        /// Add material to player inventory
        /// </summary>
        public void AddMaterial(Material material, int quantity)
        {
            if (material == null || quantity <= 0)
            {
                Debug.LogError("Invalid material or quantity");
                return;
            }

            if (!playerInventory.ContainsKey(material.id))
            {
                // Create new inventory entry
                playerInventory[material.id] = new Material(material.name, material.rarity, material.baseValue)
                {
                    id = material.id,
                    category = material.category,
                    currentValue = material.currentValue,
                    combinationRecipe = material.combinationRecipe
                };
            }

            playerInventory[material.id].AddQuantity(quantity);
            
            EventSystem.Instance.Publish(new MaterialAcquiredEvent
            {
                MaterialId = material.id,
                MaterialName = material.name,
                Quantity = quantity
            });

            Debug.Log($"Added {quantity}x {material.name} to inventory");
        }

        /// <summary>
        /// Remove material from player inventory
        /// </summary>
        public bool RemoveMaterial(string materialId, int quantity)
        {
            if (!playerInventory.ContainsKey(materialId))
            {
                Debug.LogError($"Material {materialId} not in inventory");
                return false;
            }

            return playerInventory[materialId].RemoveQuantity(quantity);
        }

        /// <summary>
        /// Get player's material inventory
        /// </summary>
        public Dictionary<string, Material> GetPlayerInventory()
        {
            return new Dictionary<string, Material>(playerInventory);
        }

        /// <summary>
        /// Get quantity of specific material in inventory
        /// </summary>
        public int GetMaterialQuantity(string materialId)
        {
            return playerInventory.ContainsKey(materialId) ? playerInventory[materialId].quantity : 0;
        }

        /// <summary>
        /// Check if player has sufficient materials
        /// </summary>
        public bool HasMaterials(Dictionary<string, int> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (GetMaterialQuantity(requirement.Key) < requirement.Value)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Buy/Sell Operations

        /// <summary>
        /// Buy material from market
        /// </summary>
        public bool BuyMaterial(string materialId, int quantity)
        {
            if (!materialRegistry.ContainsKey(materialId))
            {
                Debug.LogError($"Material {materialId} not found in registry");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogError("Invalid quantity");
                return false;
            }

            Material material = materialRegistry[materialId];
            int totalCost = material.currentValue * quantity;

            // Check if player has enough gold
            if (gameManager.gold < totalCost)
            {
                Debug.LogWarning($"Insufficient gold to buy {quantity}x {material.name}. Need {totalCost}, have {gameManager.gold}");
                return false;
            }

            // Deduct gold
            gameManager.ModifyGold(-totalCost);

            // Add material to inventory
            AddMaterial(material, quantity);

            EventSystem.Instance.Publish(new MaterialPurchasedEvent
            {
                MaterialId = materialId,
                MaterialName = material.name,
                Quantity = quantity,
                TotalCost = totalCost
            });

            Debug.Log($"Purchased {quantity}x {material.name} for {totalCost} gold");
            return true;
        }

        /// <summary>
        /// Sell material to market
        /// </summary>
        public bool SellMaterial(string materialId, int quantity)
        {
            if (!playerInventory.ContainsKey(materialId))
            {
                Debug.LogError($"Material {materialId} not in inventory");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogError("Invalid quantity");
                return false;
            }

            Material material = playerInventory[materialId];
            
            // Check if player has enough materials
            if (material.quantity < quantity)
            {
                Debug.LogWarning($"Insufficient materials to sell. Have {material.quantity}, trying to sell {quantity}");
                return false;
            }

            int totalRevenue = material.currentValue * quantity;

            // Remove material from inventory
            if (!RemoveMaterial(materialId, quantity))
            {
                return false;
            }

            // Add gold
            gameManager.ModifyGold(totalRevenue);

            EventSystem.Instance.Publish(new MaterialSoldEvent
            {
                MaterialId = materialId,
                MaterialName = material.name,
                Quantity = quantity,
                TotalRevenue = totalRevenue
            });

            Debug.Log($"Sold {quantity}x {material.name} for {totalRevenue} gold");
            return true;
        }

        #endregion

        #region Material Combination/Synthesis

        /// <summary>
        /// Combine materials to create new material
        /// </summary>
        public bool CombineMaterials(string targetMaterialId, int quantity)
        {
            if (!materialRegistry.ContainsKey(targetMaterialId))
            {
                Debug.LogError($"Target material {targetMaterialId} not found in registry");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogError("Invalid quantity");
                return false;
            }

            Material targetMaterial = materialRegistry[targetMaterialId];

            if (targetMaterial.combinationRecipe == null || targetMaterial.combinationRecipe.Count == 0)
            {
                Debug.LogError($"Material {targetMaterial.name} has no combination recipe");
                return false;
            }

            // Scale requirements by quantity
            Dictionary<string, int> scaledRequirements = new Dictionary<string, int>();
            foreach (var ingredient in targetMaterial.combinationRecipe)
            {
                scaledRequirements[ingredient.Key] = ingredient.Value * quantity;
            }

            // Check if player has all required materials
            if (!HasMaterials(scaledRequirements))
            {
                Debug.LogWarning($"Insufficient materials to combine {quantity}x {targetMaterial.name}");
                return false;
            }

            // Consume required materials
            foreach (var ingredient in scaledRequirements)
            {
                if (!RemoveMaterial(ingredient.Key, ingredient.Value))
                {
                    Debug.LogError($"Failed to remove ingredient {ingredient.Key}");
                    return false;
                }
            }

            // Add combined material
            AddMaterial(targetMaterial, quantity);

            EventSystem.Instance.Publish(new MaterialCombinedEvent
            {
                TargetMaterialId = targetMaterialId,
                TargetMaterialName = targetMaterial.name,
                Quantity = quantity
            });

            Debug.Log($"Combined materials to create {quantity}x {targetMaterial.name}");
            return true;
        }

        /// <summary>
        /// Calculate value gained from combination
        /// </summary>
        public int CalculateCombinationValue(string targetMaterialId, int quantity)
        {
            if (!materialRegistry.ContainsKey(targetMaterialId))
            {
                return 0;
            }

            Material targetMaterial = materialRegistry[targetMaterialId];
            int outputValue = targetMaterial.currentValue * quantity;

            int inputCost = 0;
            foreach (var ingredient in targetMaterial.combinationRecipe)
            {
                if (materialRegistry.ContainsKey(ingredient.Key))
                {
                    inputCost += materialRegistry[ingredient.Key].currentValue * ingredient.Value * quantity;
                }
            }

            return outputValue - inputCost;
        }

        #endregion

        #region Market Fluctuation

        /// <summary>
        /// Apply market fluctuation to a material's current value
        /// </summary>
        public void ApplyMarketFluctuation(string materialId, float fluctuationPercent)
        {
            if (!materialRegistry.ContainsKey(materialId))
            {
                Debug.LogError($"Material {materialId} not found in registry");
                return;
            }

            Material material = materialRegistry[materialId];
            int oldValue = material.currentValue;
            material.UpdateMarketValue(fluctuationPercent);

            Debug.Log($"Market fluctuation for {material.name}: {oldValue} -> {material.currentValue} ({fluctuationPercent:P0})");

            EventSystem.Instance.Publish(new MarketFluctuationEvent
            {
                MaterialId = materialId,
                MaterialName = material.name,
                OldValue = oldValue,
                NewValue = material.currentValue,
                FluctuationPercent = fluctuationPercent
            });
        }

        /// <summary>
        /// Apply random market fluctuations to all materials
        /// </summary>
        public void ApplyRandomMarketFluctuations()
        {
            foreach (var material in materialRegistry.Values)
            {
                // Random fluctuation between -20% and +20%
                float fluctuation = (float)(random.NextDouble() * 0.4 - 0.2);
                ApplyMarketFluctuation(material.id, fluctuation);
            }
        }

        /// <summary>
        /// Reset material to base value
        /// </summary>
        public void ResetMaterialValue(string materialId)
        {
            if (!materialRegistry.ContainsKey(materialId))
            {
                Debug.LogError($"Material {materialId} not found in registry");
                return;
            }

            Material material = materialRegistry[materialId];
            material.currentValue = material.baseValue;
            Debug.Log($"Reset {material.name} to base value: {material.baseValue}");
        }

        #endregion

        #region Material Filtering and Search

        /// <summary>
        /// Get materials by category
        /// </summary>
        public List<Material> GetMaterialsByCategory(string category)
        {
            return materialRegistry.Values
                .Where(m => m.category == category)
                .ToList();
        }

        /// <summary>
        /// Get materials by rarity
        /// </summary>
        public List<Material> GetMaterialsByRarity(MaterialRarity rarity)
        {
            return materialRegistry.Values
                .Where(m => m.rarity == rarity)
                .ToList();
        }

        /// <summary>
        /// Get craftable materials (those with recipes and sufficient ingredients)
        /// </summary>
        public List<Material> GetCraftableMaterials()
        {
            return materialRegistry.Values
                .Where(m => m.combinationRecipe != null && 
                           m.combinationRecipe.Count > 0 && 
                           HasMaterials(m.combinationRecipe))
                .ToList();
        }

        #endregion
    }

    #region Material Events

    public struct MaterialAcquiredEvent
    {
        public string MaterialId;
        public string MaterialName;
        public int Quantity;
    }

    public struct MaterialPurchasedEvent
    {
        public string MaterialId;
        public string MaterialName;
        public int Quantity;
        public int TotalCost;
    }

    public struct MaterialSoldEvent
    {
        public string MaterialId;
        public string MaterialName;
        public int Quantity;
        public int TotalRevenue;
    }

    public struct MaterialCombinedEvent
    {
        public string TargetMaterialId;
        public string TargetMaterialName;
        public int Quantity;
    }

    public struct MarketFluctuationEvent
    {
        public string MaterialId;
        public string MaterialName;
        public int OldValue;
        public int NewValue;
        public float FluctuationPercent;
    }

    #endregion
}
