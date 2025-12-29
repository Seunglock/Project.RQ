using UnityEngine;
using UnityEngine.UIElements;

namespace GuildReceptionist.UI
{
    /// <summary>
    /// Main menu UI controller
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Button newGameButton;
        private Button continueButton;
        private Button quitButton;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found on MainMenuUI");
                return;
            }
        }

        void OnEnable()
        {
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            // Get buttons
            newGameButton = root.Q<Button>("NewGameButton");
            continueButton = root.Q<Button>("ContinueButton");
            quitButton = root.Q<Button>("QuitButton");

            // Register button callbacks
            if (newGameButton != null)
            {
                newGameButton.clicked += OnNewGameClicked;
            }

            if (continueButton != null)
            {
                continueButton.clicked += OnContinueClicked;
                // Check if save exists
                bool saveExists = SaveLoadManager.Instance.SaveExists();
                continueButton.SetEnabled(saveExists);
            }

            if (quitButton != null)
            {
                quitButton.clicked += OnQuitClicked;
            }
        }

        void OnDisable()
        {
            if (newGameButton != null)
            {
                newGameButton.clicked -= OnNewGameClicked;
            }

            if (continueButton != null)
            {
                continueButton.clicked -= OnContinueClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= OnQuitClicked;
            }
        }

        private void OnNewGameClicked()
        {
            Debug.Log("Starting new game...");
            // Reset game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
            // Load game scene
            SceneNavigationManager.Instance.LoadGameScene();
        }

        private void OnContinueClicked()
        {
            Debug.Log("Loading saved game...");
            // Load game state
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadGame();
            }
            // Load game scene
            SceneNavigationManager.Instance.LoadGameScene();
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quitting game...");
            SceneNavigationManager.Instance.QuitGame();
        }
    }
}
