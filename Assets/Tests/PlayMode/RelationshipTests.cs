using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using GuildReceptionist;

namespace GuildReceptionist.Tests
{
    [TestFixture]
    public class RelationshipTests
    {
        private GameObject gameManagerObj;
        private GameManager gameManager;

        [SetUp]
        public void Setup()
        {
            gameManagerObj = new GameObject("GameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();
            EventSystem.Instance.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            if (gameManagerObj != null)
            {
                Object.DestroyImmediate(gameManagerObj);
            }
            EventSystem.Instance.Clear();
        }

        [UnityTest]
        public IEnumerator Character_RelationshipProgression_WorksThroughService()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            // Act - Multiple interactions
            relationshipService.ModifyRelationship(player, npc.id, 10);
            yield return null;
            relationshipService.ModifyRelationship(player, npc.id, 15);
            yield return null;
            relationshipService.ModifyRelationship(player, npc.id, 20);
            yield return null;

            // Assert
            Assert.AreEqual(45, player.GetRelationship(npc.id));

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Dialogue_SingleChoice_AffectsRelationship()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            var dialogue = new DialogueNode
            {
                text = "Test dialogue",
                choices = new[]
                {
                    new DialogueChoice { text = "Positive choice", relationshipChange = 10 },
                    new DialogueChoice { text = "Negative choice", relationshipChange = -10 }
                }
            };

            // Act - Choose positive option
            relationshipService.ProcessDialogueChoice(player, npc.id, dialogue.choices[0]);
            yield return null;

            // Assert
            Assert.AreEqual(10, player.GetRelationship(npc.id));

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Dialogue_MultipleChoices_AccumulateRelationship()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            // Act - Make multiple dialogue choices
            var choice1 = new DialogueChoice { text = "First positive", relationshipChange = 10 };
            var choice2 = new DialogueChoice { text = "Second positive", relationshipChange = 15 };
            var choice3 = new DialogueChoice { text = "Negative", relationshipChange = -5 };

            relationshipService.ProcessDialogueChoice(player, npc.id, choice1);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, npc.id, choice2);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, npc.id, choice3);
            yield return null;

            // Assert
            Assert.AreEqual(20, player.GetRelationship(npc.id)); // 10 + 15 - 5

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Alignment_OrderChoice_IncreasesOrderAlignment()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            player.SetAlignment(AlignmentFlags.Neutral);

            var choice = new DialogueChoice
            {
                text = "Lawful choice",
                alignmentChange = AlignmentFlags.Order
            };

            // Act
            relationshipService.ProcessDialogueChoice(player, "npc1", choice);
            yield return null;

            // Assert
            Assert.IsTrue((player.alignment & AlignmentFlags.Order) == AlignmentFlags.Order);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Alignment_ChaosChoice_IncreasesChaosAlignment()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            player.SetAlignment(AlignmentFlags.Neutral);

            var choice = new DialogueChoice
            {
                text = "Chaotic choice",
                alignmentChange = AlignmentFlags.Chaos
            };

            // Act
            relationshipService.ProcessDialogueChoice(player, "npc1", choice);
            yield return null;

            // Assert
            Assert.IsTrue((player.alignment & AlignmentFlags.Chaos) == AlignmentFlags.Chaos);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator SpecialEvent_HighRelationship_Triggers()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            bool eventTriggered = false;
            EventSystem.Instance.Subscribe<SpecialRelationshipEvent>(e =>
            {
                if (e.CharacterId == npc.id && e.EventType == "HighRelationship")
                    eventTriggered = true;
            });

            // Act - Raise relationship to high threshold
            relationshipService.ModifyRelationship(player, npc.id, 80);
            yield return null;

            // Assert
            Assert.IsTrue(eventTriggered);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator SpecialEvent_LowRelationship_Triggers()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            bool eventTriggered = false;
            EventSystem.Instance.Subscribe<SpecialRelationshipEvent>(e =>
            {
                if (e.CharacterId == npc.id && e.EventType == "LowRelationship")
                    eventTriggered = true;
            });

            // Act - Lower relationship to negative threshold
            relationshipService.ModifyRelationship(player, npc.id, -80);
            yield return null;

            // Assert
            Assert.IsTrue(eventTriggered);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Dialogue_EventPublished_WhenChoiceMade()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            bool eventReceived = false;
            EventSystem.Instance.Subscribe<DialogueChoiceMadeEvent>(e =>
            {
                eventReceived = true;
            });

            var choice = new DialogueChoice { text = "Test choice", relationshipChange = 5 };

            // Act
            relationshipService.ProcessDialogueChoice(player, npc.id, choice);
            yield return null;

            // Assert
            Assert.IsTrue(eventReceived);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator RelationshipThreshold_UnlocksDialogue()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            var dialogue = new DialogueNode
            {
                text = "Secret dialogue",
                requiredRelationship = 50
            };

            // Act & Assert - Before threshold
            Assert.IsFalse(relationshipService.IsDialogueUnlocked(player, npc.id, dialogue));

            // Act - Reach threshold
            relationshipService.ModifyRelationship(player, npc.id, 50);
            yield return null;

            // Assert - After threshold
            Assert.IsTrue(relationshipService.IsDialogueUnlocked(player, npc.id, dialogue));

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator MultipleNPCs_IndependentRelationships()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc1 = new Character("NPC1", CharacterType.NPC);
            var npc2 = new Character("NPC2", CharacterType.NPC);
            var npc3 = new Character("NPC3", CharacterType.NPC);

            // Act - Modify relationships independently
            relationshipService.ModifyRelationship(player, npc1.id, 30);
            relationshipService.ModifyRelationship(player, npc2.id, -20);
            relationshipService.ModifyRelationship(player, npc3.id, 50);
            yield return null;

            // Assert
            Assert.AreEqual(30, player.GetRelationship(npc1.id));
            Assert.AreEqual(-20, player.GetRelationship(npc2.id));
            Assert.AreEqual(50, player.GetRelationship(npc3.id));

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator AlignmentHistory_TrackedCorrectly()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);

            // Act - Make multiple alignment choices
            var orderChoice = new DialogueChoice { alignmentChange = AlignmentFlags.Order };
            var chaosChoice = new DialogueChoice { alignmentChange = AlignmentFlags.Chaos };

            relationshipService.ProcessDialogueChoice(player, "npc1", orderChoice);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, "npc2", orderChoice);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, "npc3", chaosChoice);
            yield return null;

            // Assert
            var history = relationshipService.GetAlignmentHistory(player);
            Assert.AreEqual(3, history.Count);
            Assert.AreEqual(2, history.FindAll(a => a == AlignmentFlags.Order).Count);
            Assert.AreEqual(1, history.FindAll(a => a == AlignmentFlags.Chaos).Count);

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator Dialogue_ConditionalOnAlignment_ShowsCorrectly()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            player.SetAlignment(AlignmentFlags.Order);

            var orderDialogue = new DialogueNode
            {
                text = "Order dialogue",
                requiredAlignment = AlignmentFlags.Order
            };

            var chaosDialogue = new DialogueNode
            {
                text = "Chaos dialogue",
                requiredAlignment = AlignmentFlags.Chaos
            };

            // Act & Assert
            Assert.IsTrue(relationshipService.IsDialogueUnlocked(player, "npc1", orderDialogue));
            Assert.IsFalse(relationshipService.IsDialogueUnlocked(player, "npc1", chaosDialogue));

            yield return null;

            Object.DestroyImmediate(relationshipService.gameObject);
        }

        [UnityTest]
        public IEnumerator CompleteDialogueSequence_ProgressesThroughNodes()
        {
            // Arrange
            var relationshipService = new GameObject("RelationshipService").AddComponent<RelationshipService>();
            relationshipService.Initialize();

            var player = new Character("Player", CharacterType.Player);
            var npc = new Character("TestNPC", CharacterType.NPC);

            var node1 = new DialogueNode { id = "node1", text = "First dialogue" };
            var node2 = new DialogueNode { id = "node2", text = "Second dialogue" };
            var node3 = new DialogueNode { id = "node3", text = "Final dialogue" };

            var choice1 = new DialogueChoice { text = "Continue", relationshipChange = 5, nextNodeId = "node2" };
            var choice2 = new DialogueChoice { text = "Continue", relationshipChange = 10, nextNodeId = "node3" };

            node1.choices = new[] { choice1 };
            node2.choices = new[] { choice2 };

            // Act - Progress through dialogue
            relationshipService.StartDialogue(npc.id, node1);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, npc.id, choice1);
            yield return null;
            relationshipService.ProcessDialogueChoice(player, npc.id, choice2);
            yield return null;

            // Assert
            Assert.AreEqual(15, player.GetRelationship(npc.id)); // 5 + 10
            Assert.AreEqual("node3", relationshipService.GetCurrentDialogueNode(npc.id).id);

            Object.DestroyImmediate(relationshipService.gameObject);
        }
    }
}
