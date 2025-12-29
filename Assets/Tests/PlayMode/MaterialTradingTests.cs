using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GuildReceptionist;
using System.Collections.Generic;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Integration tests for material trading and synthesis workflow
    /// Tests the complete flow: acquiring materials -> trading -> combining -> profit verification
    /// </summary>
    [TestFixture]
    public class MaterialTradingTests
    {
        private GameManager gameManager;
        private MaterialService materialService;
        private GameObject testObject;

        [SetUp]
        public void Setup()
        {
            // Access GameManager singleton (will create if not exists)
            gameManager = GameManager.Instance;

            // Start a new game to initialize game state
            gameManager.StartNewGame();

            // Initialize MaterialService
            if (gameManager.materialService == null)
            {
                gameManager.materialService = new MaterialService(gameManager);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up is handled by GameManager singleton lifecycle
        }

        #region Material Acquisition Tests

        [UnityTest]
        public IEnumerator MaterialTrading_AcquireMaterials_ShouldAddToInventory()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);

            // Act
            materialService.AddMaterial(ironOre, 5);
            yield return null;

            // Assert
            var inventory = materialService.GetPlayerInventory();
            Assert.IsTrue(inventory.ContainsKey(ironOre.id));
            Assert.AreEqual(5, inventory[ironOre.id].quantity);
        }

        [UnityTest]
        public IEnumerator MaterialTrading_AcquireMultipleMaterials_ShouldTrackAllTypes()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);
            var coal = new Material("Coal", MaterialRarity.Common, 5);
            var gemstone = new Material("Gemstone", MaterialRarity.Rare, 100);

            // Act
            materialService.AddMaterial(ironOre, 10);
            materialService.AddMaterial(coal, 5);
            materialService.AddMaterial(gemstone, 2);
            yield return null;

            // Assert
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(3, inventory.Count);
            Assert.AreEqual(10, inventory[ironOre.id].quantity);
            Assert.AreEqual(5, inventory[coal.id].quantity);
            Assert.AreEqual(2, inventory[gemstone.id].quantity);
        }

        #endregion

        #region Buy/Sell Trading Tests

        [UnityTest]
        public IEnumerator MaterialTrading_BuyMaterial_ShouldDeductGoldAndAddMaterial()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(1000 - gameManager.PlayerGold); // Set gold to 1000
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);
            materialService.RegisterMaterial(ironOre);

            // Act
            bool success = materialService.BuyMaterial(ironOre.id, 5);
            yield return null;

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(950, gameManager.PlayerGold); // 1000 - (10 * 5)
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(5, inventory[ironOre.id].quantity);
        }

        [UnityTest]
        public IEnumerator MaterialTrading_BuyMaterial_WithInsufficientGold_ShouldFail()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(20 - gameManager.PlayerGold); // Set gold to 20
            var expensiveMaterial = new Material("Diamond", MaterialRarity.Epic, 500);
            materialService.RegisterMaterial(expensiveMaterial);

            // Act
            bool success = materialService.BuyMaterial(expensiveMaterial.id, 1);
            yield return null;

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(20, gameManager.PlayerGold); // Gold unchanged
        }

        [UnityTest]
        public IEnumerator MaterialTrading_SellMaterial_ShouldAddGoldAndRemoveMaterial()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(100 - gameManager.PlayerGold); // Set gold to 100
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);
            materialService.RegisterMaterial(ironOre); // Register material for market pricing
            materialService.AddMaterial(ironOre, 5);

            // Act
            bool success = materialService.SellMaterial(ironOre.id, 3);
            yield return null;

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(130, gameManager.PlayerGold); // 100 + (10 * 3)
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(2, inventory[ironOre.id].quantity);
        }

        [UnityTest]
        public IEnumerator MaterialTrading_SellMaterial_WithInsufficientQuantity_ShouldFail()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(100 - gameManager.PlayerGold); // Set gold to 100
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);
            materialService.RegisterMaterial(ironOre); // Register material for market pricing
            materialService.AddMaterial(ironOre, 2);

            // Act
            bool success = materialService.SellMaterial(ironOre.id, 5);
            yield return null;

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(100, gameManager.PlayerGold); // Gold unchanged
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(2, inventory[ironOre.id].quantity); // Quantity unchanged
        }

        #endregion

        #region Material Combination/Synthesis Tests

        [UnityTest]
        public IEnumerator MaterialTrading_CombineMaterials_WithValidRecipe_ShouldCreateNewMaterial()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            
            // Create base materials
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10) { id = "iron_ore" };
            var coal = new Material("Coal", MaterialRarity.Common, 5) { id = "coal" };
            
            // Create target material with recipe
            var steelIngot = new Material("Steel Ingot", MaterialRarity.Uncommon, 50) { id = "steel_ingot" };
            steelIngot.combinationRecipe["iron_ore"] = 2;
            steelIngot.combinationRecipe["coal"] = 1;
            
            // Add materials to inventory
            materialService.AddMaterial(ironOre, 10);
            materialService.AddMaterial(coal, 5);
            materialService.RegisterMaterial(steelIngot);

            // Act
            bool success = materialService.CombineMaterials(steelIngot.id, 1);
            yield return null;

            // Assert
            Assert.IsTrue(success);
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(8, inventory[ironOre.id].quantity); // 10 - 2
            Assert.AreEqual(4, inventory[coal.id].quantity); // 5 - 1
            Assert.AreEqual(1, inventory[steelIngot.id].quantity);
        }

        [UnityTest]
        public IEnumerator MaterialTrading_CombineMaterials_WithInsufficientIngredients_ShouldFail()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10) { id = "iron_ore" };
            var coal = new Material("Coal", MaterialRarity.Common, 5) { id = "coal" };
            var steelIngot = new Material("Steel Ingot", MaterialRarity.Uncommon, 50) { id = "steel_ingot" };
            steelIngot.combinationRecipe["iron_ore"] = 2;
            steelIngot.combinationRecipe["coal"] = 1;
            
            // Add insufficient materials
            materialService.AddMaterial(ironOre, 1); // Not enough iron
            materialService.AddMaterial(coal, 5);
            materialService.RegisterMaterial(steelIngot);

            // Act
            bool success = materialService.CombineMaterials(steelIngot.id, 1);
            yield return null;

            // Assert
            Assert.IsFalse(success);
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(1, inventory[ironOre.id].quantity); // Unchanged
            Assert.AreEqual(5, inventory[coal.id].quantity); // Unchanged
            Assert.IsFalse(inventory.ContainsKey(steelIngot.id)); // Not created
        }

        [UnityTest]
        public IEnumerator MaterialTrading_CombineMultiple_ShouldScaleIngredientRequirements()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10) { id = "iron_ore" };
            var coal = new Material("Coal", MaterialRarity.Common, 5) { id = "coal" };
            var steelIngot = new Material("Steel Ingot", MaterialRarity.Uncommon, 50) { id = "steel_ingot" };
            steelIngot.combinationRecipe["iron_ore"] = 2;
            steelIngot.combinationRecipe["coal"] = 1;
            
            materialService.AddMaterial(ironOre, 10);
            materialService.AddMaterial(coal, 5);
            materialService.RegisterMaterial(steelIngot);

            // Act - Combine 3 times
            bool success = materialService.CombineMaterials(steelIngot.id, 3);
            yield return null;

            // Assert
            Assert.IsTrue(success);
            var inventory = materialService.GetPlayerInventory();
            Assert.AreEqual(4, inventory[ironOre.id].quantity); // 10 - (2 * 3) = 4
            Assert.AreEqual(2, inventory[coal.id].quantity); // 5 - (1 * 3) = 2
            Assert.AreEqual(3, inventory[steelIngot.id].quantity);
        }

        #endregion

        #region Market Fluctuation Tests

        [UnityTest]
        public IEnumerator MaterialTrading_MarketFluctuation_ShouldAffectBuySellPrices()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10);
            materialService.RegisterMaterial(ironOre);
            gameManager.ModifyGold(1000 - gameManager.PlayerGold); // Set gold to 1000

            // Act - Apply market fluctuation
            materialService.ApplyMarketFluctuation(ironOre.id, 0.5f); // +50%
            yield return null;

            // Buy at increased price
            bool buySuccess = materialService.BuyMaterial(ironOre.id, 2);
            
            // Assert
            Assert.IsTrue(buySuccess);
            Assert.AreEqual(970, gameManager.PlayerGold); // 1000 - (15 * 2) = 970 (15 = 10 * 1.5)
        }

        #endregion

        #region Complete Trading Workflow Tests

        [UnityTest]
        public IEnumerator MaterialTrading_CompleteWorkflow_BuyCombineSell_ShouldGenerateProfit()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(1000 - gameManager.PlayerGold); // Set gold to 1000
            
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10) { id = "iron_ore" };
            var coal = new Material("Coal", MaterialRarity.Common, 5) { id = "coal" };
            var steelIngot = new Material("Steel Ingot", MaterialRarity.Uncommon, 50) { id = "steel_ingot" };
            steelIngot.combinationRecipe["iron_ore"] = 2;
            steelIngot.combinationRecipe["coal"] = 1;
            
            materialService.RegisterMaterial(ironOre);
            materialService.RegisterMaterial(coal);
            materialService.RegisterMaterial(steelIngot);

            // Act
            // Step 1: Buy base materials
            materialService.BuyMaterial(ironOre.id, 4); // Cost: 40
            materialService.BuyMaterial(coal.id, 2); // Cost: 10
            yield return null;
            
            Assert.AreEqual(950, gameManager.PlayerGold); // 1000 - 50

            // Step 2: Combine materials
            bool combineSuccess = materialService.CombineMaterials(steelIngot.id, 2);
            yield return null;
            Assert.IsTrue(combineSuccess);

            // Step 3: Sell crafted materials
            bool sellSuccess = materialService.SellMaterial(steelIngot.id, 2); // Revenue: 100
            yield return null;

            // Assert - Should generate profit
            Assert.IsTrue(sellSuccess);
            Assert.AreEqual(1050, gameManager.PlayerGold); // 950 + 100 = 1050 (profit of 50)
        }

        [UnityTest]
        public IEnumerator MaterialTrading_CompleteWorkflow_WithMarketFluctuation_ShouldMaximizeProfit()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(1000 - gameManager.PlayerGold); // Set gold to 1000
            
            var ironOre = new Material("Iron Ore", MaterialRarity.Common, 10) { id = "iron_ore" };
            var steelIngot = new Material("Steel Ingot", MaterialRarity.Uncommon, 50) { id = "steel_ingot" };
            steelIngot.combinationRecipe["iron_ore"] = 2;
            
            materialService.RegisterMaterial(ironOre);
            materialService.RegisterMaterial(steelIngot);

            // Act
            // Buy when cheap (negative fluctuation)
            materialService.ApplyMarketFluctuation(ironOre.id, -0.3f); // -30%
            materialService.BuyMaterial(ironOre.id, 4); // Cost: 4 * 7 = 28
            yield return null;
            
            int goldAfterBuy = gameManager.PlayerGold;
            Assert.AreEqual(972, goldAfterBuy); // 1000 - 28

            // Combine
            materialService.CombineMaterials(steelIngot.id, 2);
            yield return null;

            // Sell when expensive (positive fluctuation)
            materialService.ApplyMarketFluctuation(steelIngot.id, 0.5f); // +50%
            materialService.SellMaterial(steelIngot.id, 2); // Revenue: 2 * 75 = 150
            yield return null;

            // Assert - Maximum profit through market timing
            Assert.AreEqual(1122, gameManager.PlayerGold); // 972 + 150 = 1122 (profit of 122)
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator MaterialTrading_InvalidMaterialId_ShouldHandleGracefully()
        {
            // Arrange
            materialService = new MaterialService(gameManager);
            gameManager.ModifyGold(1000 - gameManager.PlayerGold); // Set gold to 1000

            // Act & Assert - Should not throw exceptions
            LogAssert.Expect(LogType.Error, "Material invalid_id not found in registry");
            bool buyResult = materialService.BuyMaterial("invalid_id", 1);
            Assert.IsFalse(buyResult);

            LogAssert.Expect(LogType.Error, "Material invalid_id not in inventory");
            bool sellResult = materialService.SellMaterial("invalid_id", 1);
            Assert.IsFalse(sellResult);

            LogAssert.Expect(LogType.Error, "Target material invalid_id not found in registry");
            bool combineResult = materialService.CombineMaterials("invalid_id", 1);
            Assert.IsFalse(combineResult);

            yield return null;

            Assert.AreEqual(1000, gameManager.PlayerGold); // Gold unchanged
        }

        #endregion
    }
}
