using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuildReceptionist
{
    /// <summary>
    /// Manages scene transitions and navigation throughout the game.
    /// Provides loading screens and handles scene lifecycle.
    /// </summary>
    public class SceneNavigationManager : MonoBehaviour
    {
        private static SceneNavigationManager instance;
        public static SceneNavigationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneNavigationManager");
                    instance = go.AddComponent<SceneNavigationManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private bool isLoading = false;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Load the main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync("MainMenu"));
            }
        }

        /// <summary>
        /// Load the game scene
        /// </summary>
        public void LoadGameScene()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync("Game"));
            }
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Asynchronously load a scene with optional loading screen
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;

            // Start loading the scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Wait until the scene is fully loaded
            while (!asyncLoad.isDone)
            {
                // Update loading progress (0.0 to 1.0)
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                // Could update a loading bar here
                yield return null;
            }

            isLoading = false;
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (!isLoading)
            {
                Scene currentScene = SceneManager.GetActiveScene();
                StartCoroutine(LoadSceneAsync(currentScene.name));
            }
        }
    }
}
