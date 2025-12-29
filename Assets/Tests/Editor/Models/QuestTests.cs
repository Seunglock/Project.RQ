using NUnit.Framework;
using GuildReceptionist;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Unit tests for Quest model validation and state transitions
    /// Tests contract: Quest entity must enforce validation rules and state machine logic
    /// </summary>
    [TestFixture]
    public class QuestTests
    {
        private Quest _quest;

        [SetUp]
        public void Setup()
        {
            _quest = new Quest();
        }

        #region Validation Tests

        [Test]
        public void Quest_WithValidDifficulty_PassesValidation()
        {
            // Arrange
            _quest.difficulty = 3;
            _quest.duration = 5;
            _quest.requiredStats[StatType.Combat] = 30;

            // Act
            bool isValid = _quest.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Quest with difficulty 3 should be valid");
        }

        [Test]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(-1)]
        public void Quest_WithInvalidDifficulty_FailsValidation(int invalidDifficulty)
        {
            // Arrange
            _quest.difficulty = invalidDifficulty;
            _quest.duration = 5;

            // Expect error log
            LogAssert.Expect(LogType.Error, $"Quest {_quest.id}: Invalid difficulty {invalidDifficulty}");

            // Act
            bool isValid = _quest.IsValid();

            // Assert
            Assert.IsFalse(isValid, $"Quest with difficulty {invalidDifficulty} should be invalid");
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public void Quest_WithInvalidDuration_FailsValidation(int invalidDuration)
        {
            // Arrange
            _quest.difficulty = 3;
            _quest.duration = invalidDuration;
            _quest.requiredStats[StatType.Combat] = 30;

            // Expect error log
            LogAssert.Expect(LogType.Error, $"Quest {_quest.id}: Invalid duration {invalidDuration}");

            // Act
            bool isValid = _quest.IsValid();

            // Assert
            Assert.IsFalse(isValid, $"Quest with duration {invalidDuration} should be invalid");
        }

        [Test]
        public void Quest_WithCorrectStatRequirements_PassesValidation()
        {
            // Arrange
            _quest.difficulty = 3;
            _quest.duration = 5;
            _quest.requiredStats[StatType.Combat] = 20;
            _quest.requiredStats[StatType.Exploration] = 10;
            // Total = 30 = 3 * 10 (correct)

            // Act
            bool isValid = _quest.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Quest with correct stat requirements should be valid");
        }

        [Test]
        public void Quest_WithIncorrectStatRequirements_LogsWarningButPasses()
        {
            // Arrange
            _quest.difficulty = 3;
            _quest.duration = 5;
            _quest.requiredStats[StatType.Combat] = 15; // Total = 15, but expected 30
            
            // Act
            bool isValid = _quest.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Quest with mismatched stats should still pass (warning only)");
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void Quest_InitialState_IsAvailable()
        {
            // Assert
            Assert.AreEqual(QuestState.Available, _quest.state, "New quest should start in Available state");
        }

        [Test]
        public void Quest_AvailableToAssigned_IsValidTransition()
        {
            // Arrange
            _quest.state = QuestState.Available;

            // Act
            bool success = _quest.TransitionTo(QuestState.Assigned);

            // Assert
            Assert.IsTrue(success, "Transition from Available to Assigned should be valid");
            Assert.AreEqual(QuestState.Assigned, _quest.state);
        }

        [Test]
        public void Quest_AssignedToInProgress_IsValidTransition()
        {
            // Arrange
            _quest.state = QuestState.Assigned;

            // Act
            bool success = _quest.TransitionTo(QuestState.InProgress);

            // Assert
            Assert.IsTrue(success, "Transition from Assigned to InProgress should be valid");
            Assert.AreEqual(QuestState.InProgress, _quest.state);
        }

        [Test]
        public void Quest_InProgressToCompleted_IsValidTransition()
        {
            // Arrange
            _quest.state = QuestState.InProgress;

            // Act
            bool success = _quest.TransitionTo(QuestState.Completed);

            // Assert
            Assert.IsTrue(success, "Transition from InProgress to Completed should be valid");
            Assert.AreEqual(QuestState.Completed, _quest.state);
        }

        [Test]
        public void Quest_InProgressToFailed_IsValidTransition()
        {
            // Arrange
            _quest.state = QuestState.InProgress;

            // Act
            bool success = _quest.TransitionTo(QuestState.Failed);

            // Assert
            Assert.IsTrue(success, "Transition from InProgress to Failed should be valid");
            Assert.AreEqual(QuestState.Failed, _quest.state);
        }

        [Test]
        public void Quest_AvailableToCompleted_IsInvalidTransition()
        {
            // Arrange
            _quest.state = QuestState.Available;
            QuestState originalState = _quest.state;

            // Act
            bool success = _quest.TransitionTo(QuestState.Completed);

            // Assert
            Assert.IsFalse(success, "Transition from Available to Completed should be invalid");
            Assert.AreEqual(originalState, _quest.state, "State should not change on invalid transition");
        }

        [Test]
        public void Quest_CompletedToInProgress_IsInvalidTransition()
        {
            // Arrange
            _quest.state = QuestState.Completed;
            QuestState originalState = _quest.state;

            // Act
            bool success = _quest.TransitionTo(QuestState.InProgress);

            // Assert
            Assert.IsFalse(success, "Transition from Completed to InProgress should be invalid");
            Assert.AreEqual(originalState, _quest.state, "State should not change on invalid transition");
        }

        #endregion

        #region Assignment Tests

        [Test]
        public void Quest_AssignToParty_WhenAvailable_Succeeds()
        {
            // Arrange
            _quest.state = QuestState.Available;
            string partyId = "party-123";

            // Act
            bool success = _quest.AssignToParty(partyId);

            // Assert
            Assert.IsTrue(success, "Should successfully assign quest to party");
            Assert.AreEqual(partyId, _quest.assignedPartyId, "Party ID should be set");
            Assert.AreEqual(QuestState.Assigned, _quest.state, "State should transition to Assigned");
        }

        [Test]
        public void Quest_AssignToParty_WhenNotAvailable_Fails()
        {
            // Arrange
            _quest.state = QuestState.InProgress;
            string partyId = "party-123";

            // Act
            bool success = _quest.AssignToParty(partyId);

            // Assert
            Assert.IsFalse(success, "Should not assign quest when not in Available state");
            Assert.IsNull(_quest.assignedPartyId, "Party ID should not be set");
        }

        #endregion

        #region Quest Execution Tests

        [Test]
        public void Quest_StartQuest_WithAssignedParty_Succeeds()
        {
            // Arrange
            _quest.state = QuestState.Assigned;
            _quest.assignedPartyId = "party-123";
            int currentDay = 10;

            // Act
            bool success = _quest.StartQuest(currentDay);

            // Assert
            Assert.IsTrue(success, "Should successfully start quest");
            Assert.AreEqual(currentDay, _quest.startDay, "Start day should be set");
            Assert.AreEqual(QuestState.InProgress, _quest.state, "State should transition to InProgress");
        }

        [Test]
        public void Quest_StartQuest_WithoutAssignedParty_Fails()
        {
            // Arrange
            _quest.state = QuestState.Assigned;
            _quest.assignedPartyId = null;
            int currentDay = 10;

            // Act
            bool success = _quest.StartQuest(currentDay);

            // Assert
            Assert.IsFalse(success, "Should not start quest without assigned party");
        }

        [Test]
        public void Quest_GetDaysRemaining_WhenInProgress_ReturnsCorrectValue()
        {
            // Arrange
            _quest.state = QuestState.InProgress;
            _quest.startDay = 10;
            _quest.duration = 5;
            int currentDay = 12; // 2 days elapsed

            // Act
            int daysRemaining = _quest.GetDaysRemaining(currentDay);

            // Assert
            Assert.AreEqual(3, daysRemaining, "Should have 3 days remaining (5 - 2)");
        }

        [Test]
        public void Quest_GetDaysRemaining_WhenNotInProgress_ReturnsNegativeOne()
        {
            // Arrange
            _quest.state = QuestState.Available;
            int currentDay = 12;

            // Act
            int daysRemaining = _quest.GetDaysRemaining(currentDay);

            // Assert
            Assert.AreEqual(-1, daysRemaining, "Should return -1 when not in progress");
        }

        [Test]
        public void Quest_GetDaysRemaining_WhenOverdue_ReturnsZero()
        {
            // Arrange
            _quest.state = QuestState.InProgress;
            _quest.startDay = 10;
            _quest.duration = 5;
            int currentDay = 20; // Far past due date

            // Act
            int daysRemaining = _quest.GetDaysRemaining(currentDay);

            // Assert
            Assert.AreEqual(0, daysRemaining, "Should return 0 when overdue (not negative)");
        }

        [Test]
        public void Quest_IsReadyToComplete_WhenDurationElapsed_ReturnsTrue()
        {
            // Arrange
            _quest.state = QuestState.InProgress;
            _quest.startDay = 10;
            _quest.duration = 5;
            int currentDay = 15; // Exactly at completion

            // Act
            bool isReady = _quest.IsReadyToComplete(currentDay);

            // Assert
            Assert.IsTrue(isReady, "Quest should be ready to complete when duration elapsed");
        }

        [Test]
        public void Quest_IsReadyToComplete_WhenNotInProgress_ReturnsFalse()
        {
            // Arrange
            _quest.state = QuestState.Assigned;
            _quest.startDay = 10;
            _quest.duration = 5;
            int currentDay = 15;

            // Act
            bool isReady = _quest.IsReadyToComplete(currentDay);

            // Assert
            Assert.IsFalse(isReady, "Quest should not be ready when not in progress");
        }

        [Test]
        public void Quest_IsReadyToComplete_WhenDurationNotElapsed_ReturnsFalse()
        {
            // Arrange
            _quest.state = QuestState.InProgress;
            _quest.startDay = 10;
            _quest.duration = 5;
            int currentDay = 12; // Only 2 days elapsed

            // Act
            bool isReady = _quest.IsReadyToComplete(currentDay);

            // Assert
            Assert.IsFalse(isReady, "Quest should not be ready when duration not elapsed");
        }

        #endregion

        #region MaterialReward Tests

        [Test]
        public void MaterialReward_CreationWithValidValues_Succeeds()
        {
            // Act
            var reward = new MaterialReward("mat-001", 5, 0.75f);

            // Assert
            Assert.AreEqual("mat-001", reward.materialId);
            Assert.AreEqual(5, reward.quantity);
            Assert.AreEqual(0.75f, reward.dropChance, 0.001f);
        }

        [Test]
        public void MaterialReward_DropChanceAboveOne_IsClamped()
        {
            // Act
            var reward = new MaterialReward("mat-001", 5, 1.5f);

            // Assert
            Assert.AreEqual(1.0f, reward.dropChance, 0.001f, "Drop chance should be clamped to 1.0");
        }

        [Test]
        public void MaterialReward_DropChanceBelowZero_IsClamped()
        {
            // Act
            var reward = new MaterialReward("mat-001", 5, -0.5f);

            // Assert
            Assert.AreEqual(0.0f, reward.dropChance, 0.001f, "Drop chance should be clamped to 0.0");
        }

        #endregion
    }
}
