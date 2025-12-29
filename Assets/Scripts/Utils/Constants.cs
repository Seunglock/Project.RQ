namespace GuildReceptionist
{
    /// <summary>
    /// Game configuration constants
    /// </summary>
    public static class Constants
    {
        // Game limits
        public const int MAX_QUESTS = 10;
        public const int MAX_PARTIES = 5;
        public const int MAX_MATERIALS = 100;
        public const int MAX_NPCS = 10;

        // Performance targets
        public const int TARGET_FPS = 60;
        public const int MAX_MEMORY_MB = 100;
        public const int MAX_LOAD_TIME_MS = 200;

        // Memory limits by platform
        public const float SWITCH_MEMORY_LIMIT_GB = 3.5f;

        // Stat ranges
        public const int MIN_STAT_VALUE = 1;
        public const int MAX_STAT_VALUE = 20;

        // Quest difficulty
        public const int MIN_DIFFICULTY = 1;
        public const int MAX_DIFFICULTY = 5;
        public const int STAT_POINTS_PER_DIFFICULTY = 10;
        public const int MIN_QUEST_DURATION = 3;
        public const int MAX_QUEST_DURATION = 10;
        public const int GOLD_PER_DIFFICULTY = 100;

        // Party loyalty
        public const int MIN_LOYALTY = 0;
        public const int MAX_LOYALTY = 100;
        public const int LOYALTY_UNAVAILABLE_THRESHOLD = 20;

        // Relationships
        public const int MIN_RELATIONSHIP = -100;
        public const int MAX_RELATIONSHIP = 100;
        public const int HIGH_RELATIONSHIP_THRESHOLD = 80;
        public const int LOW_RELATIONSHIP_THRESHOLD = -80;

        // Debt
        public const int DAYS_PER_QUARTER = 90;
        public const float DEFAULT_INTEREST_RATE = 0.05f; // 5% annual rate

        // Starting values for new game
        public const int STARTING_GOLD = 1000;
        public const int STARTING_REPUTATION = 50;
        public const int STARTING_DEBT = 10000;
        public const int QUARTERLY_PAYMENT = 2500;

        // Save/Load
        public const string SAVE_FILE_NAME = "save.json";
        public const string SETTINGS_KEY_PREFIX = "GuildReceptionist_";

        // Time
        public const float QUEST_SIMULATION_TICK = 1.0f; // seconds per in-game day

        // UI
        public const float UI_TRANSITION_DURATION = 0.3f;
        public const int QUEST_POOL_SIZE = 20;
        public const int UI_ELEMENT_POOL_SIZE = 10;
    }
}
