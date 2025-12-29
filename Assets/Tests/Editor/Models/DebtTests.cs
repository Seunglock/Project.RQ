using NUnit.Framework;
using GuildReceptionist;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    /// <summary>
    /// Unit tests for Debt model validation, payment tracking, and interest calculations
    /// </summary>
    [TestFixture]
    public class DebtTests
    {
        [SetUp]
        public void Setup()
        {
            // EventSystem is a singleton, just ensure it's initialized
            EventSystem.Instance.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            EventSystem.Instance.Clear();
        }

        [Test]
        public void Debt_Constructor_SetsInitialValues()
        {
            // Arrange & Act
            int initialBalance = 10000;
            int quarterlyPayment = 1000;
            float interestRate = 0.05f;
            var debt = new Debt(initialBalance, quarterlyPayment, interestRate);

            // Assert
            Assert.AreEqual(initialBalance, debt.currentBalance);
            Assert.AreEqual(quarterlyPayment, debt.quarterlyPayment);
            Assert.AreEqual(interestRate, debt.interestRate);
            Assert.AreEqual(DebtState.Active, debt.state);
            Assert.IsNotNull(debt.paymentHistory);
            Assert.AreEqual(0, debt.paymentHistory.Count);
        }

        [Test]
        public void IsValid_WithValidData_ReturnsTrue()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool isValid = debt.IsValid();

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValid_WithNegativeBalance_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);
            debt.currentBalance = -100;

            // Expect error log
            LogAssert.Expect(LogType.Error, "Debt: Invalid balance -100");

            // Act
            bool isValid = debt.IsValid();

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void IsValid_WithZeroInterestRate_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0f);

            // Expect error log
            LogAssert.Expect(LogType.Error, "Debt: Invalid interest rate 0");

            // Act
            bool isValid = debt.IsValid();

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void IsValid_WithNegativeInterestRate_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, -0.05f);

            // Expect error log
            LogAssert.Expect(LogType.Error, "Debt: Invalid interest rate -0.05");

            // Act
            bool isValid = debt.IsValid();

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void MakePayment_WithValidAmount_ReducesBalance()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool success = debt.MakePayment(2000);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(8000, debt.currentBalance);
            Assert.AreEqual(1, debt.paymentHistory.Count);
            Assert.AreEqual(2000, debt.paymentHistory[0].amount);
            Assert.AreEqual(8000, debt.paymentHistory[0].remainingBalance);
        }

        [Test]
        public void MakePayment_WithAmountExceedingBalance_PaysFullBalance()
        {
            // Arrange
            var debt = new Debt(5000, 1000, 0.05f);

            // Act
            bool success = debt.MakePayment(10000);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(0, debt.currentBalance);
            Assert.AreEqual(DebtState.Paid, debt.state);
            Assert.AreEqual(1, debt.paymentHistory.Count);
            Assert.AreEqual(5000, debt.paymentHistory[0].amount);
        }

        [Test]
        public void MakePayment_WithZeroAmount_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool success = debt.MakePayment(0);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(10000, debt.currentBalance);
            Assert.AreEqual(0, debt.paymentHistory.Count);
        }

        [Test]
        public void MakePayment_WithNegativeAmount_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool success = debt.MakePayment(-100);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(10000, debt.currentBalance);
        }

        [Test]
        public void MakePayment_PublishesDebtPaymentEvent()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);
            DebtPaymentEvent? capturedEvent = null;
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(evt => capturedEvent = evt);

            // Act
            debt.MakePayment(2000);

            // Assert
            Assert.IsNotNull(capturedEvent);
            Assert.AreEqual(2000, capturedEvent.Value.Amount);
            Assert.AreEqual(8000, capturedEvent.Value.RemainingBalance);
        }

        [Test]
        public void IsPaymentDue_OnQuarterDay_ReturnsTrue()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act & Assert
            Assert.IsTrue(debt.IsPaymentDue(90));  // Day 90 (end of quarter)
            Assert.IsTrue(debt.IsPaymentDue(180)); // Day 180 (end of quarter 2)
            Assert.IsTrue(debt.IsPaymentDue(270)); // Day 270 (end of quarter 3)
        }

        [Test]
        public void IsPaymentDue_NotOnQuarterDay_ReturnsFalse()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act & Assert
            Assert.IsFalse(debt.IsPaymentDue(45));  // Mid-quarter
            Assert.IsFalse(debt.IsPaymentDue(89));  // Before quarter end
            Assert.IsFalse(debt.IsPaymentDue(91));  // After quarter end
        }

        [Test]
        public void ProcessQuarterlyPayment_WithSufficientFunds_MakesPayment()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool success = debt.ProcessQuarterlyPayment(90);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(9000, debt.currentBalance);
            Assert.AreEqual(DebtState.Active, debt.state);
        }

        [Test]
        public void ProcessQuarterlyPayment_WithInsufficientFunds_TriggersGameOver()
        {
            // Arrange
            var debt = new Debt(500, 1000, 0.05f); // Balance less than quarterly payment
            GameOverEvent? capturedEvent = null;
            EventSystem.Instance.Subscribe<GameOverEvent>(evt => capturedEvent = evt);

            // Act
            bool success = debt.ProcessQuarterlyPayment(90);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(DebtState.Overdue, debt.state);
            Assert.IsNotNull(capturedEvent);
            Assert.IsTrue(capturedEvent.Value.Reason.Contains("Failed to make quarterly debt payment"));
        }

        [Test]
        public void ProcessQuarterlyPayment_NotOnQuarterDay_SkipsPayment()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            bool success = debt.ProcessQuarterlyPayment(45); // Not a quarter day

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(10000, debt.currentBalance); // No payment made
            Assert.AreEqual(0, debt.paymentHistory.Count);
        }

        [Test]
        public void ApplyInterest_IncreasesBalance()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            debt.ApplyInterest();

            // Assert
            // Interest = 10000 * 0.05 / 4 = 125 (quarterly rate)
            Assert.AreEqual(10125, debt.currentBalance);
        }

        [Test]
        public void ApplyInterest_RoundsToNearestInteger()
        {
            // Arrange
            var debt = new Debt(10333, 1000, 0.05f); // Will produce fractional interest

            // Act
            debt.ApplyInterest();

            // Assert
            // Interest = 10333 * 0.05 / 4 = 129.1625 -> rounds to 129
            Assert.AreEqual(10462, debt.currentBalance);
        }

        [Test]
        public void MultiplePayments_TracksHistory()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);

            // Act
            debt.MakePayment(1000);
            debt.MakePayment(2000);
            debt.MakePayment(1500);

            // Assert
            Assert.AreEqual(3, debt.paymentHistory.Count);
            Assert.AreEqual(5500, debt.currentBalance);
            Assert.AreEqual(1000, debt.paymentHistory[0].amount);
            Assert.AreEqual(2000, debt.paymentHistory[1].amount);
            Assert.AreEqual(1500, debt.paymentHistory[2].amount);
        }

        [Test]
        public void PaymentToZero_SetsStateToPaid()
        {
            // Arrange
            var debt = new Debt(5000, 1000, 0.05f);

            // Act
            debt.MakePayment(5000);

            // Assert
            Assert.AreEqual(0, debt.currentBalance);
            Assert.AreEqual(DebtState.Paid, debt.state);
        }

        [Test]
        public void InterestApplication_AfterMultiplePayments()
        {
            // Arrange
            var debt = new Debt(10000, 1000, 0.05f);
            debt.MakePayment(2000); // Balance: 8000

            // Act
            debt.ApplyInterest(); // Interest on 8000: 100

            // Assert
            Assert.AreEqual(8100, debt.currentBalance);
        }
    }
}
