using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// UI controller for displaying and interacting with dialogue conversations
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private RelationshipService relationshipService;
        private Character playerCharacter;

        private VisualElement root;
        private Label npcNameLabel;
        private Label dialogueTextLabel;
        private VisualElement choicesContainer;
        private Button closeButton;

        private string currentNpcId;
        private DialogueNode currentNode;

        private void Awake()
        {
            relationshipService = FindObjectOfType<RelationshipService>();
            if (relationshipService == null)
            {
                Debug.LogError("DialogueUI: RelationshipService not found");
            }

            SubscribeToEvents();
        }

        private void OnEnable()
        {
            InitializeUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventSystem.Instance.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            EventSystem.Instance.Subscribe<DialogueProgressEvent>(OnDialogueProgress);
        }

        private void UnsubscribeFromEvents()
        {
            EventSystem.Instance.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
            EventSystem.Instance.Unsubscribe<DialogueProgressEvent>(OnDialogueProgress);
        }

        private void InitializeUI()
        {
            root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("DialogueUI: Root visual element not found");
                return;
            }

            npcNameLabel = root.Q<Label>("npc-name");
            dialogueTextLabel = root.Q<Label>("dialogue-text");
            choicesContainer = root.Q<VisualElement>("choices-container");
            closeButton = root.Q<Button>("close-button");

            if (closeButton != null)
            {
                closeButton.clicked += OnCloseButtonClicked;
            }

            Hide();
        }

        /// <summary>
        /// Set the player character for dialogue interactions
        /// </summary>
        public void SetPlayerCharacter(Character player)
        {
            playerCharacter = player;
        }

        /// <summary>
        /// Start a dialogue with an NPC
        /// </summary>
        public void StartDialogue(string npcId, string npcName, DialogueNode startNode)
        {
            if (startNode == null)
            {
                Debug.LogError("DialogueUI: Cannot start dialogue - start node is null");
                return;
            }

            currentNpcId = npcId;
            currentNode = startNode;

            if (npcNameLabel != null)
            {
                npcNameLabel.text = npcName;
            }

            DisplayDialogueNode(startNode);
            Show();
        }

        /// <summary>
        /// Display a dialogue node with its text and choices
        /// </summary>
        private void DisplayDialogueNode(DialogueNode node)
        {
            if (node == null) return;

            // Update dialogue text
            if (dialogueTextLabel != null)
            {
                dialogueTextLabel.text = node.text;
            }

            // Clear existing choices
            if (choicesContainer != null)
            {
                choicesContainer.Clear();

                // Create choice buttons
                if (node.choices != null)
                {
                    foreach (var choice in node.choices)
                    {
                        CreateChoiceButton(choice);
                    }
                }
            }
        }

        /// <summary>
        /// Create a button for a dialogue choice
        /// </summary>
        private void CreateChoiceButton(DialogueChoice choice)
        {
            var button = new Button();
            button.text = choice.text;
            button.AddToClassList("dialogue-choice-button");

            // Add visual indicator for relationship/alignment changes
            if (choice.relationshipChange != 0)
            {
                string indicator = choice.relationshipChange > 0 ? " ♥" : " ☠";
                button.text += indicator;
            }

            if (choice.alignmentChange == AlignmentFlags.Order)
            {
                button.text += " ⚖";
            }
            else if (choice.alignmentChange == AlignmentFlags.Chaos)
            {
                button.text += " ⚡";
            }

            button.clicked += () => OnChoiceSelected(choice);
            choicesContainer.Add(button);
        }

        /// <summary>
        /// Handle dialogue choice selection
        /// </summary>
        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (playerCharacter == null || relationshipService == null)
            {
                Debug.LogError("DialogueUI: Player character or relationship service is null");
                return;
            }

            // Process the choice through the relationship service
            relationshipService.ProcessDialogueChoice(playerCharacter, currentNpcId, choice);

            // If there's a next node, continue the dialogue
            // Otherwise, end the dialogue
            if (string.IsNullOrEmpty(choice.nextNodeId))
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Handle dialogue started event
        /// </summary>
        private void OnDialogueStarted(DialogueStartedEvent e)
        {
            currentNpcId = e.NpcId;
            currentNode = relationshipService?.GetCurrentDialogueNode(e.NpcId);
            if (currentNode != null)
            {
                DisplayDialogueNode(currentNode);
            }
        }

        /// <summary>
        /// Handle dialogue progress event
        /// </summary>
        private void OnDialogueProgress(DialogueProgressEvent e)
        {
            if (e.NpcId == currentNpcId)
            {
                // In a full implementation, this would load the next node from a dialogue database
                // For now, we just acknowledge the progress
                Debug.Log($"Dialogue progressed to node: {e.NextNodeId}");
            }
        }

        /// <summary>
        /// End the current dialogue
        /// </summary>
        private void EndDialogue()
        {
            if (relationshipService != null && !string.IsNullOrEmpty(currentNpcId))
            {
                relationshipService.ResetDialogue(currentNpcId);
            }

            currentNpcId = null;
            currentNode = null;
            Hide();
        }

        /// <summary>
        /// Handle close button click
        /// </summary>
        private void OnCloseButtonClicked()
        {
            EndDialogue();
        }

        /// <summary>
        /// Show the dialogue UI
        /// </summary>
        private void Show()
        {
            if (root != null)
            {
                root.style.display = DisplayStyle.Flex;
            }
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the dialogue UI
        /// </summary>
        private void Hide()
        {
            if (root != null)
            {
                root.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Check if dialogue is currently active
        /// </summary>
        public bool IsDialogueActive()
        {
            return currentNode != null && !string.IsNullOrEmpty(currentNpcId);
        }

        /// <summary>
        /// Get the current NPC ID in dialogue
        /// </summary>
        public string GetCurrentNpcId()
        {
            return currentNpcId;
        }
    }
}
