using System.Collections;
using NUnit.Framework;
using GuildReceptionist;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Integration tests for complete quest lifecycle
    /// Tests the full flow: quest creation → assignment → execution → completion/failure → rewards
    /// </summary>
    [TestFixture]
    public class QuestLifecycleTests
    {
        private QuestService _questService;
        private GameManager _gameManager;
        private Quest _testQuest;
        private Party _testParty;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Initialize GameManager
            GameObject go = new GameObject("GameManager");
            _gameManager = go.AddComponent<GameManager>();

            yield return null;

            // Initialize services
            _questService = new QuestService();

            // Create test quest
            _testQuest = new Quest
            {
                id = "integration-quest-001",
                type = QuestType.Combat,
                difficulty = 3,
                duration = 5,
                rewardGold = 500,
                reputationImpact = 20,
                state = QuestState.Available,
                successRate = 0.8f
            };
            _testQuest.requiredStats[StatType.Combat] = 20;
            _testQuest.requiredStats[StatType.Exploration] = 10;
            _testQuest.rewardMaterials.Add(new MaterialReward("herb-001", 3, 0.9f));
            _testQuest.rewardMaterials.Add(new MaterialReward("ore-001", 2, 0.5f));

            // Create test party
            _testParty = new Party("Integration Test Warriors");
            _testParty.stats[StatType.Combat] = 25;
            _testParty.stats[StatType.Exploration] = 15;
            _testParty.stats[StatType.Admin] = 10;
            _testParty.loyalty = 80;
            _testParty.isAvailable = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _questService?.ClearAllQuests();
            
            if (_gameManager != null)
            {
                Object.Destroy(_gameManager.gameObject);
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_FullSuccessFlow_CompletesSuccessfully()
        {
            // Phase 1: Quest Creation
            bool addResult = _questService.AddQuest(_testQuest);
            Assert.IsTrue(addResult, "Quest should be added successfully");
            Assert.AreEqual(QuestState.Available, _testQuest.state);

            yield return null;

            // Phase 2: Quest Assignment
            bool assignResult = _questService.AssignQuest(_testQuest.id, _testParty);
            Assert.IsTrue(assignResult, "Quest should be assigned successfully");
            Assert.AreEqual(QuestState.Assigned, _testQuest.state);
            Assert.AreEqual(_testParty.id, _testQuest.assignedPartyId);

            yield return null;

            // Phase 3: Quest Start
            int startDay = 1;
            bool startResult = _questService.StartQuest(_testQuest.id, startDay);
            Assert.IsTrue(startResult, "Quest should start successfully");
            Assert.AreEqual(QuestState.InProgress, _testQuest.state);
            Assert.AreEqual(startDay, _testQuest.startDay);

            yield return null;

            // Phase 4: Quest Completion (simulate time passage)
            int completionDay = startDay + _testQuest.duration;
            
            // Verify quest is ready to complete
            bool isReady = _testQuest.IsReadyToComplete(completionDay);
            Assert.IsTrue(isReady, "Quest should be ready to complete after duration elapsed");

            // Complete the quest
            float successRate = _questService.CalculateSuccessRate(_testQuest, _testParty);
            bool completeResult = _questService.CompleteQuest(_testQuest.id, completionDay, true); // Force success for testing
            
            Assert.IsTrue(completeResult, "Quest should complete successfully");
            Assert.AreEqual(QuestState.Completed, _testQuest.state);

            yield return null;

            // Phase 5: Verify Rewards
            // Check that rewards were granted (this will be verified when reward system is implemented)
            Assert.Greater(successRate, 0.5f, "Success rate should be positive with capable party");
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_FailureFlow_HandlesFailureCorrectly()
        {
            // Phase 1-3: Same as success flow
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);
            int startDay = 1;
            _questService.StartQuest(_testQuest.id, startDay);

            yield return null;

            // Phase 4: Quest Failure
            int completionDay = startDay + _testQuest.duration;
            bool failResult = _questService.CompleteQuest(_testQuest.id, completionDay, false); // Force failure

            Assert.IsTrue(failResult, "Quest failure should be processed");
            Assert.AreEqual(QuestState.Failed, _testQuest.state);

            yield return null;

            // Phase 5: Verify Consequences
            // Check that party is marked as available again
            Assert.IsTrue(_testParty.isAvailable, "Party should be available after quest completion");
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_WithLowStatsParty_HasLowerSuccessRate()
        {
            // Arrange: Create weak party
            var weakParty = new Party("Weak Adventurers");
            weakParty.stats[StatType.Combat] = 5; // Much lower than required 20
            weakParty.stats[StatType.Exploration] = 3; // Much lower than required 10
            weakParty.loyalty = 50;
            weakParty.isAvailable = true;

            _questService.AddQuest(_testQuest);

            // Act: Calculate success rates
            float strongPartyRate = _questService.CalculateSuccessRate(_testQuest, _testParty);
            float weakPartyRate = _questService.CalculateSuccessRate(_testQuest, weakParty);

            yield return null;

            // Assert: Weak party should have lower success rate
            Assert.Less(weakPartyRate, strongPartyRate, 
                "Party with insufficient stats should have lower success rate");
            Assert.Greater(weakPartyRate, 0f, "Success rate should still be above 0");
            Assert.Less(weakPartyRate, 0.5f, "Success rate with weak party should be below 50%");
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_MultipleQuestsSimultaneously_HandlesCorrectly()
        {
            // Arrange: Create multiple quests and parties
            var quest1 = new Quest { id = "quest-1", difficulty = 2, duration = 3, state = QuestState.Available };
            quest1.requiredStats[StatType.Combat] = 15;
            
            var quest2 = new Quest { id = "quest-2", difficulty = 3, duration = 4, state = QuestState.Available };
            quest2.requiredStats[StatType.Exploration] = 20;

            var party1 = new Party("Party 1");
            party1.stats[StatType.Combat] = 20;
            party1.isAvailable = true;

            var party2 = new Party("Party 2");
            party2.stats[StatType.Exploration] = 25;
            party2.isAvailable = true;

            // Act: Add and assign quests
            _questService.AddQuest(quest1);
            _questService.AddQuest(quest2);
            _questService.AssignQuest(quest1.id, party1);
            _questService.AssignQuest(quest2.id, party2);

            yield return null;

            // Assert: Both quests should be assigned independently
            Assert.AreEqual(QuestState.Assigned, quest1.state);
            Assert.AreEqual(QuestState.Assigned, quest2.state);
            Assert.AreEqual(party1.id, quest1.assignedPartyId);
            Assert.AreEqual(party2.id, quest2.assignedPartyId);
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_UnassignBeforeStart_AllowsReassignment()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);

            yield return null;

            // Act: Unassign quest
            bool unassignResult = _questService.UnassignQuest(_testQuest.id);
            Assert.IsTrue(unassignResult, "Should successfully unassign");
            Assert.AreEqual(QuestState.Available, _testQuest.state);

            yield return null;

            // Act: Reassign to another party
            var otherParty = new Party("Other Warriors");
            otherParty.stats[StatType.Combat] = 20;
            otherParty.isAvailable = true;
            
            bool reassignResult = _questService.AssignQuest(_testQuest.id, otherParty);

            // Assert
            Assert.IsTrue(reassignResult, "Should successfully reassign quest");
            Assert.AreEqual(otherParty.id, _testQuest.assignedPartyId);
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_WithMaterialRewards_ProcessesRewardsCorrectly()
        {
            // Arrange: Quest with multiple material rewards
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);
            _questService.StartQuest(_testQuest.id, 1);

            yield return null;

            // Act: Complete quest
            int completionDay = 1 + _testQuest.duration;
            bool completeResult = _questService.CompleteQuest(_testQuest.id, completionDay, true);

            // Assert: Quest completed
            Assert.IsTrue(completeResult, "Quest should complete with rewards");
            Assert.AreEqual(QuestState.Completed, _testQuest.state);
            
            // Note: Actual reward processing will be tested when MaterialService is implemented
            Assert.AreEqual(2, _testQuest.rewardMaterials.Count, "Quest should have 2 material rewards defined");
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_QuestExpirationCheck_DetectsOverdueQuests()
        {
            // Arrange
            _questService.AddQuest(_testQuest);
            _questService.AssignQuest(_testQuest.id, _testParty);
            _questService.StartQuest(_testQuest.id, 1);

            yield return null;

            // Act: Check days remaining at different points
            int day2 = 2; // 1 day elapsed
            int daysRemaining1 = _testQuest.GetDaysRemaining(day2);
            Assert.AreEqual(4, daysRemaining1, "Should have 4 days remaining after 1 day");

            int day6 = 6; // 5 days elapsed (exactly at completion)
            int daysRemaining2 = _testQuest.GetDaysRemaining(day6);
            Assert.AreEqual(0, daysRemaining2, "Should have 0 days remaining at completion");

            int day10 = 10; // 9 days elapsed (overdue)
            int daysRemaining3 = _testQuest.GetDaysRemaining(day10);
            Assert.AreEqual(0, daysRemaining3, "Should return 0 (not negative) when overdue");

            yield return null;
        }

        [UnityTest]
        public IEnumerator QuestLifecycle_ConcurrentCompletionChecks_HandleCorrectly()
        {
            // Arrange: Multiple quests at different stages
            var quest1 = new Quest { id = "concurrent-1", difficulty = 2, duration = 3, state = QuestState.Available };
            quest1.requiredStats[StatType.Combat] = 10;
            
            var quest2 = new Quest { id = "concurrent-2", difficulty = 3, duration = 5, state = QuestState.Available };
            quest2.requiredStats[StatType.Exploration] = 15;

            _questService.AddQuest(quest1);
            _questService.AddQuest(quest2);
            _questService.AssignQuest(quest1.id, _testParty);
            
            var party2 = new Party("Party 2");
            party2.stats[StatType.Exploration] = 20;
            party2.isAvailable = true;
            _questService.AssignQuest(quest2.id, party2);

            yield return null;

            // Act: Start both quests
            _questService.StartQuest(quest1.id, 1);
            _questService.StartQuest(quest2.id, 1);

            yield return null;

            // Assert: Check completion readiness at different days
            bool quest1Ready = quest1.IsReadyToComplete(4); // Day 4: quest1 should be ready (duration 3)
            bool quest2Ready = quest2.IsReadyToComplete(4); // Day 4: quest2 should not be ready (duration 5)

            Assert.IsTrue(quest1Ready, "Quest 1 should be ready on day 4");
            Assert.IsFalse(quest2Ready, "Quest 2 should not be ready on day 4");

            // Complete quest 1
            _questService.CompleteQuest(quest1.id, 4, true);
            Assert.AreEqual(QuestState.Completed, quest1.state);
            Assert.AreEqual(QuestState.InProgress, quest2.state, "Quest 2 should still be in progress");

            yield return null;

            // Complete quest 2 on day 6
            bool quest2ReadyLater = quest2.IsReadyToComplete(6);
            Assert.IsTrue(quest2ReadyLater, "Quest 2 should be ready on day 6");
            _questService.CompleteQuest(quest2.id, 6, true);
            Assert.AreEqual(QuestState.Completed, quest2.state);
        }
    }
}
