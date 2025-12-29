using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for displaying debt information, payment status, and history
    /// Provides interface for manual debt payments
    /// </summary>
    public class DebtUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Settings")]
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float refreshInterval = 1f;

        private VisualElement root;
        private Label currentBalanceLabel;
        private Label quarterlyPaymentLabel;
        private Label daysUntilPaymentLabel;
        private Label projectedBalanceLabel;
        private Label debtStateLabel;
        private Button manualPaymentButton;
        private TextField paymentAmountField;
        private ScrollView paymentHistoryScrollView;
        private VisualElement paymentHistoryContainer;

        private float refreshTimer = 0f;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null)
            {
                InitializeUI();
            }
        }

        private void OnEnable()
        {
            // Subscribe to debt events
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(OnDebtPayment);
            EventSystem.Instance.Subscribe<DebtPaidOffEvent>(OnDebtPaidOff);
            EventSystem.Instance.Subscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);

            RefreshUI();
        }

        private void OnDisable()
        {
            EventSystem.Instance.Unsubscribe<DebtPaymentEvent>(OnDebtPayment);
            EventSystem.Instance.Unsubscribe<DebtPaidOffEvent>(OnDebtPaidOff);
            EventSystem.Instance.Unsubscribe<QuarterAdvancedEvent>(OnQuarterAdvanced);
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

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;

            // Create UI structure programmatically
            CreateDebtUI();

            // Set up button handlers
            if (manualPaymentButton != null)
            {
                manualPaymentButton.clicked += OnManualPaymentClicked;
            }
        }

        private void CreateDebtUI()
        {
            // Main container
            var mainContainer = new VisualElement();
            mainContainer.name = "debt-container";
            mainContainer.style.paddingTop = new StyleLength(20);
            mainContainer.style.paddingBottom = new StyleLength(20);
            mainContainer.style.paddingLeft = new StyleLength(20);
            mainContainer.style.paddingRight = new StyleLength(20);
            mainContainer.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.9f));

            // Title
            var titleLabel = new Label("Debt Management");
            titleLabel.style.fontSize = 24;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            mainContainer.Add(titleLabel);

            // Debt Status Section
            var statusSection = CreateStatusSection();
            mainContainer.Add(statusSection);

            // Payment Input Section
            var paymentSection = CreatePaymentSection();
            mainContainer.Add(paymentSection);

            // Payment History Section
            var historySection = CreateHistorySection();
            mainContainer.Add(historySection);

            root.Add(mainContainer);
        }

        private VisualElement CreateStatusSection()
        {
            var section = new VisualElement();
            section.name = "status-section";
            section.style.marginBottom = 20;
            section.style.paddingTop = new StyleLength(15);
            section.style.paddingBottom = new StyleLength(15);
            section.style.paddingLeft = new StyleLength(15);
            section.style.paddingRight = new StyleLength(15);
            section.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));

            var sectionTitle = new Label("Current Status");
            sectionTitle.style.fontSize = 18;
            sectionTitle.style.marginBottom = 10;
            section.Add(sectionTitle);

            currentBalanceLabel = CreateInfoLabel("Current Balance: $0");
            quarterlyPaymentLabel = CreateInfoLabel("Quarterly Payment: $0");
            daysUntilPaymentLabel = CreateInfoLabel("Days Until Payment: 0");
            projectedBalanceLabel = CreateInfoLabel("Projected Balance (with interest): $0");
            debtStateLabel = CreateInfoLabel("State: Active");

            section.Add(currentBalanceLabel);
            section.Add(quarterlyPaymentLabel);
            section.Add(daysUntilPaymentLabel);
            section.Add(projectedBalanceLabel);
            section.Add(debtStateLabel);

            return section;
        }

        private VisualElement CreatePaymentSection()
        {
            var section = new VisualElement();
            section.name = "payment-section";
            section.style.marginBottom = 20;
            section.style.paddingTop = new StyleLength(15);
            section.style.paddingBottom = new StyleLength(15);
            section.style.paddingLeft = new StyleLength(15);
            section.style.paddingRight = new StyleLength(15);
            section.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));

            var sectionTitle = new Label("Make Manual Payment");
            sectionTitle.style.fontSize = 18;
            sectionTitle.style.marginBottom = 10;
            section.Add(sectionTitle);

            paymentAmountField = new TextField("Payment Amount");
            paymentAmountField.style.marginBottom = 10;
            section.Add(paymentAmountField);

            manualPaymentButton = new Button { text = "Pay Debt" };
            manualPaymentButton.style.height = 40;
            section.Add(manualPaymentButton);

            return section;
        }

        private VisualElement CreateHistorySection()
        {
            var section = new VisualElement();
            section.name = "history-section";
            section.style.paddingTop = new StyleLength(15);
            section.style.paddingBottom = new StyleLength(15);
            section.style.paddingLeft = new StyleLength(15);
            section.style.paddingRight = new StyleLength(15);
            section.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));

            var sectionTitle = new Label("Payment History");
            sectionTitle.style.fontSize = 18;
            sectionTitle.style.marginBottom = 10;
            section.Add(sectionTitle);

            paymentHistoryScrollView = new ScrollView();
            paymentHistoryScrollView.style.height = 200;
            paymentHistoryContainer = new VisualElement();
            paymentHistoryScrollView.Add(paymentHistoryContainer);
            section.Add(paymentHistoryScrollView);

            return section;
        }

        private Label CreateInfoLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 14;
            label.style.marginBottom = 5;
            return label;
        }

        private void OnManualPaymentClicked()
        {
            if (int.TryParse(paymentAmountField.value, out int amount))
            {
                if (amount > 0)
                {
                    bool success = DebtService.Instance.MakeManualPayment(amount);
                    if (success)
                    {
                        Debug.Log($"Manual payment of ${amount} successful");
                        paymentAmountField.value = "";
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
                    debtStateLabel.style.color = new StyleColor(Color.green);
                }
                else if (debt.state == DebtState.Overdue)
                {
                    debtStateLabel.style.color = new StyleColor(Color.red);
                }
                else
                {
                    debtStateLabel.style.color = new StyleColor(Color.white);
                }
            }

            // Refresh payment history
            RefreshPaymentHistory();
        }

        private void RefreshPaymentHistory()
        {
            if (paymentHistoryContainer == null) return;

            paymentHistoryContainer.Clear();

            var history = DebtService.Instance.GetPaymentHistory();
            if (history == null || history.Count == 0)
            {
                var noHistoryLabel = new Label("No payment history yet");
                noHistoryLabel.style.fontSize = 12;
                noHistoryLabel.style.color = new StyleColor(Color.gray);
                paymentHistoryContainer.Add(noHistoryLabel);
                return;
            }

            // Display payment history in reverse order (newest first)
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var record = history[i];
                var recordElement = new VisualElement();
                recordElement.style.flexDirection = FlexDirection.Row;
                recordElement.style.marginBottom = 5;
                recordElement.style.paddingTop = new StyleLength(5);
                recordElement.style.paddingBottom = new StyleLength(5);
                recordElement.style.paddingLeft = new StyleLength(5);
                recordElement.style.paddingRight = new StyleLength(5);
                recordElement.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));

                var dateLabel = new Label(record.date.ToString("yyyy-MM-dd HH:mm"));
                dateLabel.style.width = 150;
                dateLabel.style.fontSize = 12;

                var amountLabel = new Label($"${record.amount:N0}");
                amountLabel.style.width = 100;
                amountLabel.style.fontSize = 12;
                amountLabel.style.color = new StyleColor(Color.green);

                var balanceLabel = new Label($"Balance: ${record.remainingBalance:N0}");
                balanceLabel.style.fontSize = 12;

                recordElement.Add(dateLabel);
                recordElement.Add(amountLabel);
                recordElement.Add(balanceLabel);

                paymentHistoryContainer.Add(recordElement);
            }
        }

        /// <summary>
        /// Show or hide the debt UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
