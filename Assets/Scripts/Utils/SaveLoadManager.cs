using System;
using System.IO;
using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Manages game state persistence using JSON serialization
    /// </summary>
    public class SaveLoadManager
    {
        private static SaveLoadManager _instance;
        public static SaveLoadManager Instance => _instance ??= new SaveLoadManager();

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, Constants.SAVE_FILE_NAME);

        /// <summary>
        /// Save current game state to JSON file
        /// </summary>
        public bool SaveGame()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found, cannot save game");
                return false;
            }

            GameState gameState = GameManager.Instance.GetGameState();
            return SaveGame(gameState);
        }

        /// <summary>
        /// Save game state to JSON file
        /// </summary>
        public bool SaveGame(GameState gameState)
        {
            try
            {
                string json = JsonUtility.ToJson(gameState, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"Game saved successfully to {SaveFilePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load game state from JSON file and apply to GameManager
        /// </summary>
        public bool LoadGame()
        {
            GameState gameState = LoadGameState();
            if (gameState == null)
            {
                Debug.LogWarning("Failed to load game state");
                return false;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found, cannot load game");
                return false;
            }

            GameManager.Instance.SetGameState(gameState);
            Debug.Log("Game loaded and applied to GameManager");
            return true;
        }

        /// <summary>
        /// Load game state from JSON file
        /// </summary>
        public GameState LoadGameState()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    Debug.LogWarning("No save file found");
                    return null;
                }

                string json = File.ReadAllText(SaveFilePath);
                GameState gameState = JsonUtility.FromJson<GameState>(json);
                Debug.Log($"Game loaded successfully from {SaveFilePath}");
                return gameState;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a save file exists
        /// </summary>
        public bool SaveExists()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Check if a save file exists (alternative name for backward compatibility)
        /// </summary>
        public bool SaveFileExists()
        {
            return SaveExists();
        }

        /// <summary>
        /// Delete the save file
        /// </summary>
        public bool DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("Save file deleted successfully");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save a setting to PlayerPrefs
        /// </summary>
        public void SaveSetting(string key, string value)
        {
            PlayerPrefs.SetString(Constants.SETTINGS_KEY_PREFIX + key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load a setting from PlayerPrefs
        /// </summary>
        public string LoadSetting(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(Constants.SETTINGS_KEY_PREFIX + key, defaultValue);
        }

        /// <summary>
        /// Create a default game state for new games
        /// </summary>
        public GameState CreateDefaultGameState()
        {
            return new GameState
            {
                currentDay = 1,
                currentQuarter = 1,
                playerGold = Constants.STARTING_GOLD,
                playerReputation = Constants.STARTING_REPUTATION,
                debtBalance = Constants.STARTING_DEBT,
                quarterlyPayment = Constants.QUARTERLY_PAYMENT
            };
        }
    }

    /// <summary>
    /// Serializable game state container
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int currentDay;
        public int currentQuarter;
        public int playerGold;
        public int playerReputation;
        public int debtBalance;
        public int quarterlyPayment;
        
        // These will be populated with actual game data
        // For now, keeping them simple for the foundation
    }
}
