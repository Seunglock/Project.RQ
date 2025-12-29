using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist.UI
{
    /// <summary>
    /// UI controller for displaying random events
    /// </summary>
    public class RandomEventUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement eventContainer;
        private Label eventTitleLabel;
        private Label eventDescriptionLabel;
        private Button eventCloseButton;
        private VisualElement eventHistoryList;

        private Queue<RandomEvent> pendingEvents = new Queue<RandomEvent>();
        private bool isDisplayingEvent = false;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found on RandomEventUI");
                return;
            }
        }

        void OnEnable()
        {
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            // Get UI elements
            eventContainer = root.Q<VisualElement>("EventContainer");
            eventTitleLabel = root.Q<Label>("EventTitle");
            eventDescriptionLabel = root.Q<Label>("EventDescription");
            eventCloseButton = root.Q<Button>("EventCloseButton");
            eventHistoryList = root.Q<VisualElement>("EventHistoryList");

            // Hide event container by default
            if (eventContainer != null)
            {
                eventContainer.style.display = DisplayStyle.None;
            }

            // Register button callbacks
            if (eventCloseButton != null)
            {
                eventCloseButton.clicked += OnEventClosed;
            }

            // Subscribe to random event occurrences
            EventSystem.Instance.Subscribe<RandomEventOccurredEvent>(OnRandomEventOccurred);

            // Load and display event history
            RefreshEventHistory();
        }

        void OnDisable()
        {
            if (eventCloseButton != null)
            {
                eventCloseButton.clicked -= OnEventClosed;
            }

            EventSystem.Instance.Unsubscribe<RandomEventOccurredEvent>(OnRandomEventOccurred);
        }

        void Update()
        {
            // If not currently displaying an event and there are pending events, show next one
            if (!isDisplayingEvent && pendingEvents.Count > 0)
            {
                RandomEvent nextEvent = pendingEvents.Dequeue();
                DisplayEvent(nextEvent);
            }
        }

        private void OnRandomEventOccurred(RandomEventOccurredEvent evt)
        {
            // Queue the event for display
            pendingEvents.Enqueue(evt.Event);
        }

        private void DisplayEvent(RandomEvent evt)
        {
            if (eventContainer == null || eventTitleLabel == null || eventDescriptionLabel == null)
            {
                Debug.LogWarning("Event UI elements not found");
                return;
            }

            isDisplayingEvent = true;

            // Set event text
            eventTitleLabel.text = evt.title;
            eventDescriptionLabel.text = evt.description;

            // Color based on positive/negative
            if (evt.isPositive)
            {
                eventTitleLabel.style.color = new StyleColor(new Color(0.2f, 0.8f, 0.2f)); // Green
            }
            else
            {
                eventTitleLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f)); // Red
            }

            // Show the event container
            eventContainer.style.display = DisplayStyle.Flex;

            // Refresh history to include this new event
            RefreshEventHistory();
        }

        private void OnEventClosed()
        {
            if (eventContainer != null)
            {
                eventContainer.style.display = DisplayStyle.None;
            }
            isDisplayingEvent = false;
        }

        private void RefreshEventHistory()
        {
            if (eventHistoryList == null) return;

            // Clear existing history
            eventHistoryList.Clear();

            // Get event history from service
            List<RandomEvent> history = RandomEventService.Instance.GetEventHistory();

            // Display last 10 events in reverse order (most recent first)
            int startIndex = Mathf.Max(0, history.Count - 10);
            for (int i = history.Count - 1; i >= startIndex; i--)
            {
                RandomEvent evt = history[i];
                
                var historyItem = new VisualElement();
                historyItem.AddToClassList("event-history-item");

                var titleLabel = new Label(evt.title);
                titleLabel.AddToClassList("event-history-title");
                if (evt.isPositive)
                {
                    titleLabel.style.color = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                }
                else
                {
                    titleLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                }

                var descLabel = new Label(evt.description);
                descLabel.AddToClassList("event-history-description");

                historyItem.Add(titleLabel);
                historyItem.Add(descLabel);
                eventHistoryList.Add(historyItem);
            }
        }

        /// <summary>
        /// Clear all event history (for new game)
        /// </summary>
        public void ClearHistory()
        {
            RandomEventService.Instance.ClearHistory();
            RefreshEventHistory();
        }
    }
}
