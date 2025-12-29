using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using GuildReceptionist;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Unit tests for Party model stat calculations and validation
    /// </summary>
    [TestFixture]
    public class PartyTests
    {
        private Party party;

        [SetUp]
        public void Setup()
        {
            party = new Party("Test Party");
        }

        [Test]
        public void Party_Constructor_InitializesCorrectly()
        {
            Assert.IsNotNull(party.id);
            Assert.AreEqual("Test Party", party.name);
            Assert.AreEqual(50, party.loyalty);
            Assert.IsTrue(party.isAvailable);
            Assert.IsNotNull(party.stats);
            Assert.AreEqual(3, party.stats.Count);
            Assert.IsNotNull(party.equipment);
            Assert.IsNotNull(party.specializations);
        }

        [Test]
        public void Party_IsValid_WithValidStats_ReturnsTrue()
        {
            party.stats[StatType.Exploration] = 10;
            party.stats[StatType.Combat] = 10;
            party.stats[StatType.Admin] = 10;
            party.loyalty = 50;

            Assert.IsTrue(party.IsValid());
        }

        [Test]
        [TestCase(0)]
        [TestCase(21)]
        [TestCase(-1)]
        public void Party_IsValid_WithInvalidStats_ReturnsFalse(int invalidStatValue)
        {
            party.stats[StatType.Exploration] = invalidStatValue;

            // Expect error log
            LogAssert.Expect(LogType.Error, $"Party {party.name}: Invalid stat value Exploration={invalidStatValue}");

            Assert.IsFalse(party.IsValid());
        }

        [Test]
        [TestCase(-1)]
        [TestCase(101)]
        public void Party_IsValid_WithInvalidLoyalty_ReturnsFalse(int invalidLoyalty)
        {
            party.loyalty = invalidLoyalty;

            // Expect error log
            LogAssert.Expect(LogType.Error, $"Party {party.name}: Invalid loyalty {invalidLoyalty}");

            Assert.IsFalse(party.IsValid());
        }

        [Test]
        public void Party_GetEffectiveStat_WithoutEquipment_ReturnsBaseStat()
        {
            party.stats[StatType.Exploration] = 10;
            
            int effectiveStat = party.GetEffectiveStat(StatType.Exploration);
            
            Assert.AreEqual(10, effectiveStat);
        }

        [Test]
        public void Party_GetEffectiveStat_WithEquipment_ReturnsBaseStatPlusBonus()
        {
            party.stats[StatType.Combat] = 10;
            
            var equipment = new Equipment("Test Sword", 100);
            equipment.statBonuses[StatType.Combat] = 5;
            party.AddEquipment(equipment);
            
            int effectiveStat = party.GetEffectiveStat(StatType.Combat);
            
            Assert.AreEqual(15, effectiveStat);
        }

        [Test]
        public void Party_GetEffectiveStat_ClampedAtMaxValue()
        {
            party.stats[StatType.Combat] = 20; // Max stat value
            
            var equipment = new Equipment("Overpowered Sword", 1000);
            equipment.statBonuses[StatType.Combat] = 10;
            party.AddEquipment(equipment);
            
            int effectiveStat = party.GetEffectiveStat(StatType.Combat);
            
            Assert.AreEqual(20, effectiveStat); // Should be clamped at max
        }

        [Test]
        public void Party_MeetsRequirements_WithSufficientStats_ReturnsTrue()
        {
            party.stats[StatType.Exploration] = 10;
            party.stats[StatType.Combat] = 8;
            
            var requirements = new Dictionary<StatType, int>
            {
                { StatType.Exploration, 8 },
                { StatType.Combat, 6 }
            };
            
            Assert.IsTrue(party.MeetsRequirements(requirements));
        }

        [Test]
        public void Party_MeetsRequirements_WithInsufficientStats_ReturnsFalse()
        {
            party.stats[StatType.Exploration] = 5;
            party.stats[StatType.Combat] = 8;
            
            var requirements = new Dictionary<StatType, int>
            {
                { StatType.Exploration, 10 },
                { StatType.Combat, 6 }
            };
            
            Assert.IsFalse(party.MeetsRequirements(requirements));
        }

        [Test]
        public void Party_CalculateSuccessRate_WithPerfectStats_ReturnsHighRate()
        {
            party.stats[StatType.Exploration] = 15;
            party.stats[StatType.Combat] = 10;
            party.loyalty = 100;
            
            var quest = new Quest
            {
                difficulty = 3,
                requiredStats = new Dictionary<StatType, int>
                {
                    { StatType.Exploration, 10 },
                    { StatType.Combat, 5 }
                }
            };
            
            float successRate = party.CalculateSuccessRate(quest);
            
            Assert.Greater(successRate, 0.8f);
        }

        [Test]
        public void Party_CalculateSuccessRate_WithLowStats_ReturnsLowRate()
        {
            party.stats[StatType.Exploration] = 3;
            party.stats[StatType.Combat] = 2;
            party.loyalty = 20;
            
            var quest = new Quest
            {
                difficulty = 4,
                requiredStats = new Dictionary<StatType, int>
                {
                    { StatType.Exploration, 12 },
                    { StatType.Combat, 10 }
                }
            };
            
            float successRate = party.CalculateSuccessRate(quest);
            
            Assert.Less(successRate, 0.3f);
        }

        [Test]
        public void Party_CalculateSuccessRate_WithNullQuest_ReturnsZero()
        {
            float successRate = party.CalculateSuccessRate(null);
            
            Assert.AreEqual(0f, successRate);
        }

        [Test]
        public void Party_AddEquipment_IncreasesEquipmentCount()
        {
            var equipment = new Equipment("Test Equipment", 50);
            
            party.AddEquipment(equipment);
            
            Assert.AreEqual(1, party.equipment.Count);
        }

        [Test]
        public void Party_AddEquipment_WithNull_DoesNothing()
        {
            party.AddEquipment(null);
            
            Assert.AreEqual(0, party.equipment.Count);
        }

        [Test]
        public void Party_RemoveEquipment_WithValidId_ReturnsTrue()
        {
            var equipment = new Equipment("Test Equipment", 50);
            party.AddEquipment(equipment);
            
            bool removed = party.RemoveEquipment(equipment.id);
            
            Assert.IsTrue(removed);
            Assert.AreEqual(0, party.equipment.Count);
        }

        [Test]
        public void Party_RemoveEquipment_WithInvalidId_ReturnsFalse()
        {
            bool removed = party.RemoveEquipment("invalid-id");
            
            Assert.IsFalse(removed);
        }

        [Test]
        public void Party_AddExperience_ImprovesStatsEvery100Points()
        {
            party.stats[StatType.Exploration] = 5;
            party.stats[StatType.Combat] = 5;
            party.stats[StatType.Admin] = 5;
            
            int initialTotalStats = party.stats[StatType.Exploration] + 
                                  party.stats[StatType.Combat] + 
                                  party.stats[StatType.Admin];
            
            party.AddExperience(100);
            
            int finalTotalStats = party.stats[StatType.Exploration] + 
                                party.stats[StatType.Combat] + 
                                party.stats[StatType.Admin];
            
            Assert.AreEqual(initialTotalStats + 1, finalTotalStats);
        }

        [Test]
        public void Party_AddExperience_DoesNotExceedMaxStats()
        {
            party.stats[StatType.Exploration] = 20; // Already at max
            party.stats[StatType.Combat] = 20;
            party.stats[StatType.Admin] = 20;
            
            party.AddExperience(300);
            
            Assert.AreEqual(20, party.stats[StatType.Exploration]);
            Assert.AreEqual(20, party.stats[StatType.Combat]);
            Assert.AreEqual(20, party.stats[StatType.Admin]);
        }

        [Test]
        public void Party_ModifyLoyalty_IncreasesClampsAt100()
        {
            party.loyalty = 90;
            
            party.ModifyLoyalty(20);
            
            Assert.AreEqual(100, party.loyalty);
        }

        [Test]
        public void Party_ModifyLoyalty_DecreasesClampsAt0()
        {
            party.loyalty = 10;
            
            party.ModifyLoyalty(-20);
            
            Assert.AreEqual(0, party.loyalty);
        }

        [Test]
        public void Party_ModifyLoyalty_BelowThreshold_MakesUnavailable()
        {
            party.loyalty = 25;
            party.isAvailable = true;
            
            party.ModifyLoyalty(-10); // Should go below 20 threshold
            
            Assert.IsFalse(party.isAvailable);
        }

        [Test]
        public void Party_UpdateAvailability_WithHighLoyalty_BecomesAvailable()
        {
            party.loyalty = 50;
            party.isAvailable = false;
            
            party.UpdateAvailability();
            
            Assert.IsTrue(party.isAvailable);
        }

        [Test]
        public void Party_UpdateAvailability_WithLowLoyalty_BecomesUnavailable()
        {
            party.loyalty = 15;
            party.isAvailable = true;
            
            party.UpdateAvailability();
            
            Assert.IsFalse(party.isAvailable);
        }

        [Test]
        public void Equipment_Constructor_InitializesCorrectly()
        {
            var equipment = new Equipment("Test Armor", 150);
            
            Assert.IsNotNull(equipment.id);
            Assert.AreEqual("Test Armor", equipment.name);
            Assert.AreEqual(150, equipment.cost);
            Assert.IsNotNull(equipment.statBonuses);
        }

        [Test]
        public void Party_MultipleEquipment_StackStatBonuses()
        {
            party.stats[StatType.Combat] = 5;
            
            var weapon = new Equipment("Sword", 100);
            weapon.statBonuses[StatType.Combat] = 3;
            
            var armor = new Equipment("Armor", 100);
            armor.statBonuses[StatType.Combat] = 2;
            
            party.AddEquipment(weapon);
            party.AddEquipment(armor);
            
            int effectiveStat = party.GetEffectiveStat(StatType.Combat);
            
            Assert.AreEqual(10, effectiveStat); // 5 base + 3 + 2
        }
    }
}
