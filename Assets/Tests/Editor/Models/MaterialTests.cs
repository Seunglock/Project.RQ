using NUnit.Framework;
using GuildReceptionist;
using System.Collections.Generic;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Unit tests for Material model combination logic and validation
    /// Tests material creation, combination recipes, value calculations, and market mechanics
    /// </summary>
    [TestFixture]
    public class MaterialTests
    {
        [SetUp]
        public void Setup()
        {
            // EventSystem singleton will be accessed when needed
        }

        #region Material Creation and Validation Tests

        [Test]
        public void Material_Creation_ShouldInitializeWithValidData()
        {
            // Arrange & Act
            var material = new Material("Iron Ore", MaterialRarity.Common, 10);

            // Assert
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.id);
            Assert.AreEqual("Iron Ore", material.name);
            Assert.AreEqual(MaterialRarity.Common, material.rarity);
            Assert.AreEqual(10, material.baseValue);
            Assert.AreEqual(10, material.currentValue);
            Assert.AreEqual(0, material.quantity);
            Assert.IsNotNull(material.combinationRecipe);
        }

        [Test]
        public void Material_IsValid_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var material = new Material("Gold Ingot", MaterialRarity.Rare, 100);
            material.quantity = 5;

            // Act
            bool isValid = material.IsValid();

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void Material_IsValid_WithNegativeQuantity_ShouldReturnFalse()
        {
            // Arrange
            var material = new Material("Silver Ore", MaterialRarity.Uncommon, 50);
            material.quantity = -1;

            // Act
            bool isValid = material.IsValid();

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void Material_IsValid_WithNegativeValue_ShouldReturnFalse()
        {
            // Arrange
            var material = new Material("Copper Ore", MaterialRarity.Common, 5);
            material.baseValue = -10;

            // Act
            bool isValid = material.IsValid();

            // Assert
            Assert.IsFalse(isValid);
        }

        #endregion

        #region Quantity Management Tests

        [Test]
        public void Material_AddQuantity_ShouldIncreaseQuantity()
        {
            // Arrange
            var material = new Material("Wood", MaterialRarity.Common, 5);
            material.quantity = 10;

            // Act
            material.AddQuantity(5);

            // Assert
            Assert.AreEqual(15, material.quantity);
        }

        [Test]
        public void Material_RemoveQuantity_WithSufficientAmount_ShouldDecreaseQuantity()
        {
            // Arrange
            var material = new Material("Stone", MaterialRarity.Common, 3);
            material.quantity = 10;

            // Act
            bool result = material.RemoveQuantity(5);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(5, material.quantity);
        }

        [Test]
        public void Material_RemoveQuantity_WithInsufficientAmount_ShouldReturnFalse()
        {
            // Arrange
            var material = new Material("Diamond", MaterialRarity.Epic, 500);
            material.quantity = 3;

            // Act
            bool result = material.RemoveQuantity(5);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(3, material.quantity); // Quantity unchanged
        }

        #endregion

        #region Market Value Tests

        [Test]
        public void Material_UpdateMarketValue_WithPositiveFluctuation_ShouldIncreaseValue()
        {
            // Arrange
            var material = new Material("Gemstone", MaterialRarity.Rare, 100);

            // Act
            material.UpdateMarketValue(0.2f); // +20%

            // Assert
            Assert.AreEqual(120, material.currentValue);
        }

        [Test]
        public void Material_UpdateMarketValue_WithNegativeFluctuation_ShouldDecreaseValue()
        {
            // Arrange
            var material = new Material("Cloth", MaterialRarity.Common, 10);

            // Act
            material.UpdateMarketValue(-0.3f); // -30%

            // Assert
            Assert.AreEqual(7, material.currentValue);
        }

        [Test]
        public void Material_UpdateMarketValue_WithLargeNegativeFluctuation_ShouldNotGoBelowMinimum()
        {
            // Arrange
            var material = new Material("Scrap", MaterialRarity.Common, 2);

            // Act
            material.UpdateMarketValue(-0.99f); // -99%

            // Assert
            Assert.AreEqual(1, material.currentValue); // Minimum value of 1
        }

        #endregion

        #region Combination Recipe Tests

        [Test]
        public void Material_CombinationRecipe_ShouldBeEmptyByDefault()
        {
            // Arrange & Act
            var material = new Material("Basic Material", MaterialRarity.Common, 5);

            // Assert
            Assert.IsNotNull(material.combinationRecipe);
            Assert.AreEqual(0, material.combinationRecipe.Count);
        }

        [Test]
        public void Material_CombinationRecipe_CanAddRecipeIngredients()
        {
            // Arrange
            var material = new Material("Steel Ingot", MaterialRarity.Uncommon, 50);
            
            // Act
            material.combinationRecipe["iron_ore"] = 2;
            material.combinationRecipe["coal"] = 1;

            // Assert
            Assert.AreEqual(2, material.combinationRecipe.Count);
            Assert.AreEqual(2, material.combinationRecipe["iron_ore"]);
            Assert.AreEqual(1, material.combinationRecipe["coal"]);
        }

        [Test]
        public void Material_CombinationRecipe_CanHaveMultipleIngredients()
        {
            // Arrange
            var material = new Material("Magic Crystal", MaterialRarity.Epic, 300);
            
            // Act
            material.combinationRecipe["gemstone"] = 3;
            material.combinationRecipe["magic_essence"] = 5;
            material.combinationRecipe["gold_dust"] = 2;

            // Assert
            Assert.AreEqual(3, material.combinationRecipe.Count);
        }

        #endregion

        #region Material Rarity Tests

        [Test]
        public void Material_Rarity_ShouldAffectBaseValue()
        {
            // Arrange & Act
            var common = new Material("Common Item", MaterialRarity.Common, 10);
            var uncommon = new Material("Uncommon Item", MaterialRarity.Uncommon, 50);
            var rare = new Material("Rare Item", MaterialRarity.Rare, 150);
            var epic = new Material("Epic Item", MaterialRarity.Epic, 500);

            // Assert - Higher rarity should typically have higher base values
            Assert.IsTrue(common.baseValue < uncommon.baseValue);
            Assert.IsTrue(uncommon.baseValue < rare.baseValue);
            Assert.IsTrue(rare.baseValue < epic.baseValue);
        }

        #endregion

        #region Material Category Tests

        [Test]
        public void Material_Category_CanBeAssigned()
        {
            // Arrange
            var material = new Material("Iron Ore", MaterialRarity.Common, 10);
            
            // Act
            material.category = "Metal";

            // Assert
            Assert.AreEqual("Metal", material.category);
        }

        [Test]
        public void Material_Category_SupportsMultipleCategories()
        {
            // Arrange & Act
            var metal = new Material("Iron", MaterialRarity.Common, 10) { category = "Metal" };
            var herb = new Material("Lavender", MaterialRarity.Common, 5) { category = "Herb" };
            var gem = new Material("Ruby", MaterialRarity.Rare, 200) { category = "Gem" };

            // Assert
            Assert.AreEqual("Metal", metal.category);
            Assert.AreEqual("Herb", herb.category);
            Assert.AreEqual("Gem", gem.category);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Material_CompleteLifecycle_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var material = new Material("Complete Test Item", MaterialRarity.Rare, 100);
            material.category = "Test";
            material.combinationRecipe["base_material"] = 2;

            // Act - Simulate full lifecycle
            material.AddQuantity(10);
            material.UpdateMarketValue(0.5f); // +50% value
            bool removeSuccess = material.RemoveQuantity(3);
            
            // Assert
            Assert.IsTrue(removeSuccess);
            Assert.AreEqual(7, material.quantity);
            Assert.AreEqual(150, material.currentValue);
            Assert.IsTrue(material.IsValid());
            Assert.AreEqual(1, material.combinationRecipe.Count);
        }

        #endregion
    }
}
