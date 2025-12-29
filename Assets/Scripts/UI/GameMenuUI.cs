using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist.UI
{
    /// <summary>
    /// In-game menu UI controller (pause menu)
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement menuContainer;
        private Button resumeButton;
        private Button saveButton;
        private Button mainMenuButton;
        private Button quitButton;
        private bool isPaused = false;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found on GameMenuUI");
                return;
            }
        }

        void OnEnable()
        {
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            // Get menu container
            menuContainer = root.Q<VisualElement>("MenuContainer");
            if (menuContainer != null)
            {
                menuContainer.style.display = DisplayStyle.None; // Hidden by default
            }

            // Get buttons
            resumeButton = root.Q<Button>("ResumeButton");
            saveButton = root.Q<Button>("SaveButton");
            mainMenuButton = root.Q<Button>("MainMenuButton");
            quitButton = root.Q<Button>("QuitButton");

            // Register button callbacks
            if (resumeButton != null)
            {
                resumeButton.clicked += OnResumeClicked;
            }

            if (saveButton != null)
            {
                saveButton.clicked += OnSaveClicked;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.clicked += OnMainMenuClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked += OnQuitClicked;
            }
        }

        void OnDisable()
        {
            if (resumeButton != null)
            {
                resumeButton.clicked -= OnResumeClicked;
            }

            if (saveButton != null)
            {
                saveButton.clicked -= OnSaveClicked;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.clicked -= OnMainMenuClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= OnQuitClicked;
            }
        }

        void Update()
        {
            // Toggle pause menu with Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }
        }

        public void TogglePauseMenu()
        {
            isPaused = !isPaused;

            if (menuContainer != null)
            {
                menuContainer.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Pause/unpause game
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnResumeClicked()
        {
            TogglePauseMenu();
        }

        private void OnSaveClicked()
        {
            Debug.Log("Saving game...");
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
            }
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("Returning to main menu...");
            // Unpause game
            Time.timeScale = 1f;
            // Save game before leaving
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
            }
            // Load main menu
            SceneNavigationManager.Instance.LoadMainMenu();
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quitting game...");
            // Unpause game
            Time.timeScale = 1f;
            // Save game before quitting
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
            }
            // Quit
            SceneNavigationManager.Instance.QuitGame();
        }
    }
}
