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
        public MaterialService materialService;
        public EndingTracker endingTracker;

        public GameState CurrentGameState => _currentGameState;
        public bool IsGameRunning => _isGameRunning;
        public int CurrentDay => _currentGameState?.currentDay ?? 1;
        public int CurrentQuarter => _currentGameState?.currentQuarter ?? 1;
        public int PlayerGold => _currentGameState?.playerGold ?? 0;
        public int PlayerReputation => _currentGameState?.playerReputation ?? 0;

        // Convenience property for gold access
        public int gold => _currentGameState?.playerGold ?? 0;

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

            // Initialize platform-specific optimizations
            PlatformOptimizations.Initialize();

            // Initialize performance manager
            var perfManager = PerformanceManager.Instance;
            Debug.Log($"Performance manager initialized for {PlatformOptimizations.GetPlatformName()}");

            // Initialize event system
            EventSystem.Instance.Clear();

            // Initialize ending tracker
            endingTracker = new EndingTracker();

            // Subscribe to game events
            EventSystem.Instance.Subscribe<GameOverEvent>(OnGameOver);
            EventSystem.Instance.Subscribe<DebtPaymentEvent>(OnDebtPayment);
            EventSystem.Instance.Subscribe<GoldChangedEvent>(OnGoldChangedForTracker);
            EventSystem.Instance.Subscribe<ReputationChangedEvent>(OnReputationChangedForTracker);
        }

        /// <summary>
        /// Get current game state for saving
        /// </summary>
        public GameState GetGameState()
        {
            if (_currentGameState == null)
            {
                _currentGameState = SaveLoadManager.Instance.CreateDefaultGameState();
            }
            return _currentGameState;
        }

        /// <summary>
        /// Set game state from loaded data
        /// </summary>
        public void SetGameState(GameState gameState)
        {
            _currentGameState = gameState;
            _isGameRunning = true;
            Debug.Log("Game state applied to GameManager");
            EventSystem.Instance.Publish(new GameLoadedEvent { });
        }

        /// <summary>
        /// Start a new game with default state
        /// </summary>
        public void StartNewGame()
        {
            // Delete existing save if any
            if (SaveLoadManager.Instance.SaveFileExists())
            {
                SaveLoadManager.Instance.DeleteSave();
            }

            // Create new default game state
            _currentGameState = SaveLoadManager.Instance.CreateDefaultGameState();
            _isGameRunning = true;
            _currentDayTime = 0f;

            // Reset ending tracker for new game
            if (endingTracker != null)
            {
                endingTracker.Reset();
            }

            Debug.Log("New game started");
            EventSystem.Instance.Publish(new GameStartedEvent { });
        }

        /// <summary>
        /// Start new game (alias for StartNewGame)
        /// </summary>
        public void NewGame()
        {
            StartNewGame();
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

            GameState loadedState = SaveLoadManager.Instance.LoadGameState();
            if (loadedState != null)
            {
                SetGameState(loadedState);
                _currentDayTime = 0f;
                Debug.Log("Game loaded");
            }
            else
            {
                Debug.LogWarning("Failed to load game. Starting new game instead.");
                StartNewGame();
            }
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

            // Update random event system
            RandomEventService.Instance.Update(Time.deltaTime);

            // Check performance and apply graceful degradation if needed
            float currentFPS = PerformanceManager.Instance.GetFPS();
            float memoryUsage = PerformanceManager.Instance.GetMemoryUsageMB();
            PlatformOptimizations.ApplyGracefulDegradation(currentFPS, memoryUsage);

            // Optional: Auto-advance day after certain time threshold
            // This can be enabled for testing or set to manual advancement
        }

        private void OnGameOver(GameOverEvent evt)
        {
            _isGameRunning = false;
            Debug.Log($"Game Over: {evt.Reason}");

            // Determine and display ending
            if (endingTracker != null)
            {
                EndingType ending = endingTracker.DetermineEnding();
                Debug.Log($"Ending reached: {ending}");
            }
        }

        private void OnDebtPayment(DebtPaymentEvent evt)
        {
            if (_currentGameState != null)
            {
                _currentGameState.debtBalance = evt.RemainingBalance;

                // Track debt paid off
                if (endingTracker != null && evt.RemainingBalance <= 0)
                {
                    endingTracker.OnDebtPaidOff();
                }
            }
        }

        private void OnGoldChangedForTracker(GoldChangedEvent evt)
        {
            if (endingTracker != null)
            {
                endingTracker.OnGoldChanged(evt.NewTotal);
            }
        }

        private void OnReputationChangedForTracker(ReputationChangedEvent evt)
        {
            if (endingTracker != null)
            {
                endingTracker.OnReputationChanged(evt.NewTotal);
            }
        }

        private void OnDestroy()
        {
            EventSystem.Instance.Unsubscribe<GameOverEvent>(OnGameOver);
            EventSystem.Instance.Unsubscribe<DebtPaymentEvent>(OnDebtPayment);
            EventSystem.Instance.Unsubscribe<GoldChangedEvent>(OnGoldChangedForTracker);
            EventSystem.Instance.Unsubscribe<ReputationChangedEvent>(OnReputationChangedForTracker);
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
