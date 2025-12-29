namespace GuildReceptionist
{
    /// <summary>
    /// Enum definitions for the Guild Receptionist Tycoon game
    /// </summary>

    public enum QuestType
    {
        Exploration,
        Combat,
        Admin
    }

    public enum StatType
    {
        Exploration,
        Combat,
        Admin,
        Charisma,
        Empathy,
        Courage
    }

    public enum MaterialRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public enum CharacterType
    {
        Player,
        NPC
    }

    [System.Flags]
    public enum AlignmentFlags
    {
        Neutral = 0,
        Order = 1,
        Chaos = 2
    }

    public enum QuestState
    {
        Available,
        Assigned,
        InProgress,
        Completed,
        Failed
    }

    public enum DebtState
    {
        Active,
        Paid,
        Overdue
    }
}
