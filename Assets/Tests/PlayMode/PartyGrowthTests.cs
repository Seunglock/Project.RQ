using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using GuildReceptionist;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Integration tests for party recruitment and growth workflow
    /// </summary>
    [TestFixture]
    public class PartyGrowthTests
    {
        private PartyService partyService;
        private GameManager gameManager;

        [SetUp]
        public void Setup()
        {
            // Initialize GameManager
            var go = new GameObject("GameManager");
            gameManager = go.AddComponent<GameManager>();
            gameManager.StartNewGame(); // Initialize game state

            // Initialize PartyService with explicit GameManager
            partyService = new PartyService(gameManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (gameManager != null && gameManager.gameObject != null)
            {
                Object.DestroyImmediate(gameManager.gameObject);
            }
        }

        private void SetGold(int targetAmount)
        {
            gameManager.ModifyGold(targetAmount - gameManager.PlayerGold);
        }

        [Test]
        public void PartyRecruitment_WithSufficientFunds_CreatesNewParty()
        {
            // Arrange
            SetGold(500);
            int initialPartyCount = partyService.GetAllParties().Count;

            // Act
            bool recruited = partyService.RecruitParty("Brave Adventurers", 300);

            // Assert
            Assert.IsTrue(recruited);
            Assert.AreEqual(initialPartyCount + 1, partyService.GetAllParties().Count);
            Assert.AreEqual(200, gameManager.PlayerGold); // 500 - 300
        }

        [Test]
        public void PartyRecruitment_WithInsufficientFunds_Fails()
        {
            // Arrange
            SetGold(100);
            int initialPartyCount = partyService.GetAllParties().Count;

            // Act
            bool recruited = partyService.RecruitParty("Expensive Party", 300);

            // Assert
            Assert.IsFalse(recruited);
            Assert.AreEqual(initialPartyCount, partyService.GetAllParties().Count);
            Assert.AreEqual(100, gameManager.PlayerGold); // Unchanged
        }

        [Test]
        public void PartyRecruitment_AtMaxCapacity_Fails()
        {
            // Arrange
            SetGold(10000);

            // Recruit maximum parties (5 according to constants)
            for (int i = 0; i < 5; i++)
            {
                partyService.RecruitParty($"Party {i}", 100);
            }

            int partyCountAtMax = partyService.GetAllParties().Count;

            // Act
            bool recruited = partyService.RecruitParty("Extra Party", 100);

            // Assert
            Assert.IsFalse(recruited);
            Assert.AreEqual(partyCountAtMax, partyService.GetAllParties().Count);
        }

        [Test]
        public void PartyTraining_ImprovesStats()
        {
            // Arrange
            SetGold(1000);
            partyService.RecruitParty("Training Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialExplorationStat = party.stats[StatType.Exploration];

            // Act
            bool trained = partyService.TrainParty(party.id, StatType.Exploration, 200);

            // Assert
            Assert.IsTrue(trained);
            Assert.Greater(party.stats[StatType.Exploration], initialExplorationStat);
            Assert.AreEqual(500, gameManager.PlayerGold); // 1000 - 300 - 200
        }

        [Test]
        public void PartyTraining_WithInsufficientFunds_Fails()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Training Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialExplorationStat = party.stats[StatType.Exploration];

            // Act
            bool trained = partyService.TrainParty(party.id, StatType.Exploration, 300);

            // Assert
            Assert.IsFalse(trained);
            Assert.AreEqual(initialExplorationStat, party.stats[StatType.Exploration]);
            Assert.AreEqual(200, gameManager.PlayerGold); // 500 - 300 from recruitment
        }

        [Test]
        public void PartyTraining_AtMaxStat_Fails()
        {
            // Arrange
            SetGold(1000);
            partyService.RecruitParty("Maxed Party", 300);

            Party party = partyService.GetAllParties()[0];
            party.stats[StatType.Exploration] = 20; // Max stat

            // Act
            bool trained = partyService.TrainParty(party.id, StatType.Exploration, 200);

            // Assert
            Assert.IsFalse(trained);
            Assert.AreEqual(20, party.stats[StatType.Exploration]);
            Assert.AreEqual(700, gameManager.PlayerGold); // No training cost deducted
        }

        [Test]
        public void EquipmentPurchase_AddsToParty()
        {
            // Arrange
            SetGold(1000);
            partyService.RecruitParty("Equipped Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialEquipmentCount = party.equipment.Count;

            var equipment = new Equipment("Magic Sword", 250);
            equipment.statBonuses[StatType.Combat] = 5;

            // Act
            bool purchased = partyService.PurchaseEquipment(party.id, equipment);

            // Assert
            Assert.IsTrue(purchased);
            Assert.AreEqual(initialEquipmentCount + 1, party.equipment.Count);
            Assert.AreEqual(450, gameManager.PlayerGold); // 1000 - 300 - 250
        }

        [Test]
        public void EquipmentPurchase_WithInsufficientFunds_Fails()
        {
            // Arrange
            SetGold(400);
            partyService.RecruitParty("Poor Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialEquipmentCount = party.equipment.Count;

            var equipment = new Equipment("Expensive Armor", 250);

            // Act
            bool purchased = partyService.PurchaseEquipment(party.id, equipment);

            // Assert
            Assert.IsFalse(purchased);
            Assert.AreEqual(initialEquipmentCount, party.equipment.Count);
            Assert.AreEqual(100, gameManager.PlayerGold); // 400 - 300 from recruitment
        }

        [Test]
        public void PartyLoyalty_IncreasesWithSuccessfulQuests()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Loyal Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialLoyalty = party.loyalty;

            // Act
            partyService.ModifyPartyLoyalty(party.id, 20); // Simulate quest success

            // Assert
            Assert.AreEqual(initialLoyalty + 20, party.loyalty);
        }

        [Test]
        public void PartyLoyalty_DecreasesWithFailedQuests()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Unlucky Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialLoyalty = party.loyalty;

            // Act
            partyService.ModifyPartyLoyalty(party.id, -15); // Simulate quest failure

            // Assert
            Assert.AreEqual(initialLoyalty - 15, party.loyalty);
        }

        [Test]
        public void PartyLoyalty_BelowThreshold_BecomesUnavailable()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Disloyal Party", 300);

            Party party = partyService.GetAllParties()[0];
            party.loyalty = 25;

            // Act
            partyService.ModifyPartyLoyalty(party.id, -10); // Should drop below 20

            // Assert
            Assert.IsFalse(party.isAvailable);
        }

        [Test]
        public void PartyGrowth_ThroughExperience_ImprovesStats()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Growing Party", 300);

            Party party = partyService.GetAllParties()[0];
            int initialTotalStats = party.stats[StatType.Exploration] +
                                  party.stats[StatType.Combat] +
                                  party.stats[StatType.Admin];

            // Act
            party.AddExperience(300); // Should grant 3 stat points

            // Assert
            int finalTotalStats = party.stats[StatType.Exploration] +
                                party.stats[StatType.Combat] +
                                party.stats[StatType.Admin];
            Assert.AreEqual(initialTotalStats + 3, finalTotalStats);
        }

        [UnityTest]
        public IEnumerator PartyRecruitment_WorkflowIntegration()
        {
            // Arrange
            SetGold(2000);

            // Act - Recruit party
            bool recruited = partyService.RecruitParty("Integration Party", 500);
            yield return null;

            Assert.IsTrue(recruited);
            Party party = partyService.GetPartyById(partyService.GetAllParties()[0].id);
            Assert.IsNotNull(party);

            // Act - Train party
            bool trained = partyService.TrainParty(party.id, StatType.Combat, 300);
            yield return null;

            Assert.IsTrue(trained);

            // Act - Purchase equipment
            var equipment = new Equipment("Battle Axe", 400);
            equipment.statBonuses[StatType.Combat] = 4;
            bool purchased = partyService.PurchaseEquipment(party.id, equipment);
            yield return null;

            Assert.IsTrue(purchased);

            // Assert - Check final state
            Assert.AreEqual(800, gameManager.PlayerGold); // 2000 - 500 - 300 - 400
            Assert.Greater(party.GetEffectiveStat(StatType.Combat), 1); // Base + training + equipment
            Assert.AreEqual(1, party.equipment.Count);
        }

        [Test]
        public void PartyService_GetPartyById_WithValidId_ReturnsParty()
        {
            // Arrange
            SetGold(500);
            partyService.RecruitParty("Find Me", 300);
            Party party = partyService.GetAllParties()[0];

            // Act
            Party found = partyService.GetPartyById(party.id);

            // Assert
            Assert.IsNotNull(found);
            Assert.AreEqual(party.id, found.id);
        }

        [Test]
        public void PartyService_GetPartyById_WithInvalidId_ReturnsNull()
        {
            // Act
            Party found = partyService.GetPartyById("invalid-id");

            // Assert
            Assert.IsNull(found);
        }

        [Test]
        public void PartyService_GetAvailableParties_FiltersCorrectly()
        {
            // Arrange
            SetGold(2000);
            partyService.RecruitParty("Available 1", 300);
            partyService.RecruitParty("Available 2", 300);
            partyService.RecruitParty("Unavailable", 300);

            // Make one party unavailable
            Party unavailableParty = partyService.GetAllParties()[2];
            unavailableParty.loyalty = 10;
            unavailableParty.UpdateAvailability();

            // Act
            var availableParties = partyService.GetAvailableParties();

            // Assert
            Assert.AreEqual(2, availableParties.Count);
            Assert.IsTrue(availableParties.TrueForAll(p => p.isAvailable));
        }
    }
}
