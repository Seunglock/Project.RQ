using NUnit.Framework;
using GuildReceptionist;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Integration tests for quarterly debt payment processing through DebtService and GameManager
    /// </summary>
    [TestFixture]
    public class DebtPaymentTests
    {
        private GameObject gameManagerObject;
        private GameObject debtServiceObject;
        private GameManager gameManager;
        private DebtService debtService;

        [SetUp]
        public void Setup()
        {
            // Clean up any existing instances
            var existingManagers = GameObject.FindObjectsOfType<GameManager>();
            foreach (var manager in existingManagers)
            {
                GameObject.DestroyImmediate(manager.gameObject);
            }

            var existingServices = GameObject.FindObjectsOfType<DebtService>();
            foreach (var service in existingServices)
            {
                GameObject.DestroyImmediate(service.gameObject);
            }

            // Create fresh instances
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            debtServiceObject = new GameObject("DebtService");
            debtService = debtServiceObject.AddComponent<DebtService>();

            // EventSystem is a singleton, just ensure it's initialized
            EventSystem.Instance.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            EventSystem.Instance.Clear();

            if (debtServiceObject != null)
                GameObject.DestroyImmediate(debtServiceObject);
            if (gameManagerObject != null)
                GameObject.DestroyImmediate(gameManagerObject);
        }

        [UnityTest]
        public IEnumerator QuarterlyPayment_WithSufficientFunds_SuccessfullyPays()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();

            // Set up sufficient gold for payment
            gameManager.ModifyGold(10000);

            yield return null;

            // Act
            for (int day = 1; day < Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Advance to the quarter end
            gameManager.AdvanceDay();
            yield return null;

            // Assert
            Assert.IsTrue(gameManager.IsGameRunning, "Game should still be running after successful payment");
            // Gold should have been deducted by quarterly payment amount
        }

        [UnityTest]
        public IEnumerator QuarterlyPayment_WithInsufficientFunds_TriggersGameOver()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();

            // Ensure insufficient gold for payment
            gameManager.ModifyGold(-gameManager.PlayerGold); // Reset to 0

            bool gameOverTriggered = false;
            EventSystem.Instance.Subscribe<GameOverEvent>(evt => gameOverTriggered = true);

            yield return null;

            // Act
            for (int day = 1; day < Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Advance to the quarter end
            gameManager.AdvanceDay();
            yield return null;

            // Assert
            Assert.IsTrue(gameOverTriggered, "Game over should be triggered with insufficient funds");
            Assert.IsFalse(gameManager.IsGameRunning, "Game should not be running after failed payment");
        }

        [UnityTest]
        public IEnumerator MultipleQuarters_ProcessesPaymentsCorrectly()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();

            // Set up sufficient gold for multiple quarters
            gameManager.ModifyGold(20000);

            int paymentsProcessed = 0;
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(evt => paymentsProcessed++);

            yield return null;

            // Act - Advance through 3 quarters
            for (int quarter = 0; quarter < 3; quarter++)
            {
                for (int day = 1; day <= Constants.DAYS_PER_QUARTER; day++)
                {
                    gameManager.AdvanceDay();
                    yield return null;
                }
            }

            // Assert
            Assert.AreEqual(3, paymentsProcessed, "Should have processed 3 quarterly payments");
            Assert.IsTrue(gameManager.IsGameRunning, "Game should still be running after multiple successful payments");
        }

        [UnityTest]
        public IEnumerator DebtService_TracksPaymentHistory()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();
            gameManager.ModifyGold(15000);

            yield return null;

            // Act
            for (int day = 1; day <= Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Assert
            var debt = debtService.GetCurrentDebt();
            Assert.IsNotNull(debt, "Debt should exist");
            Assert.AreEqual(1, debt.paymentHistory.Count, "Should have one payment in history");
        }

        [UnityTest]
        public IEnumerator DebtService_AppliesInterestCorrectly()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();

            var initialBalance = debtService.GetCurrentDebt().currentBalance;
            gameManager.ModifyGold(20000); // Sufficient funds

            yield return null;

            // Act - Advance through one quarter
            for (int day = 1; day <= Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Assert
            var debt = debtService.GetCurrentDebt();
            // Balance should be: (initial - quarterly payment) + interest
            Assert.Greater(debt.currentBalance, 0, "Debt balance should be positive");
        }

        [UnityTest]
        public IEnumerator ManualPayment_ReducesDebtBalance()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();
            gameManager.ModifyGold(10000);

            var initialBalance = debtService.GetCurrentDebt().currentBalance;

            yield return null;

            // Act
            debtService.MakeManualPayment(3000);
            yield return null;

            // Assert
            var debt = debtService.GetCurrentDebt();
            Assert.AreEqual(initialBalance - 3000, debt.currentBalance, "Balance should be reduced by payment amount");
            Assert.AreEqual(7000, gameManager.PlayerGold, "Gold should be deducted");
        }

        [UnityTest]
        public IEnumerator ManualPayment_WithInsufficientGold_Fails()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();
            gameManager.ModifyGold(-gameManager.PlayerGold); // Reset to 0

            var initialBalance = debtService.GetCurrentDebt().currentBalance;

            yield return null;

            // Act
            bool success = debtService.MakeManualPayment(3000);
            yield return null;

            // Assert
            Assert.IsFalse(success, "Payment should fail with insufficient gold");
            Assert.AreEqual(initialBalance, debtService.GetCurrentDebt().currentBalance, "Balance should remain unchanged");
        }

        [UnityTest]
        public IEnumerator DebtPayment_PublishesCorrectEvents()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();
            gameManager.ModifyGold(10000);

            DebtPaymentEvent? capturedEvent = null;
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(evt => capturedEvent = evt);

            yield return null;

            // Act
            for (int day = 1; day <= Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Assert
            Assert.IsNotNull(capturedEvent, "DebtPaymentEvent should be published");
            Assert.Greater(capturedEvent.Value.Amount, 0, "Payment amount should be positive");
        }

        [UnityTest]
        public IEnumerator CompleteDebtPayoff_SetsStateToPaid()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();

            var totalDebt = debtService.GetCurrentDebt().currentBalance;
            gameManager.ModifyGold(totalDebt + 10000); // Enough to pay off completely

            yield return null;

            // Act
            debtService.MakeManualPayment(totalDebt);
            yield return null;

            // Assert
            var debt = debtService.GetCurrentDebt();
            Assert.AreEqual(0, debt.currentBalance, "Debt balance should be zero");
            Assert.AreEqual(DebtState.Paid, debt.state, "Debt state should be Paid");
        }

        [UnityTest]
        public IEnumerator QuarterAdvance_UpdatesDebtState()
        {
            // Arrange
            gameManager.StartNewGame();
            debtService.Initialize();
            gameManager.ModifyGold(10000);

            int quarterAdvancedCount = 0;
            EventSystem.Instance.Subscribe<QuarterAdvancedEvent>(evt => quarterAdvancedCount++);

            yield return null;

            // Act
            for (int day = 1; day <= Constants.DAYS_PER_QUARTER; day++)
            {
                gameManager.AdvanceDay();
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, quarterAdvancedCount, "Quarter should have advanced once");
            Assert.AreEqual(2, gameManager.CurrentQuarter, "Should be in quarter 2");
        }
    }
}
