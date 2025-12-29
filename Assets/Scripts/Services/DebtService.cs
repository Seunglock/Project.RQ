using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Service for managing debt operations, quarterly payments, and game over conditions
    /// Integrates with GameManager for gold management and quarter tracking
    /// </summary>
    public class DebtService : MonoBehaviour
    {
        private static DebtService _instance;
        public static DebtService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DebtService");
                    _instance = go.AddComponent<DebtService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Debt _currentDebt;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initialize debt service with new game or loaded debt
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Subscribe to quarter advance events
            EventSystem.Instance.Subscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
            EventSystem.Instance.Subscribe<GameStartedEvent>(OnGameStarted);

            _isInitialized = true;
            Debug.Log("DebtService initialized");
        }

        /// <summary>
        /// Initialize new debt when game starts
        /// </summary>
        private void OnGameStarted(GameStartedEvent evt)
        {
            // Create initial debt from game state
            if (GameManager.Instance != null)
            {
                var gameState = GameManager.Instance.CurrentGameState;
                if (gameState != null)
                {
                    _currentDebt = new Debt(
                        gameState.debtBalance,
                        gameState.quarterlyPayment,
                        Constants.DEFAULT_INTEREST_RATE
                    );
                    Debug.Log($"Debt initialized: Balance={_currentDebt.currentBalance}, Quarterly Payment={_currentDebt.quarterlyPayment}");
                }
            }
        }

        /// <summary>
        /// Handle quarterly payment when quarter advances
        /// </summary>
        private void OnQuarterAdvanced(QuarterAdvancedEvent evt)
        {
            if (_currentDebt == null)
            {
                Debug.LogWarning("DebtService: No debt to process");
                return;
            }

            Debug.Log($"Processing quarterly debt payment for Quarter {evt.Quarter}");

            // Apply interest first
            _currentDebt.ApplyInterest();
            Debug.Log($"Interest applied. New balance: {_currentDebt.currentBalance}");

            // Check if player has sufficient gold
            if (GameManager.Instance.PlayerGold < _currentDebt.quarterlyPayment)
            {
                Debug.LogError($"Insufficient gold for quarterly payment. Required: {_currentDebt.quarterlyPayment}, Available: {GameManager.Instance.PlayerGold}");
                _currentDebt.state = DebtState.Overdue;
                EventSystem.Instance.Publish(new GameOverEvent { Reason = "Failed to make quarterly debt payment - Insufficient funds" });
                return;
            }

            // Process the payment
            int paymentAmount = _currentDebt.quarterlyPayment;
            if (_currentDebt.currentBalance < paymentAmount)
            {
                paymentAmount = _currentDebt.currentBalance;
            }

            // Deduct gold from player
            GameManager.Instance.ModifyGold(-paymentAmount);

            // Make the payment
            bool success = _currentDebt.MakePayment(paymentAmount);
            if (success)
            {
                Debug.Log($"Quarterly payment successful. Amount: {paymentAmount}, Remaining balance: {_currentDebt.currentBalance}");

                if (_currentDebt.state == DebtState.Paid)
                {
                    Debug.Log("Debt fully paid! Congratulations!");
                    EventSystem.Instance.Publish(new DebtPaidOffEvent { });
                }
            }
            else
            {
                Debug.LogError("Failed to process quarterly payment");
            }
        }

        /// <summary>
        /// Make a manual payment towards the debt
        /// </summary>
        public bool MakeManualPayment(int amount)
        {
            if (_currentDebt == null)
            {
                Debug.LogWarning("No debt to pay");
                return false;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Payment amount must be positive");
                return false;
            }

            if (GameManager.Instance.PlayerGold < amount)
            {
                Debug.LogWarning($"Insufficient gold for payment. Required: {amount}, Available: {GameManager.Instance.PlayerGold}");
                return false;
            }

            // Cap payment at remaining balance
            int actualPayment = Mathf.Min(amount, _currentDebt.currentBalance);

            // Deduct gold
            GameManager.Instance.ModifyGold(-actualPayment);

            // Make payment
            bool success = _currentDebt.MakePayment(actualPayment);
            if (success)
            {
                Debug.Log($"Manual payment successful. Amount: {actualPayment}, Remaining balance: {_currentDebt.currentBalance}");

                if (_currentDebt.state == DebtState.Paid)
                {
                    EventSystem.Instance.Publish(new DebtPaidOffEvent { });
                }
            }

            return success;
        }

        /// <summary>
        /// Get the current debt object
        /// </summary>
        public Debt GetCurrentDebt()
        {
            return _currentDebt;
        }

        /// <summary>
        /// Set debt (for testing purposes)
        /// </summary>
        public void SetDebt(Debt debt)
        {
            _currentDebt = debt;
        }

        /// <summary>
        /// Get current debt balance
        /// </summary>
        public int GetCurrentBalance()
        {
            return _currentDebt?.currentBalance ?? 0;
        }

        /// <summary>
        /// Get quarterly payment amount
        /// </summary>
        public int GetQuarterlyPayment()
        {
            return _currentDebt?.quarterlyPayment ?? 0;
        }

        /// <summary>
        /// Get payment history
        /// </summary>
        public System.Collections.Generic.List<PaymentRecord> GetPaymentHistory()
        {
            return _currentDebt?.paymentHistory ?? new System.Collections.Generic.List<PaymentRecord>();
        }

        /// <summary>
        /// Check if payment is overdue
        /// </summary>
        public bool IsOverdue()
        {
            return _currentDebt?.state == DebtState.Overdue;
        }

        /// <summary>
        /// Check if debt is fully paid
        /// </summary>
        public bool IsPaid()
        {
            return _currentDebt?.state == DebtState.Paid;
        }

        /// <summary>
        /// Get days until next payment
        /// </summary>
        public int GetDaysUntilPayment()
        {
            if (GameManager.Instance == null) return 0;

            int currentDay = GameManager.Instance.CurrentDay;
            int daysIntoQuarter = currentDay % Constants.DAYS_PER_QUARTER;
            return Constants.DAYS_PER_QUARTER - daysIntoQuarter;
        }

        /// <summary>
        /// Calculate projected balance after interest (for UI display)
        /// </summary>
        public int GetProjectedBalanceAfterInterest()
        {
            if (_currentDebt == null) return 0;

            int balance = _currentDebt.currentBalance;
            int interestAmount = Mathf.RoundToInt(balance * _currentDebt.interestRate / 4f);
            return balance + interestAmount;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                EventSystem.Instance.Unsubscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
                EventSystem.Instance.Unsubscribe<GameStartedEvent>(OnGameStarted);
                _instance = null;
            }
        }
    }

    // Debt paid off event
    public struct DebtPaidOffEvent { }
}
