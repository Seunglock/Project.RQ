using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for displaying debt information, payment status, and history
    /// Provides interface for manual debt payments
    /// </summary>
    public class DebtUI : MonoBehaviour
    {
        [Header("Status UI Elements")]
        [SerializeField] private TMP_Text currentBalanceLabel;
        [SerializeField] private TMP_Text quarterlyPaymentLabel;
        [SerializeField] private TMP_Text daysUntilPaymentLabel;
        [SerializeField] private TMP_Text projectedBalanceLabel;
        [SerializeField] private TMP_Text debtStateLabel;

        [Header("Payment Input")]
        [SerializeField] private TMP_InputField paymentAmountField;
        [SerializeField] private Button manualPaymentButton;

        [Header("Payment History")]
        [SerializeField] private Transform paymentHistoryContainer;
        [SerializeField] private GameObject paymentHistoryItemPrefab;
        [SerializeField] private ScrollRect paymentHistoryScrollView;

        [Header("Settings")]
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float refreshInterval = 1f;

        private float refreshTimer = 0f;

        private void Start()
        {
            // Set up button handlers
            if (manualPaymentButton != null)
            {
                manualPaymentButton.onClick.AddListener(OnManualPaymentClicked);
            }

            RefreshUI();
        }

        private void OnEnable()
        {
            // Subscribe to debt events
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Subscribe<DebtPaymentEvent>(OnDebtPayment);
                EventSystem.Instance.Subscribe<DebtPaidOffEvent>(OnDebtPaidOff);
                EventSystem.Instance.Subscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Unsubscribe<DebtPaymentEvent>(OnDebtPayment);
                EventSystem.Instance.Unsubscribe<DebtPaidOffEvent>(OnDebtPaidOff);
                EventSystem.Instance.Unsubscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
            }
        }

        private void Update()
        {
            if (autoRefresh)
            {
                refreshTimer += Time.deltaTime;
                if (refreshTimer >= refreshInterval)
                {
                    refreshTimer = 0f;
                    RefreshUI();
                }
            }
        }

        private void OnManualPaymentClicked()
        {
            if (paymentAmountField == null) return;

            if (int.TryParse(paymentAmountField.text, out int amount))
            {
                if (amount > 0)
                {
                    bool success = DebtService.Instance.MakeManualPayment(amount);
                    if (success)
                    {
                        Debug.Log($"Manual payment of ${amount} successful");
                        paymentAmountField.text = "";
                    }
                    else
                    {
                        Debug.LogWarning("Manual payment failed - check gold balance");
                    }
                }
                else
                {
                    Debug.LogWarning("Payment amount must be positive");
                }
            }
            else
            {
                Debug.LogWarning("Invalid payment amount");
            }

            RefreshUI();
        }

        private void OnDebtPayment(DebtPaymentEvent evt)
        {
            RefreshUI();
        }

        private void OnDebtPaidOff(DebtPaidOffEvent evt)
        {
            RefreshUI();
            Debug.Log("Congratulations! Debt fully paid!");
        }

        private void OnQuarterAdvanced(QuarterAdvancedEvent evt)
        {
            RefreshUI();
        }

        /// <summary>
        /// Refresh all UI elements with current debt data
        /// </summary>
        public void RefreshUI()
        {
            if (DebtService.Instance == null) return;

            var debt = DebtService.Instance.GetCurrentDebt();
            if (debt == null) return;

            // Update labels
            if (currentBalanceLabel != null)
                currentBalanceLabel.text = $"Current Balance: ${debt.currentBalance:N0}";

            if (quarterlyPaymentLabel != null)
                quarterlyPaymentLabel.text = $"Quarterly Payment: ${debt.quarterlyPayment:N0}";

            if (daysUntilPaymentLabel != null)
            {
                int daysUntil = DebtService.Instance.GetDaysUntilPayment();
                daysUntilPaymentLabel.text = $"Days Until Payment: {daysUntil}";
            }

            if (projectedBalanceLabel != null)
            {
                int projected = DebtService.Instance.GetProjectedBalanceAfterInterest();
                projectedBalanceLabel.text = $"Projected Balance (with interest): ${projected:N0}";
            }

            if (debtStateLabel != null)
            {
                debtStateLabel.text = $"State: {debt.state}";

                // Color code based on state
                if (debt.state == DebtState.Paid)
                {
                    debtStateLabel.color = Color.green;
                }
                else if (debt.state == DebtState.Overdue)
                {
                    debtStateLabel.color = Color.red;
                }
                else
                {
                    debtStateLabel.color = Color.white;
                }
            }

            // Refresh payment history
            RefreshPaymentHistory();
        }

        private void RefreshPaymentHistory()
        {
            if (paymentHistoryContainer == null) return;

            // Clear existing items
            foreach (Transform child in paymentHistoryContainer)
            {
                Destroy(child.gameObject);
            }

            var history = DebtService.Instance.GetPaymentHistory();
            if (history == null || history.Count == 0)
            {
                // Create "no history" text if needed
                GameObject noHistoryObj = new GameObject("NoHistoryText");
                noHistoryObj.transform.SetParent(paymentHistoryContainer, false);
                TMP_Text noHistoryText = noHistoryObj.AddComponent<TMP_Text>();
                noHistoryText.text = "No payment history yet";
                noHistoryText.fontSize = 14;
                noHistoryText.color = Color.gray;
                return;
            }

            // Display payment history in reverse order (newest first)
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var record = history[i];
                CreatePaymentHistoryItem(record);
            }
        }

        private void CreatePaymentHistoryItem(PaymentRecord record)
        {
            GameObject item;

            if (paymentHistoryItemPrefab != null)
            {
                item = Instantiate(paymentHistoryItemPrefab, paymentHistoryContainer);
            }
            else
            {
                // Create item programmatically if no prefab
                item = new GameObject("PaymentHistoryItem");
                item.transform.SetParent(paymentHistoryContainer, false);

                var layoutGroup = item.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = 10f;

                // Date
                GameObject dateObj = new GameObject("Date");
                dateObj.transform.SetParent(item.transform, false);
                TMP_Text dateText = dateObj.AddComponent<TMP_Text>();
                dateText.text = record.date.ToString("yyyy-MM-dd HH:mm");
                dateText.fontSize = 14;

                // Amount
                GameObject amountObj = new GameObject("Amount");
                amountObj.transform.SetParent(item.transform, false);
                TMP_Text amountText = amountObj.AddComponent<TMP_Text>();
                amountText.text = $"${record.amount:N0}";
                amountText.fontSize = 14;
                amountText.color = Color.green;

                // Remaining Balance
                GameObject balanceObj = new GameObject("Balance");
                balanceObj.transform.SetParent(item.transform, false);
                TMP_Text balanceText = balanceObj.AddComponent<TMP_Text>();
                balanceText.text = $"Balance: ${record.remainingBalance:N0}";
                balanceText.fontSize = 14;
            }
        }

        /// <summary>
        /// Show or hide the debt UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
