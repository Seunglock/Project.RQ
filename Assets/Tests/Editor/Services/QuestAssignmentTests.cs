using NUnit.Framework;
using GuildReceptionist;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Unit tests for quest assignment logic in QuestService
    /// Tests contract: QuestService must handle quest creation, assignment, and success rate calculations
    /// </summary>
    [TestFixture]
    public class QuestAssignmentTests
    {
        private QuestService _questService;
        private Quest _testQuest;
        private Party _testParty;

        [SetUp]
        public void Setup()
        {
            _questService = new QuestService();
            
            // Create a test quest
            _testQuest = new Quest
            {
                id = "quest-001",
                type = QuestType.Combat,
                difficulty = 3,
                duration = 5,
                rewardGold = 100,
                reputationImpact = 10,
                state = QuestState.Available
            };
            _testQuest.requiredStats[StatType.Combat] = 20;
            _testQuest.requiredStats[StatType.Exploration] = 10;

            // Create a test party
            _testParty = new Party("Test Warriors");
            _testParty.stats[StatType.Combat] = 15;
            _testParty.stats[StatType.Exploration] = 10;
            _testParty.stats[StatType.Admin] = 5;
            _testParty.loyalty = 75;
            _testParty.isAvailable = true;
        }

        [TearDown]
        public void TearDown()
        {
            _questService?.ClearAllQuests();
        }

        #region Quest Creation Tests

        [Test]
        public void QuestService_AddQuest_AddsQuestSuccessfully()
        {
            // Act
            bool result = _questService.AddQuest(_testQuest);
            var quests = _questService.GetAvailableQuests();

            // Assert
            Assert.IsTrue(result, "Should successfully add quest");
            Assert.Contains(_testQuest, quests, "Quest should be in available quests list");
        }

        [Test]
        public void QuestService_AddInvalidQuest_Fails()
        {
            // Arrange
            var invalidQuest = new Quest
            {
                difficulty = 10, // Invalid (> 5)
                duration = 0 // Invalid (<= 0)
            };

            // Expect error logs (both from Quest.IsValid and QuestService.AddQuest)
            LogAssert.Expect(LogType.Error, $"Quest {invalidQuest.id}: Invalid difficulty 10");
            LogAssert.Expect(LogType.Error, $"Quest {invalidQuest.id} failed validation");

            // Act
            bool result = _questService.AddQuest(invalidQuest);

            // Assert
            Assert.IsFalse(result, "Should not add invalid quest");
        }

        [Test]
        public void QuestService_AddDuplicateQuest_Fails()
        {
            // Arrange
            _questService.AddQuest(_testQuest);

            // Act
            bool result = _questService.AddQuest(_testQuest);

            // Assert
            Assert.IsFalse(result, "Should not add duplicate quest");
        }

        [Test]
        public void QuestService_GetQuestById_ReturnsCorrectQuest()
        {
            // Arrange
            _questService.AddQuest(_testQuest);

            // Act
            var quest = _questService.GetQuestById("quest-001");

            // Assert
            Assert.IsNotNull(quest, "Should find quest by ID");
            Assert.AreEqual(_testQuest.id, quest.id);
        }

        [Test]
        public void QuestService_GetQuestById_WithInvalidId_ReturnsNull()
        {
            // Act
            var quest = _questService.GetQuestById("invalid-id");

            // Assert
            Assert.IsNull(quest, "Should return null for invalid ID");
        }

        #endregion

        #region Quest Assignment Tests

        [Test]
        public void QuestService_AssignQuest_WithValidParty_Succeeds()
        {
            // Arrange
            _questService.AddQuest(_testQuest);

            // Act
            bool result = _questService.AssignQuest(_testQuest.id, _testParty);

            // Assert
            Assert.IsTrue(result, "Should successfully assign quest to party");
            Assert.AreEqual(_testParty.id, _testQuest.assignedPartyId, "Quest should store party ID");
            Assert.AreEqual(QuestState.Assigned, _testQuest.state, "Quest state should be Assigned");
        }

        [Test]
        public void QuestService_AssignQuest_WithUnavailableParty_Fails()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _testParty.isAvailable = false;

            // Act
            bool result = _questService.AssignQuest(_testQuest.id, _testParty);

            // Assert
            Assert.IsFalse(result, "Should not assign quest to unavailable party");
            Assert.IsNull(_testQuest.assignedPartyId, "Quest should not store party ID");
        }

        [Test]
        public void QuestService_AssignQuest_WhenAlreadyAssigned_Fails()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);

            var anotherParty = new Party("Other Warriors");

            // Act
            bool result = _questService.AssignQuest(_testQuest.id, anotherParty);

            // Assert
            Assert.IsFalse(result, "Should not reassign already assigned quest");
            Assert.AreEqual(_testParty.id, _testQuest.assignedPartyId, "Original party should remain assigned");
        }

        [Test]
        public void QuestService_AssignQuest_WithInvalidQuestId_Fails()
        {
            // Expect error log
            LogAssert.Expect(LogType.Error, "Quest invalid-id not found");

            // Act
            bool result = _questService.AssignQuest("invalid-id", _testParty);

            // Assert
            Assert.IsFalse(result, "Should fail when quest ID is invalid");
        }

        [Test]
        public void QuestService_AssignQuest_WithNullParty_Fails()
        {
            // Arrange
            _questService.AddQuest(_testQuest);

            // Expect error log
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "Party cannot be null");

            // Act
            bool result = _questService.AssignQuest(_testQuest.id, null);

            // Assert
            Assert.IsFalse(result, "Should fail when party is null");
        }

        #endregion

        #region Success Rate Calculation Tests

        [Test]
        public void QuestService_CalculateSuccessRate_WithMatchingStats_ReturnsHighRate()
        {
            // Arrange
            _testParty.stats[StatType.Combat] = 25; // Exceeds requirement of 20
            _testParty.stats[StatType.Exploration] = 15; // Exceeds requirement of 10

            // Act
            float successRate = _questService.CalculateSuccessRate(_testQuest, _testParty);

            // Assert
            Assert.Greater(successRate, 0.7f, "Success rate should be high when stats exceed requirements");
            Assert.LessOrEqual(successRate, 1.0f, "Success rate should not exceed 1.0");
        }

        [Test]
        public void QuestService_CalculateSuccessRate_WithInsufficientStats_ReturnsLowRate()
        {
            // Arrange
            _testParty.stats[StatType.Combat] = 5; // Below requirement of 20
            _testParty.stats[StatType.Exploration] = 3; // Below requirement of 10

            // Act
            float successRate = _questService.CalculateSuccessRate(_testQuest, _testParty);

            // Assert
            Assert.Less(successRate, 0.5f, "Success rate should be low when stats are insufficient");
            Assert.GreaterOrEqual(successRate, 0.0f, "Success rate should not be negative");
        }

        [Test]
        public void QuestService_CalculateSuccessRate_WithLowLoyalty_ReducesRate()
        {
            // Arrange
            _testParty.stats[StatType.Combat] = 20;
            _testParty.stats[StatType.Exploration] = 10;
            _testParty.loyalty = 20; // Low loyalty

            // Act
            float successRate = _questService.CalculateSuccessRate(_testQuest, _testParty);

            // Arrange party with high loyalty for comparison
            var highLoyaltyParty = new Party("Loyal Warriors");
            highLoyaltyParty.stats[StatType.Combat] = 20;
            highLoyaltyParty.stats[StatType.Exploration] = 10;
            highLoyaltyParty.loyalty = 100;
            float highLoyaltyRate = _questService.CalculateSuccessRate(_testQuest, highLoyaltyParty);

            // Assert
            Assert.Less(successRate, highLoyaltyRate, "Low loyalty should reduce success rate");
        }

        [Test]
        public void QuestService_CalculateSuccessRate_WithEquipmentBonuses_IncorporatesBonus()
        {
            // Arrange
            var equipment = new Equipment("Magic Sword", 100);
            equipment.statBonuses[StatType.Combat] = 10;
            _testParty.AddEquipment(equipment);

            // Act
            float successRateWithEquipment = _questService.CalculateSuccessRate(_testQuest, _testParty);

            // Arrange party without equipment for comparison
            var partyWithoutEquipment = new Party("Basic Warriors");
            partyWithoutEquipment.stats[StatType.Combat] = _testParty.stats[StatType.Combat];
            partyWithoutEquipment.stats[StatType.Exploration] = _testParty.stats[StatType.Exploration];
            partyWithoutEquipment.loyalty = _testParty.loyalty;
            float successRateWithoutEquipment = _questService.CalculateSuccessRate(_testQuest, partyWithoutEquipment);

            // Assert
            Assert.Greater(successRateWithEquipment, successRateWithoutEquipment, 
                "Equipment bonuses should increase success rate");
        }

        [Test]
        public void QuestService_CalculateSuccessRate_WithNullQuest_ReturnsZero()
        {
            // Act
            float successRate = _questService.CalculateSuccessRate(null, _testParty);

            // Assert
            Assert.AreEqual(0f, successRate, "Success rate should be 0 for null quest");
        }

        [Test]
        public void QuestService_CalculateSuccessRate_WithNullParty_ReturnsZero()
        {
            // Act
            float successRate = _questService.CalculateSuccessRate(_testQuest, null);

            // Assert
            Assert.AreEqual(0f, successRate, "Success rate should be 0 for null party");
        }

        #endregion

        #region Quest Filtering Tests

        [Test]
        public void QuestService_GetAvailableQuests_ReturnsOnlyAvailableQuests()
        {
            // Arrange
            var quest1 = new Quest { id = "q1", difficulty = 1, duration = 3, state = QuestState.Available };
            var quest2 = new Quest { id = "q2", difficulty = 2, duration = 4, state = QuestState.Assigned };
            var quest3 = new Quest { id = "q3", difficulty = 3, duration = 5, state = QuestState.Available };

            _questService.AddQuest(quest1);
            _questService.AddQuest(quest2);
            _questService.AddQuest(quest3);

            // Act
            var availableQuests = _questService.GetAvailableQuests();

            // Assert
            Assert.AreEqual(2, availableQuests.Count, "Should return only available quests");
            Assert.Contains(quest1, availableQuests);
            Assert.Contains(quest3, availableQuests);
        }

        [Test]
        public void QuestService_GetAssignedQuests_ReturnsOnlyAssignedQuests()
        {
            // Arrange
            var quest1 = new Quest { id = "q1", difficulty = 1, duration = 3, state = QuestState.Available };
            var quest2 = new Quest { id = "q2", difficulty = 2, duration = 4, state = QuestState.Assigned };
            var quest3 = new Quest { id = "q3", difficulty = 3, duration = 5, state = QuestState.InProgress };

            _questService.AddQuest(quest1);
            _questService.AddQuest(quest2);
            _questService.AddQuest(quest3);

            // Act
            var assignedQuests = _questService.GetAssignedQuests();

            // Assert
            Assert.AreEqual(1, assignedQuests.Count, "Should return only assigned quests");
            Assert.Contains(quest2, assignedQuests);
        }

        [Test]
        public void QuestService_GetQuestsByType_ReturnsCorrectQuests()
        {
            // Arrange
            var combatQuest = new Quest { id = "q1", type = QuestType.Combat, difficulty = 1, duration = 3 };
            var explorationQuest = new Quest { id = "q2", type = QuestType.Exploration, difficulty = 2, duration = 4 };
            var adminQuest = new Quest { id = "q3", type = QuestType.Admin, difficulty = 3, duration = 5 };

            _questService.AddQuest(combatQuest);
            _questService.AddQuest(explorationQuest);
            _questService.AddQuest(adminQuest);

            // Act
            var combatQuests = _questService.GetQuestsByType(QuestType.Combat);

            // Assert
            Assert.AreEqual(1, combatQuests.Count, "Should return only combat quests");
            Assert.Contains(combatQuest, combatQuests);
        }

        #endregion

        #region Quest Unassignment Tests

        [Test]
        public void QuestService_UnassignQuest_ResetsQuestState()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);

            // Act
            bool result = _questService.UnassignQuest(_testQuest.id);

            // Assert
            Assert.IsTrue(result, "Should successfully unassign quest");
            Assert.IsNull(_testQuest.assignedPartyId, "Party ID should be cleared");
            Assert.AreEqual(QuestState.Available, _testQuest.state, "State should return to Available");
        }

        [Test]
        public void QuestService_UnassignQuest_WithInvalidId_Fails()
        {
            // Act
            bool result = _questService.UnassignQuest("invalid-id");

            // Assert
            Assert.IsFalse(result, "Should fail for invalid quest ID");
        }

        [Test]
        public void QuestService_UnassignQuest_WhenInProgress_Fails()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _testQuest.state = QuestState.InProgress;

            // Act
            bool result = _questService.UnassignQuest(_testQuest.id);

            // Assert
            Assert.IsFalse(result, "Should not unassign quest that is in progress");
            Assert.AreEqual(QuestState.InProgress, _testQuest.state, "State should remain InProgress");
        }

        #endregion
    }
}
