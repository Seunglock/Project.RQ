using NUnit.Framework;
using GuildReceptionist;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuildReceptionist.Tests
{
    [TestFixture]
    public class CharacterTests
    {
        [Test]
        public void Constructor_CreatesValidCharacter()
        {
            var character = new Character("TestNPC", CharacterType.NPC);

            Assert.IsNotNull(character.id);
            Assert.AreEqual("TestNPC", character.name);
            Assert.AreEqual(CharacterType.NPC, character.type);
            Assert.IsFalse(character.isPlayer);
            Assert.AreEqual(AlignmentFlags.Neutral, character.alignment);
            Assert.IsNotNull(character.stats);
            Assert.IsNotNull(character.relationships);
        }

        [Test]
        public void Constructor_PlayerCharacter_SetsIsPlayerTrue()
        {
            var character = new Character("Player", CharacterType.Player);

            Assert.IsTrue(character.isPlayer);
        }

        [Test]
        public void IsValid_WithValidStats_ReturnsTrue()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Charisma] = 10;
            character.stats[StatType.Empathy] = 15;
            character.stats[StatType.Courage] = 20;

            Assert.IsTrue(character.IsValid());
        }

        [Test]
        public void IsValid_WithStatTooLow_ReturnsFalse()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Charisma] = 0; // Below MIN_STAT_VALUE (1)

            // Expect error log
            LogAssert.Expect(LogType.Error, "Character TestNPC: Invalid stat value Charisma=0");

            Assert.IsFalse(character.IsValid());
        }

        [Test]
        public void IsValid_WithStatTooHigh_ReturnsFalse()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Charisma] = 21; // Above MAX_STAT_VALUE (20)

            // Expect error log
            LogAssert.Expect(LogType.Error, "Character TestNPC: Invalid stat value Charisma=21");

            Assert.IsFalse(character.IsValid());
        }

        [Test]
        public void IsValid_WithValidRelationships_ReturnsTrue()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 50;
            character.relationships["npc2"] = -30;
            character.relationships["npc3"] = 100;

            Assert.IsTrue(character.IsValid());
        }

        [Test]
        public void IsValid_WithRelationshipTooLow_ReturnsFalse()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = -101; // Below MIN_RELATIONSHIP (-100)

            // Expect error log
            LogAssert.Expect(LogType.Error, "Character TestNPC: Invalid relationship value with npc1=-101");

            Assert.IsFalse(character.IsValid());
        }

        [Test]
        public void IsValid_WithRelationshipTooHigh_ReturnsFalse()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 101; // Above MAX_RELATIONSHIP (100)

            // Expect error log
            LogAssert.Expect(LogType.Error, "Character TestNPC: Invalid relationship value with npc1=101");

            Assert.IsFalse(character.IsValid());
        }

        [Test]
        public void GetRelationship_ExistingCharacter_ReturnsValue()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 50;

            Assert.AreEqual(50, character.GetRelationship("npc1"));
        }

        [Test]
        public void GetRelationship_NonExistingCharacter_ReturnsZero()
        {
            var character = new Character("TestNPC", CharacterType.NPC);

            Assert.AreEqual(0, character.GetRelationship("nonexistent"));
        }

        [Test]
        public void ModifyRelationship_NewCharacter_CreatesRelationship()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.ModifyRelationship("npc1", 20);

            Assert.AreEqual(20, character.GetRelationship("npc1"));
        }

        [Test]
        public void ModifyRelationship_ExistingCharacter_AddsToExisting()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 30;
            character.ModifyRelationship("npc1", 20);

            Assert.AreEqual(50, character.GetRelationship("npc1"));
        }

        [Test]
        public void ModifyRelationship_NegativeChange_DecreasesRelationship()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 50;
            character.ModifyRelationship("npc1", -30);

            Assert.AreEqual(20, character.GetRelationship("npc1"));
        }

        [Test]
        public void ModifyRelationship_ExceedsMax_ClampsToMax()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = 90;
            character.ModifyRelationship("npc1", 50); // Would be 140

            Assert.AreEqual(100, character.GetRelationship("npc1")); // Clamped to MAX_RELATIONSHIP
        }

        [Test]
        public void ModifyRelationship_ExceedsMin_ClampsToMin()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.relationships["npc1"] = -90;
            character.ModifyRelationship("npc1", -50); // Would be -140

            Assert.AreEqual(-100, character.GetRelationship("npc1")); // Clamped to MIN_RELATIONSHIP
        }

        [Test]
        public void SetAlignment_UpdatesAlignment()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.SetAlignment(AlignmentFlags.Order);

            Assert.AreEqual(AlignmentFlags.Order, character.alignment);
        }

        [Test]
        public void SetAlignment_ChaosAlignment_Works()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.SetAlignment(AlignmentFlags.Chaos);

            Assert.AreEqual(AlignmentFlags.Chaos, character.alignment);
        }

        [Test]
        public void SetAlignment_CombinedAlignment_Works()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.SetAlignment(AlignmentFlags.Order | AlignmentFlags.Chaos);

            Assert.AreEqual(AlignmentFlags.Order | AlignmentFlags.Chaos, character.alignment);
        }

        [Test]
        public void Stats_CharismaStat_CanBeSetAndRetrieved()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Charisma] = 15;

            Assert.AreEqual(15, character.stats[StatType.Charisma]);
        }

        [Test]
        public void Stats_EmpathyStat_CanBeSetAndRetrieved()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Empathy] = 12;

            Assert.AreEqual(12, character.stats[StatType.Empathy]);
        }

        [Test]
        public void Stats_CourageStat_CanBeSetAndRetrieved()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.stats[StatType.Courage] = 18;

            Assert.AreEqual(18, character.stats[StatType.Courage]);
        }

        [Test]
        public void Relationships_MultipleCharacters_TrackedIndependently()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.ModifyRelationship("npc1", 30);
            character.ModifyRelationship("npc2", -20);
            character.ModifyRelationship("npc3", 50);

            Assert.AreEqual(30, character.GetRelationship("npc1"));
            Assert.AreEqual(-20, character.GetRelationship("npc2"));
            Assert.AreEqual(50, character.GetRelationship("npc3"));
        }

        [Test]
        public void Relationships_SequentialModifications_AccumulateCorrectly()
        {
            var character = new Character("TestNPC", CharacterType.NPC);
            character.ModifyRelationship("npc1", 10);
            character.ModifyRelationship("npc1", 15);
            character.ModifyRelationship("npc1", -5);

            Assert.AreEqual(20, character.GetRelationship("npc1"));
        }
    }
}
