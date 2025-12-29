using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace GuildReceptionist
{
    /// <summary>
    /// Simplified gameplay controller for basic interaction
    /// </summary>
    public class SimpleGameController : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI statusText;
        public Button generateQuestsButton;
        public Button advanceDayButton;
        public Transform questListContainer;
        public Transform partyListContainer;
        
        private int gold = 10000;
        private int reputation = 0;
        private int day = 1;
        
        private List<string> questNames = new List<string>
        {
            "Defeat Goblins",
            "Collect Herbs",
            "Escort Merchant",
            "Clear Dungeon",
            "Hunt Wolf Pack"
        };
        
        private List<string> partyNames = new List<string>
        {
            "Brave Warriors",
            "Shadow Rogues",
            "Magic Circle",
            "Steel Legion"
        };
        
        private void Start()
        {
            Debug.Log("SimpleGameController Started!");
            
            // Setup buttons
            if (generateQuestsButton != null)
            {
                generateQuestsButton.onClick.AddListener(GenerateQuests);
                Debug.Log("Generate Quests Button connected");
            }
            
            if (advanceDayButton != null)
            {
                advanceDayButton.onClick.AddListener(AdvanceDay);
                Debug.Log("Advance Day Button connected");
            }
            
            // Initial setup
            UpdateStatusText();
            GenerateInitialParties();
            GenerateQuests();
        }
        
        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                statusText.text = $"Day {day} | Gold: {gold} | Reputation: {reputation}";
            }
        }
        
        private void GenerateQuests()
        {
            Debug.Log("Generating Quests...");
            
            if (questListContainer == null)
            {
                Debug.LogWarning("Quest List Container not assigned!");
                return;
            }
            
            // Clear existing quests
            foreach (Transform child in questListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Generate 3-5 random quests
            int questCount = Random.Range(3, 6);
            for (int i = 0; i < questCount; i++)
            {
                CreateQuestItem(i);
            }
            
            Debug.Log($"Generated {questCount} quests");
        }
        
        private void CreateQuestItem(int index)
        {
            // Create quest item GameObject
            GameObject questItem = new GameObject($"Quest_{index}");
            questItem.transform.SetParent(questListContainer);
            
            // Add RectTransform
            var rectTransform = questItem.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(380, 80);
            
            // Add background Image
            var image = questItem.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
            
            // Add Button
            var button = questItem.AddComponent<Button>();
            button.targetGraphic = image;
            
            // Create Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(questItem.transform);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            string questName = questNames[Random.Range(0, questNames.Count)];
            int reward = Random.Range(500, 2000);
            int duration = Random.Range(1, 5);
            
            text.text = $"{questName}\nReward: {reward}g | Duration: {duration} days\nClick to start!";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            // Add button click listener
            int capturedReward = reward;
            int capturedDuration = duration;
            button.onClick.AddListener(() => OnQuestClicked(questName, capturedReward, capturedDuration, questItem));
        }
        
        private void OnQuestClicked(string questName, int reward, int duration, GameObject questItem)
        {
            Debug.Log($"Quest clicked: {questName}");
            
            // Simple quest completion (instant for now)
            gold += reward;
            reputation += 5;
            
            UpdateStatusText();
            
            // Remove the quest
            Destroy(questItem);
            
            Debug.Log($"Quest completed! +{reward} gold, +5 reputation");
        }
        
        private void AdvanceDay()
        {
            day++;
            UpdateStatusText();
            
            Debug.Log($"Advanced to Day {day}");
            
            // Chance to generate new quests
            if (Random.value > 0.5f)
            {
                GenerateQuests();
            }
        }
        
        private void GenerateInitialParties()
        {
            if (partyListContainer == null)
            {
                Debug.LogWarning("Party List Container not assigned!");
                return;
            }
            
            // Generate 3 parties
            for (int i = 0; i < 3; i++)
            {
                CreatePartyItem(i);
            }
        }
        
        private void CreatePartyItem(int index)
        {
            // Create party item GameObject
            GameObject partyItem = new GameObject($"Party_{index}");
            partyItem.transform.SetParent(partyListContainer);
            
            // Add RectTransform
            var rectTransform = partyItem.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(380, 60);
            
            // Add background Image
            var image = partyItem.AddComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.2f, 0.9f);
            
            // Create Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(partyItem.transform);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            string partyName = partyNames[index % partyNames.Count];
            int str = Random.Range(5, 15);
            int dex = Random.Range(5, 15);
            int intel = Random.Range(5, 15);
            
            text.text = $"{partyName} - Available\nSTR:{str} DEX:{dex} INT:{intel}";
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }
    }
}
