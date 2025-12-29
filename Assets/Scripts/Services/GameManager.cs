using UnityEngine;

namespace GuildReceptionist
{
    /// <summary>
    /// Global game state manager - singleton pattern
    /// Manages game flow, state transitions, and provides access to core services
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Game state
        private GameState _currentGameState;
        private bool _isGameRunning = false;
        private float _currentDayTime = 0f;

        // Core services (will be initialized as needed)
        public GameState CurrentGameState => _currentGameState;
        public bool IsGameRunning => _isGameRunning;
        public int CurrentDay => _currentGameState?.currentDay ?? 1;
        public int CurrentQuarter => _currentGameState?.currentQuarter ?? 1;
        public int PlayerGold => _currentGameState?.playerGold ?? 0;
        public int PlayerReputation => _currentGameState?.playerReputation ?? 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("GameManager initialized");
            
            // Initialize event system
            EventSystem.Instance.Clear();

            // Subscribe to game events
            EventSystem.Instance.Subscribe<GameOverEvent>(OnGameOver);
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(OnDebtPayment);
        }

        /// <summary>
        /// Start a new game with default state
        /// </summary>
        public void StartNewGame()
        {
            _currentGameState = SaveLoadManager.Instance.LoadGame();
            if (SaveLoadManager.Instance.SaveFileExists())
            {
                // Reset to default if we're starting a new game
                SaveLoadManager.Instance.DeleteSave();
                _currentGameState = SaveLoadManager.Instance.LoadGame();
            }

            _isGameRunning = true;
            _currentDayTime = 0f;

            Debug.Log("New game started");
            EventSystem.Instance.Publish(new GameStartedEvent { });
        }

        /// <summary>
        /// Load existing game from save file
        /// </summary>
        public void LoadGame()
        {
            if (!SaveLoadManager.Instance.SaveFileExists())
            {
                Debug.LogWarning("No save file found. Starting new game instead.");
                StartNewGame();
                return;
            }

            _currentGameState = SaveLoadManager.Instance.LoadGame();
            _isGameRunning = true;
            _currentDayTime = 0f;

            Debug.Log("Game loaded");
            EventSystem.Instance.Publish(new GameLoadedEvent { });
        }

        /// <summary>
        /// Save current game state
        /// </summary>
        public void SaveGame()
        {
            if (_currentGameState != null)
            {
                bool success = SaveLoadManager.Instance.SaveGame(_currentGameState);
                if (success)
                {
                    EventSystem.Instance.Publish(new GameSavedEvent { });
                }
            }
        }

        /// <summary>
        /// Advance to next day
        /// </summary>
        public void AdvanceDay()
        {
            if (!_isGameRunning || _currentGameState == null) return;

            _currentGameState.currentDay++;
            _currentDayTime = 0f;

            // Check if quarter ended (every 90 days)
            if (_currentGameState.currentDay % Constants.DAYS_PER_QUARTER == 0)
            {
                AdvanceQuarter();
            }

            EventSystem.Instance.Publish(new DayAdvancedEvent { Day = _currentGameState.currentDay });
            SaveGame(); // Auto-save on day advance
        }

        /// <summary>
        /// Advance to next quarter and trigger debt payment
        /// </summary>
        private void AdvanceQuarter()
        {
            _currentGameState.currentQuarter++;
            EventSystem.Instance.Publish(new QuarterAdvancedEvent { Quarter = _currentGameState.currentQuarter });
            
            // Trigger debt payment check
            // This will be handled by DebtService in a later phase
        }

        /// <summary>
        /// Modify player gold
        /// </summary>
        public void ModifyGold(int amount)
        {
            if (_currentGameState == null) return;

            _currentGameState.playerGold += amount;
            EventSystem.Instance.Publish(new GoldChangedEvent { Amount = amount, NewTotal = _currentGameState.playerGold });
        }

        /// <summary>
        /// Modify player reputation
        /// </summary>
        public void ModifyReputation(int amount)
        {
            if (_currentGameState == null) return;

            _currentGameState.playerReputation += amount;
            _currentGameState.playerReputation = Mathf.Clamp(_currentGameState.playerReputation, 0, 100);
            EventSystem.Instance.Publish(new ReputationChangedEvent { Amount = amount, NewTotal = _currentGameState.playerReputation });
        }

        private void Update()
        {
            if (!_isGameRunning || _currentGameState == null) return;

            // Track time for quest simulation
            _currentDayTime += Time.deltaTime;

            // Optional: Auto-advance day after certain time threshold
            // This can be enabled for testing or set to manual advancement
        }

        private void OnGameOver(GameOverEvent evt)
        {
            _isGameRunning = false;
            Debug.Log($"Game Over: {evt.Reason}");
        }

        private void OnDebtPayment(DebtPaymentEvent evt)
        {
            if (_currentGameState != null)
            {
                _currentGameState.debtBalance = evt.RemainingBalance;
            }
        }

        private void OnDestroy()
        {
            EventSystem.Instance.Unsubscribe<GameOverEvent>(OnGameOver);
            EventSystem.Instance.Unsubscribe<DebtPaymentEvent>(OnDebtPayment);
        }
    }

    // Additional game events
    public struct GameStartedEvent { }
    public struct GameLoadedEvent { }
    public struct GameSavedEvent { }
    public struct DayAdvancedEvent { public int Day; }
    public struct QuarterAdvancedEvent { public int Quarter; }
    public struct GoldChangedEvent { public int Amount; public int NewTotal; }
    public struct ReputationChangedEvent { public int Amount; public int NewTotal; }
}
