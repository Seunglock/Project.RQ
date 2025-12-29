using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Service for managing character relationships and dialogue interactions
    /// </summary>
    public class RelationshipService : MonoBehaviour
    {
        private Dictionary<string, DialogueNode> currentDialogues = new Dictionary<string, DialogueNode>();
        private Dictionary<string, List<AlignmentFlags>> alignmentHistory = new Dictionary<string, List<AlignmentFlags>>();

        private Character playerCharacter;

        public void Initialize()
        {
            // Initialize the service
            currentDialogues = new Dictionary<string, DialogueNode>();
            alignmentHistory = new Dictionary<string, List<AlignmentFlags>>();
        }

        public void SetPlayerCharacter(Character player)
        {
            playerCharacter = player;
        }

        /// <summary>
        /// Modify relationship between characters
        /// </summary>
        public void ModifyRelationship(Character character, string targetCharacterId, int change)
        {
            if (character == null)
            {
                Debug.LogError("RelationshipService: Character is null");
                return;
            }

            int previousValue = character.GetRelationship(targetCharacterId);
            character.ModifyRelationship(targetCharacterId, change);
            int newValue = character.GetRelationship(targetCharacterId);

            // Check for special event thresholds
            CheckRelationshipThresholds(character, targetCharacterId, previousValue, newValue);
        }

        /// <summary>
        /// Check if relationship crossed any special event thresholds
        /// </summary>
        private void CheckRelationshipThresholds(Character character, string targetCharacterId, int previousValue, int newValue)
        {
            // High relationship threshold (80+)
            if (previousValue < Constants.HIGH_RELATIONSHIP_THRESHOLD && newValue >= Constants.HIGH_RELATIONSHIP_THRESHOLD)
            {
                EventSystem.Instance.Publish(new SpecialRelationshipEvent
                {
                    CharacterId = targetCharacterId,
                    EventType = "HighRelationship",
                    Value = newValue
                });
            }

            // Low relationship threshold (-80 or lower)
            if (previousValue > Constants.LOW_RELATIONSHIP_THRESHOLD && newValue <= Constants.LOW_RELATIONSHIP_THRESHOLD)
            {
                EventSystem.Instance.Publish(new SpecialRelationshipEvent
                {
                    CharacterId = targetCharacterId,
                    EventType = "LowRelationship",
                    Value = newValue
                });
            }

            // Max relationship (100)
            if (previousValue < Constants.MAX_RELATIONSHIP && newValue >= Constants.MAX_RELATIONSHIP)
            {
                EventSystem.Instance.Publish(new SpecialRelationshipEvent
                {
                    CharacterId = targetCharacterId,
                    EventType = "MaxRelationship",
                    Value = newValue
                });
            }

            // Min relationship (-100)
            if (previousValue > Constants.MIN_RELATIONSHIP && newValue <= Constants.MIN_RELATIONSHIP)
            {
                EventSystem.Instance.Publish(new SpecialRelationshipEvent
                {
                    CharacterId = targetCharacterId,
                    EventType = "MinRelationship",
                    Value = newValue
                });
            }
        }

        /// <summary>
        /// Start a dialogue with an NPC
        /// </summary>
        public void StartDialogue(string npcId, DialogueNode startNode)
        {
            if (startNode == null)
            {
                Debug.LogError($"RelationshipService: Cannot start dialogue - start node is null");
                return;
            }

            currentDialogues[npcId] = startNode;
            EventSystem.Instance.Publish(new DialogueStartedEvent
            {
                NpcId = npcId,
                NodeId = startNode.id
            });
        }

        /// <summary>
        /// Get the current dialogue node for an NPC
        /// </summary>
        public DialogueNode GetCurrentDialogueNode(string npcId)
        {
            return currentDialogues.ContainsKey(npcId) ? currentDialogues[npcId] : null;
        }

        /// <summary>
        /// Process a dialogue choice made by the player
        /// </summary>
        public void ProcessDialogueChoice(Character player, string npcId, DialogueChoice choice)
        {
            if (player == null || choice == null)
            {
                Debug.LogError("RelationshipService: Player or choice is null");
                return;
            }

            // Apply relationship change
            if (choice.relationshipChange != 0)
            {
                ModifyRelationship(player, npcId, choice.relationshipChange);
            }

            // Apply alignment change
            if (choice.alignmentChange != AlignmentFlags.Neutral)
            {
                ApplyAlignmentChange(player, choice.alignmentChange);
            }

            // Publish dialogue choice event
            EventSystem.Instance.Publish(new DialogueChoiceMadeEvent
            {
                NpcId = npcId,
                ChoiceText = choice.text,
                RelationshipChange = choice.relationshipChange
            });

            // Move to next dialogue node if specified
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                // Note: In a full implementation, this would load the next node from a dialogue database
                // For now, we just track that we should move to the next node
                EventSystem.Instance.Publish(new DialogueProgressEvent
                {
                    NpcId = npcId,
                    NextNodeId = choice.nextNodeId
                });
            }
        }

        /// <summary>
        /// Apply alignment change to character
        /// </summary>
        private void ApplyAlignmentChange(Character character, AlignmentFlags alignmentChange)
        {
            // Combine existing alignment with new flags
            character.SetAlignment(character.alignment | alignmentChange);

            // Track alignment history
            if (!alignmentHistory.ContainsKey(character.id))
            {
                alignmentHistory[character.id] = new List<AlignmentFlags>();
            }
            alignmentHistory[character.id].Add(alignmentChange);

            EventSystem.Instance.Publish(new AlignmentChangedEvent
            {
                CharacterId = character.id,
                NewAlignment = character.alignment
            });
        }

        /// <summary>
        /// Get alignment history for a character
        /// </summary>
        public List<AlignmentFlags> GetAlignmentHistory(Character character)
        {
            if (!alignmentHistory.ContainsKey(character.id))
            {
                return new List<AlignmentFlags>();
            }
            return new List<AlignmentFlags>(alignmentHistory[character.id]);
        }

        /// <summary>
        /// Check if a dialogue is unlocked based on relationship and alignment requirements
        /// </summary>
        public bool IsDialogueUnlocked(Character player, string npcId, DialogueNode dialogue)
        {
            if (player == null || dialogue == null)
            {
                return false;
            }

            // Check relationship requirement
            if (dialogue.requiredRelationship > 0)
            {
                int currentRelationship = player.GetRelationship(npcId);
                if (currentRelationship < dialogue.requiredRelationship)
                {
                    return false;
                }
            }

            // Check alignment requirement
            if (dialogue.requiredAlignment != AlignmentFlags.Neutral)
            {
                if ((player.alignment & dialogue.requiredAlignment) != dialogue.requiredAlignment)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all NPCs with relationships above a threshold
        /// </summary>
        public List<string> GetNPCsAboveThreshold(Character player, int threshold)
        {
            var result = new List<string>();
            if (player == null) return result;

            foreach (var rel in player.relationships)
            {
                if (rel.Value >= threshold)
                {
                    result.Add(rel.Key);
                }
            }
            return result;
        }

        /// <summary>
        /// Get all NPCs with relationships below a threshold
        /// </summary>
        public List<string> GetNPCsBelowThreshold(Character player, int threshold)
        {
            var result = new List<string>();
            if (player == null) return result;

            foreach (var rel in player.relationships)
            {
                if (rel.Value <= threshold)
                {
                    result.Add(rel.Key);
                }
            }
            return result;
        }

        /// <summary>
        /// Reset dialogue state for an NPC
        /// </summary>
        public void ResetDialogue(string npcId)
        {
            if (currentDialogues.ContainsKey(npcId))
            {
                currentDialogues.Remove(npcId);
            }
        }

        /// <summary>
        /// Get relationship status description
        /// </summary>
        public string GetRelationshipStatus(int relationshipValue)
        {
            if (relationshipValue >= 80) return "Close Friends";
            if (relationshipValue >= 50) return "Good Friends";
            if (relationshipValue >= 20) return "Friendly";
            if (relationshipValue >= -20) return "Neutral";
            if (relationshipValue >= -50) return "Unfriendly";
            if (relationshipValue >= -80) return "Hostile";
            return "Enemies";
        }
    }

    /// <summary>
    /// Dialogue node structure
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        public string id;
        public string text;
        public DialogueChoice[] choices;
        public int requiredRelationship = 0;
        public AlignmentFlags requiredAlignment = AlignmentFlags.Neutral;
    }

    /// <summary>
    /// Dialogue choice structure
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public int relationshipChange = 0;
        public AlignmentFlags alignmentChange = AlignmentFlags.Neutral;
        public string nextNodeId;
    }

    // Events
    public struct SpecialRelationshipEvent
    {
        public string CharacterId;
        public string EventType;
        public int Value;
    }

    public struct DialogueStartedEvent
    {
        public string NpcId;
        public string NodeId;
    }

    public struct DialogueChoiceMadeEvent
    {
        public string NpcId;
        public string ChoiceText;
        public int RelationshipChange;
    }

    public struct DialogueProgressEvent
    {
        public string NpcId;
        public string NextNodeId;
    }

    public struct AlignmentChangedEvent
    {
        public string CharacterId;
        public AlignmentFlags NewAlignment;
    }
}
