using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for displaying character relationships and status
    /// </summary>
    public class RelationshipUI : MonoBehaviour
    {
        private RelationshipService relationshipService;
        private Character playerCharacter;

        private VisualElement root;
        private Label playerNameLabel;
        private Label alignmentLabel;
        private VisualElement relationshipListContainer;
        private Button refreshButton;

        private Dictionary<string, Character> npcCharacters = new Dictionary<string, Character>();

        private void Awake()
        {
            relationshipService = FindObjectOfType<RelationshipService>();
            if (relationshipService == null)
            {
                Debug.LogError("RelationshipUI: RelationshipService not found");
            }

            SubscribeToEvents();
        }

        private void OnEnable()
        {
            InitializeUI();
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventSystem.Instance.Subscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            EventSystem.Instance.Subscribe<AlignmentChangedEvent>(OnAlignmentChanged);
            EventSystem.Instance.Subscribe<SpecialRelationshipEvent>(OnSpecialRelationshipEvent);
        }

        private void UnsubscribeFromEvents()
        {
            EventSystem.Instance.Unsubscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            EventSystem.Instance.Unsubscribe<AlignmentChangedEvent>(OnAlignmentChanged);
            EventSystem.Instance.Unsubscribe<SpecialRelationshipEvent>(OnSpecialRelationshipEvent);
        }

        private void InitializeUI()
        {
            root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("RelationshipUI: Root visual element not found");
                return;
            }

            playerNameLabel = root.Q<Label>("player-name");
            alignmentLabel = root.Q<Label>("alignment-label");
            relationshipListContainer = root.Q<VisualElement>("relationship-list");
            refreshButton = root.Q<Button>("refresh-button");

            if (refreshButton != null)
            {
                refreshButton.clicked += RefreshDisplay;
            }
        }

        /// <summary>
        /// Set the player character
        /// </summary>
        public void SetPlayerCharacter(Character player)
        {
            playerCharacter = player;
            RefreshDisplay();
        }

        /// <summary>
        /// Register an NPC character for display
        /// </summary>
        public void RegisterNPC(Character npc)
        {
            if (npc == null || string.IsNullOrEmpty(npc.id))
            {
                Debug.LogError("RelationshipUI: Cannot register null or invalid NPC");
                return;
            }

            npcCharacters[npc.id] = npc;
            RefreshDisplay();
        }

        /// <summary>
        /// Unregister an NPC character
        /// </summary>
        public void UnregisterNPC(string npcId)
        {
            if (npcCharacters.ContainsKey(npcId))
            {
                npcCharacters.Remove(npcId);
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Refresh the entire display
        /// </summary>
        private void RefreshDisplay()
        {
            if (playerCharacter == null) return;

            UpdatePlayerInfo();
            UpdateRelationshipList();
        }

        /// <summary>
        /// Update player information display
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (playerNameLabel != null)
            {
                playerNameLabel.text = playerCharacter.name;
            }

            if (alignmentLabel != null)
            {
                alignmentLabel.text = GetAlignmentText(playerCharacter.alignment);
            }
        }

        /// <summary>
        /// Update the list of relationships
        /// </summary>
        private void UpdateRelationshipList()
        {
            if (relationshipListContainer == null) return;

            relationshipListContainer.Clear();

            // Sort NPCs by relationship value (highest first)
            var sortedNpcs = npcCharacters.Values
                .OrderByDescending(npc => playerCharacter.GetRelationship(npc.id))
                .ToList();

            foreach (var npc in sortedNpcs)
            {
                CreateRelationshipEntry(npc);
            }
        }

        /// <summary>
        /// Create a relationship entry for an NPC
        /// </summary>
        private void CreateRelationshipEntry(Character npc)
        {
            var container = new VisualElement();
            container.AddToClassList("relationship-entry");

            // NPC name
            var nameLabel = new Label(npc.name);
            nameLabel.AddToClassList("npc-name");
            container.Add(nameLabel);

            // Relationship value
            int relationshipValue = playerCharacter.GetRelationship(npc.id);
            var valueLabel = new Label($"{relationshipValue:+0;-#}");
            valueLabel.AddToClassList("relationship-value");

            // Apply color based on relationship value
            if (relationshipValue >= 50)
            {
                valueLabel.AddToClassList("relationship-high");
            }
            else if (relationshipValue >= 0)
            {
                valueLabel.AddToClassList("relationship-neutral");
            }
            else
            {
                valueLabel.AddToClassList("relationship-low");
            }

            container.Add(valueLabel);

            // Relationship status description
            if (relationshipService != null)
            {
                var statusLabel = new Label(relationshipService.GetRelationshipStatus(relationshipValue));
                statusLabel.AddToClassList("relationship-status");
                container.Add(statusLabel);
            }

            // Relationship bar (visual representation)
            var barContainer = new VisualElement();
            barContainer.AddToClassList("relationship-bar-container");

            var bar = new VisualElement();
            bar.AddToClassList("relationship-bar");

            // Scale bar based on relationship value (-100 to 100 -> 0% to 100%)
            float barWidth = ((relationshipValue + 100f) / 200f) * 100f;
            bar.style.width = new StyleLength(new Length(barWidth, LengthUnit.Percent));

            if (relationshipValue >= 50)
            {
                bar.AddToClassList("bar-high");
            }
            else if (relationshipValue >= 0)
            {
                bar.AddToClassList("bar-neutral");
            }
            else
            {
                bar.AddToClassList("bar-low");
            }

            barContainer.Add(bar);
            container.Add(barContainer);

            // Interaction button
            var interactButton = new Button(() => OnInteractWithNPC(npc.id));
            interactButton.text = "Talk";
            interactButton.AddToClassList("interact-button");
            container.Add(interactButton);

            relationshipListContainer.Add(container);
        }

        /// <summary>
        /// Get alignment text description
        /// </summary>
        private string GetAlignmentText(AlignmentFlags alignment)
        {
            if (alignment == AlignmentFlags.Neutral)
            {
                return "Neutral";
            }

            List<string> alignments = new List<string>();

            if ((alignment & AlignmentFlags.Order) == AlignmentFlags.Order)
            {
                alignments.Add("Order");
            }

            if ((alignment & AlignmentFlags.Chaos) == AlignmentFlags.Chaos)
            {
                alignments.Add("Chaos");
            }

            return string.Join(" / ", alignments);
        }

        /// <summary>
        /// Handle interaction button click
        /// </summary>
        private void OnInteractWithNPC(string npcId)
        {
            if (!npcCharacters.ContainsKey(npcId)) return;

            // Publish event to trigger dialogue
            EventSystem.Instance.Publish(new NPCInteractionRequestedEvent
            {
                NpcId = npcId
            });
        }

        /// <summary>
        /// Handle relationship changed event
        /// </summary>
        private void OnRelationshipChanged(RelationshipChangedEvent e)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Handle alignment changed event
        /// </summary>
        private void OnAlignmentChanged(AlignmentChangedEvent e)
        {
            if (e.CharacterId == playerCharacter?.id)
            {
                UpdatePlayerInfo();
            }
        }

        /// <summary>
        /// Handle special relationship event
        /// </summary>
        private void OnSpecialRelationshipEvent(SpecialRelationshipEvent e)
        {
            // Show notification for special relationship milestones
            ShowRelationshipNotification(e);
        }

        /// <summary>
        /// Show a notification for relationship milestones
        /// </summary>
        private void ShowRelationshipNotification(SpecialRelationshipEvent e)
        {
            if (!npcCharacters.ContainsKey(e.CharacterId)) return;

            string npcName = npcCharacters[e.CharacterId].name;
            string message = "";

            switch (e.EventType)
            {
                case "HighRelationship":
                    message = $"Your relationship with {npcName} has become very strong!";
                    break;
                case "LowRelationship":
                    message = $"Your relationship with {npcName} has deteriorated significantly.";
                    break;
                case "MaxRelationship":
                    message = $"You've reached maximum friendship with {npcName}!";
                    break;
                case "MinRelationship":
                    message = $"Your relationship with {npcName} couldn't be worse.";
                    break;
            }

            // Log notification (in a full implementation, this would show a toast/popup)
            Debug.Log($"[Relationship Notification] {message}");
        }

        /// <summary>
        /// Get all NPCs above a relationship threshold
        /// </summary>
        public List<Character> GetNPCsAboveThreshold(int threshold)
        {
            if (playerCharacter == null || relationshipService == null) return new List<Character>();

            var npcIds = relationshipService.GetNPCsAboveThreshold(playerCharacter, threshold);
            return npcIds.Select(id => npcCharacters.ContainsKey(id) ? npcCharacters[id] : null)
                         .Where(npc => npc != null)
                         .ToList();
        }

        /// <summary>
        /// Get all NPCs below a relationship threshold
        /// </summary>
        public List<Character> GetNPCsBelowThreshold(int threshold)
        {
            if (playerCharacter == null || relationshipService == null) return new List<Character>();

            var npcIds = relationshipService.GetNPCsBelowThreshold(playerCharacter, threshold);
            return npcIds.Select(id => npcCharacters.ContainsKey(id) ? npcCharacters[id] : null)
                         .Where(npc => npc != null)
                         .ToList();
        }
    }

    // Event for requesting NPC interaction
    public struct NPCInteractionRequestedEvent
    {
        public string NpcId;
    }
}
