using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// Controls the interactive gameplay - party selection, quest assignment, day progression
    /// </summary>
    public class GamePlayController : MonoBehaviour
    {
        private List<PartyData> parties = new List<PartyData>();
        private List<QuestData> quests = new List<QuestData>();
        private PartyData selectedParty = null;

        private class PartyData
        {
            public GameObject gameObject;
            public Image image;
            public Button button;
            public string id;
            public int str, dex, intel;
            public bool isOnQuest;
        }

        private class QuestData
        {
            public GameObject gameObject;
            public Image image;
            public Button button;
            public string id;
            public int reward;
            public int difficulty;
            public PartyData assignedParty;
            public bool hasStarted;
            public int daysRemaining;
        }

        private void Start()
        {
            Debug.Log("🎮 Initializing GamePlay system...");

            // Initialize game state - always start fresh for this demo
            GameManager gm = GameManager.Instance;
            gm.StartNewGame();
            Debug.Log($"🎮 Game started - Day: {gm.CurrentDay}, Gold: {gm.PlayerGold}, Rep: {gm.PlayerReputation}");

            // Setup parties
            SetupParties();

            // Setup quests
            SetupQuests();

            // Setup buttons
            SetupButtons();

            // Initial status update
            UpdateStatus();

            Debug.Log($"✅ Setup complete: {parties.Count} parties, {quests.Count} quests");
        }

        private void Update()
        {
            // Update status every frame
            UpdateStatus();
        }

        private void SetupParties()
        {
            GameObject partyList = GameObject.Find("PartyList");
            if (partyList == null)
            {
                Debug.LogError("PartyList not found!");
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                GameObject partyObj = GameObject.Find($"Party_{i}");
                if (partyObj == null) continue;

                // Add Button component if not present
                Button btn = partyObj.GetComponent<Button>();
                if (btn == null)
                {
                    btn = partyObj.AddComponent<Button>();
                }

                Image img = partyObj.GetComponent<Image>();

                PartyData party = new PartyData
                {
                    gameObject = partyObj,
                    image = img,
                    button = btn,
                    id = $"Party_{i}",
                    str = Random.Range(8, 16),
                    dex = Random.Range(8, 16),
                    intel = Random.Range(8, 16),
                    isOnQuest = false
                };

                parties.Add(party);

                // Setup button click
                int index = i;
                btn.onClick.AddListener(() => OnPartyClicked(index));

                // Set initial color (available - green)
                UpdatePartyColor(party);

                Debug.Log($"✅ Party {i}: STR={party.str}, DEX={party.dex}, INT={party.intel}");
            }
        }

        private void SetupQuests()
        {
            GameObject questList = GameObject.Find("QuestList");
            if (questList == null)
            {
                Debug.LogError("QuestList not found!");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                GameObject questObj = GameObject.Find($"Quest_{i}");
                if (questObj == null) continue;

                // Add Button component if not present
                Button btn = questObj.GetComponent<Button>();
                if (btn == null)
                {
                    btn = questObj.AddComponent<Button>();
                }

                Image img = questObj.GetComponent<Image>();

                QuestData quest = new QuestData
                {
                    gameObject = questObj,
                    image = img,
                    button = btn,
                    id = $"Quest_{i}",
                    reward = Random.Range(500, 2001),
                    difficulty = Random.Range(15, 36),
                    assignedParty = null,
                    hasStarted = false,
                    daysRemaining = 2
                };

                quests.Add(quest);

                // Setup button click
                int index = i;
                btn.onClick.AddListener(() => OnQuestClicked(index));

                // Set initial color (unassigned - blue)
                UpdateQuestColor(quest);

                Debug.Log($"✅ Quest {i}: Reward={quest.reward}g, Difficulty={quest.difficulty}");
            }
        }

        private void SetupButtons()
        {
            // Generate Quests button
            GameObject generateBtn = GameObject.Find("ActionButton");
            if (generateBtn != null)
            {
                Button btn = generateBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GenerateQuests);
                }
            }

            // Advance Day button
            GameObject advanceBtn = GameObject.Find("AdvanceDayButton");
            if (advanceBtn != null)
            {
                Button btn = advanceBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(AdvanceDay);
                }
            }
        }

        private void OnPartyClicked(int index)
        {
            if (index < 0 || index >= parties.Count) return;

            PartyData party = parties[index];

            // Can't select party on quest
            if (party.isOnQuest)
            {
                Debug.Log($"❌ {party.id} is currently on a quest!");
                return;
            }

            // Toggle selection
            if (selectedParty == party)
            {
                selectedParty = null;
                Debug.Log($"⭕ Deselected {party.id}");
            }
            else
            {
                selectedParty = party;
                Debug.Log($"✅ Selected {party.id} (STR:{party.str}, DEX:{party.dex}, INT:{party.intel})");
            }

            // Update all party colors
            foreach (var p in parties)
            {
                UpdatePartyColor(p);
            }
        }

        private void OnQuestClicked(int index)
        {
            if (index < 0 || index >= quests.Count) return;

            QuestData quest = quests[index];

            // Can't cancel after quest starts
            if (quest.hasStarted)
            {
                Debug.Log($"❌ {quest.id} has already started! Cannot cancel.");
                return;
            }

            // Cancel assignment
            if (quest.assignedParty != null)
            {
                quest.assignedParty.isOnQuest = false;
                UpdatePartyColor(quest.assignedParty);
                Debug.Log($"❌ Cancelled {quest.id} assignment from {quest.assignedParty.id}");
                quest.assignedParty = null;
            }
            // Assign party
            else if (selectedParty != null)
            {
                quest.assignedParty = selectedParty;
                selectedParty.isOnQuest = true;

                int totalStats = selectedParty.str + selectedParty.dex + selectedParty.intel;
                int successRate = Mathf.Clamp(10 + (totalStats - quest.difficulty) * 2, 10, 95);

                Debug.Log($"🎯 Assigned {selectedParty.id} to {quest.id} (Success rate: {successRate}%)");

                UpdatePartyColor(selectedParty);
                selectedParty = null;

                // Update all party colors
                foreach (var p in parties)
                {
                    UpdatePartyColor(p);
                }
            }
            else
            {
                Debug.Log($"⚠️ Select a party first!");
                return;
            }

            UpdateQuestColor(quest);
        }

        private void GenerateQuests()
        {
            Debug.Log("🎲 Generating new quests...");

            foreach (var quest in quests)
            {
                quest.reward = Random.Range(500, 2001);
                quest.difficulty = Random.Range(15, 36);
                quest.assignedParty = null;
                quest.hasStarted = false;
                quest.daysRemaining = 2;

                UpdateQuestColor(quest);

                Debug.Log($"  Quest {quest.id}: Reward={quest.reward}g, Difficulty={quest.difficulty}");
            }

            // Reset all parties
            foreach (var party in parties)
            {
                party.isOnQuest = false;
                UpdatePartyColor(party);
            }

            selectedParty = null;
        }

        private void AdvanceDay()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            gm.AdvanceDay();

            Debug.Log($"📅 Advanced to Day {gm.CurrentDay}");

            // Process quests
            List<QuestData> completedQuests = new List<QuestData>();

            foreach (var quest in quests)
            {
                if (quest.assignedParty == null) continue;

                // Start quest on first day
                if (!quest.hasStarted)
                {
                    quest.hasStarted = true;
                    Debug.Log($"🚀 {quest.id} started by {quest.assignedParty.id}");
                    continue;
                }

                // Process quest on second day
                quest.daysRemaining--;
                if (quest.daysRemaining <= 0)
                {
                    completedQuests.Add(quest);
                }
            }

            // Complete quests
            foreach (var quest in completedQuests)
            {
                int totalStats = quest.assignedParty.str + quest.assignedParty.dex + quest.assignedParty.intel;
                int successRate = Mathf.Clamp(10 + (totalStats - quest.difficulty) * 2, 10, 95);
                int roll = Random.Range(0, 100);
                bool success = roll < successRate;

                if (success)
                {
                    gm.ModifyGold(quest.reward);
                    gm.ModifyReputation(10);
                    Debug.Log($"✅ {quest.id} SUCCESS! {quest.assignedParty.id} earned {quest.reward}g, +10 rep");
                }
                else
                {
                    gm.ModifyReputation(-5);
                    Debug.Log($"❌ {quest.id} FAILED! {quest.assignedParty.id} lost -5 rep");
                }

                // Free party
                quest.assignedParty.isOnQuest = false;
                UpdatePartyColor(quest.assignedParty);

                // Reset quest
                quest.assignedParty = null;
                quest.hasStarted = false;
                quest.daysRemaining = 2;
                UpdateQuestColor(quest);
            }

            UpdateStatus();
        }

        private void UpdatePartyColor(PartyData party)
        {
            if (party.image == null) return;

            if (party.isOnQuest)
            {
                // Red - on quest
                party.image.color = new Color(0.5f, 0.3f, 0.3f, 0.9f);
            }
            else if (party == selectedParty)
            {
                // Bright green - selected
                party.image.color = new Color(0.5f, 0.8f, 0.5f, 0.9f);
            }
            else
            {
                // Dark green - available
                party.image.color = new Color(0.2f, 0.5f, 0.2f, 0.9f);
            }
        }

        private void UpdateQuestColor(QuestData quest)
        {
            if (quest.image == null) return;

            if (quest.assignedParty != null)
            {
                // Purple - assigned
                quest.image.color = new Color(0.4f, 0.4f, 0.6f, 0.9f);
            }
            else
            {
                // Blue - unassigned
                quest.image.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
            }
        }

        private void UpdateStatus()
        {
            GameObject statusText = GameObject.Find("StatusText");
            if (statusText != null)
            {
                TMPro.TextMeshProUGUI text = statusText.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    GameManager gm = GameManager.Instance;
                    text.text = $"Day: {gm.CurrentDay} | Gold: {gm.PlayerGold} | Reputation: {gm.PlayerReputation}";
                }
            }
        }
    }
}
