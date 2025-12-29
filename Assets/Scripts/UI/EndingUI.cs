using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using GuildReceptionist;

/// <summary>
/// UI for displaying game endings and achievements
/// Shows ending type, description, statistics, and unlocked achievements
/// </summary>
public class EndingUI : MonoBehaviour
{
    private EndingTracker endingTracker;
    private VisualElement root;
    private VisualElement endingPanel;
    private Label endingTitleLabel;
    private Label endingDescriptionLabel;
    private VisualElement statisticsContainer;
    private VisualElement achievementsContainer;
    private Button returnToMenuButton;
    private Button playAgainButton;

    private bool isShowing;

    private void Awake()
    {
        // Get UI Document root
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;
            SetupUI();
        }
    }

    /// <summary>
    /// Setup UI elements
    /// </summary>
    private void SetupUI()
    {
        // Create ending panel
        endingPanel = new VisualElement();
        endingPanel.name = "ending-panel";
        endingPanel.style.display = DisplayStyle.None;
        endingPanel.style.position = Position.Absolute;
        endingPanel.style.width = new Length(100, LengthUnit.Percent);
        endingPanel.style.height = new Length(100, LengthUnit.Percent);
        endingPanel.style.backgroundColor = new Color(0, 0, 0, 0.9f);
        endingPanel.style.alignItems = Align.Center;
        endingPanel.style.justifyContent = Justify.Center;

        // Create content container
        var contentContainer = new VisualElement();
        contentContainer.name = "content-container";
        contentContainer.style.width = new Length(80, LengthUnit.Percent);
        contentContainer.style.maxWidth = 800;
        contentContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        contentContainer.style.paddingTop = 20;
        contentContainer.style.paddingBottom = 20;
        contentContainer.style.paddingLeft = 20;
        contentContainer.style.paddingRight = 20;
        contentContainer.style.borderTopLeftRadius = 10;
        contentContainer.style.borderTopRightRadius = 10;
        contentContainer.style.borderBottomLeftRadius = 10;
        contentContainer.style.borderBottomRightRadius = 10;

        // Ending title
        endingTitleLabel = new Label("Ending Title");
        endingTitleLabel.name = "ending-title";
        endingTitleLabel.style.fontSize = 32;
        endingTitleLabel.style.color = Color.white;
        endingTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        endingTitleLabel.style.marginBottom = 20;
        contentContainer.Add(endingTitleLabel);

        // Ending description
        endingDescriptionLabel = new Label("Ending description...");
        endingDescriptionLabel.name = "ending-description";
        endingDescriptionLabel.style.fontSize = 16;
        endingDescriptionLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        endingDescriptionLabel.style.whiteSpace = WhiteSpace.Normal;
        endingDescriptionLabel.style.marginBottom = 30;
        contentContainer.Add(endingDescriptionLabel);

        // Statistics section
        var statisticsLabel = new Label("Game Statistics");
        statisticsLabel.style.fontSize = 20;
        statisticsLabel.style.color = Color.yellow;
        statisticsLabel.style.marginBottom = 10;
        contentContainer.Add(statisticsLabel);

        statisticsContainer = new VisualElement();
        statisticsContainer.name = "statistics-container";
        statisticsContainer.style.marginBottom = 30;
        contentContainer.Add(statisticsContainer);

        // Achievements section
        var achievementsLabel = new Label("Achievements Unlocked");
        achievementsLabel.style.fontSize = 20;
        achievementsLabel.style.color = Color.yellow;
        achievementsLabel.style.marginBottom = 10;
        contentContainer.Add(achievementsLabel);

        achievementsContainer = new VisualElement();
        achievementsContainer.name = "achievements-container";
        achievementsContainer.style.marginBottom = 30;
        contentContainer.Add(achievementsContainer);

        // Buttons container
        var buttonsContainer = new VisualElement();
        buttonsContainer.style.flexDirection = FlexDirection.Row;
        buttonsContainer.style.justifyContent = Justify.Center;

        // Return to menu button
        returnToMenuButton = new Button(() => OnReturnToMenu());
        returnToMenuButton.text = "Return to Menu";
        returnToMenuButton.style.width = 200;
        returnToMenuButton.style.height = 40;
        returnToMenuButton.style.marginRight = 10;
        buttonsContainer.Add(returnToMenuButton);

        // Play again button
        playAgainButton = new Button(() => OnPlayAgain());
        playAgainButton.text = "Play Again";
        playAgainButton.style.width = 200;
        playAgainButton.style.height = 40;
        buttonsContainer.Add(playAgainButton);

        contentContainer.Add(buttonsContainer);
        endingPanel.Add(contentContainer);
        root.Add(endingPanel);

        isShowing = false;
    }

    /// <summary>
    /// Initialize with ending tracker reference
    /// </summary>
    public void Initialize(EndingTracker tracker)
    {
        endingTracker = tracker;
    }

    /// <summary>
    /// Show ending screen with results
    /// </summary>
    public void ShowEnding(EndingType endingType)
    {
        if (endingTracker == null)
        {
            Debug.LogError("EndingTracker not initialized!");
            return;
        }

        // Set ending title
        endingTitleLabel.text = GetEndingTitle(endingType);

        // Set ending description
        endingDescriptionLabel.text = endingTracker.GetEndingDescription(endingType);

        // Display statistics
        DisplayStatistics();

        // Display achievements
        DisplayAchievements();

        // Show panel
        endingPanel.style.display = DisplayStyle.Flex;
        isShowing = true;

        // Log ending reached
        Debug.Log($"Ending reached: {endingType}");
    }

    /// <summary>
    /// Get ending title based on type
    /// </summary>
    private string GetEndingTitle(EndingType endingType)
    {
        switch (endingType)
        {
            case EndingType.TrueEnding:
                return "★ TRUE ENDING ★";
            case EndingType.DebtVictory:
                return "DEBT PAID - VICTORY!";
            case EndingType.DebtFailure:
                return "GAME OVER - DEBT UNPAID";
            case EndingType.OrderEnding:
                return "THE ORDER ENDING";
            case EndingType.ChaosEnding:
                return "THE CHAOS ENDING";
            case EndingType.RelationshipEnding:
                return "THE FRIENDSHIP ENDING";
            case EndingType.WealthEnding:
                return "THE WEALTH ENDING";
            case EndingType.ReputationEnding:
                return "THE LEGEND ENDING";
            case EndingType.BalancedEnding:
                return "THE BALANCED ENDING";
            default:
                return "GAME COMPLETE";
        }
    }

    /// <summary>
    /// Display game statistics
    /// </summary>
    private void DisplayStatistics()
    {
        statisticsContainer.Clear();

        var statistics = endingTracker.GetStatistics();

        foreach (var stat in statistics)
        {
            var statRow = new VisualElement();
            statRow.style.flexDirection = FlexDirection.Row;
            statRow.style.justifyContent = Justify.SpaceBetween;
            statRow.style.marginBottom = 5;

            var statName = new Label($"{stat.Key}:");
            statName.style.color = Color.white;
            statName.style.fontSize = 14;

            var statValue = new Label(stat.Value.ToString());
            statValue.style.color = Color.cyan;
            statValue.style.fontSize = 14;
            statValue.style.unityFontStyleAndWeight = FontStyle.Bold;

            statRow.Add(statName);
            statRow.Add(statValue);
            statisticsContainer.Add(statRow);
        }
    }

    /// <summary>
    /// Display unlocked achievements
    /// </summary>
    private void DisplayAchievements()
    {
        achievementsContainer.Clear();

        var unlockedAchievements = endingTracker.GetUnlockedAchievements();

        if (unlockedAchievements.Count == 0)
        {
            var noAchievementsLabel = new Label("No achievements unlocked");
            noAchievementsLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            noAchievementsLabel.style.fontSize = 14;
            noAchievementsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            achievementsContainer.Add(noAchievementsLabel);
            return;
        }

        foreach (var achievement in unlockedAchievements)
        {
            var achievementRow = new VisualElement();
            achievementRow.style.marginBottom = 10;
            achievementRow.style.paddingLeft = 10;
            achievementRow.style.paddingRight = 10;
            achievementRow.style.paddingTop = 5;
            achievementRow.style.paddingBottom = 5;
            achievementRow.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            achievementRow.style.borderTopLeftRadius = 5;
            achievementRow.style.borderTopRightRadius = 5;
            achievementRow.style.borderBottomLeftRadius = 5;
            achievementRow.style.borderBottomRightRadius = 5;

            var achievementName = new Label($"★ {achievement.AchievementName}");
            achievementName.style.color = Color.yellow;
            achievementName.style.fontSize = 16;
            achievementName.style.unityFontStyleAndWeight = FontStyle.Bold;
            achievementRow.Add(achievementName);

            var achievementDesc = new Label(achievement.Description);
            achievementDesc.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            achievementDesc.style.fontSize = 12;
            achievementDesc.style.whiteSpace = WhiteSpace.Normal;
            achievementRow.Add(achievementDesc);

            achievementsContainer.Add(achievementRow);
        }
    }

    /// <summary>
    /// Hide ending screen
    /// </summary>
    public void Hide()
    {
        if (endingPanel != null)
        {
            endingPanel.style.display = DisplayStyle.None;
            isShowing = false;
        }
    }

    /// <summary>
    /// Handle return to menu button
    /// </summary>
    private void OnReturnToMenu()
    {
        Hide();
        
        // Load main menu scene
        if (SceneNavigationManager.Instance != null)
        {
            SceneNavigationManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Handle play again button
    /// </summary>
    private void OnPlayAgain()
    {
        Hide();
        
        // Reset game and start new game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NewGame();
        }
        
        // Load game scene
        if (SceneNavigationManager.Instance != null)
        {
            SceneNavigationManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }

    /// <summary>
    /// Check if ending screen is currently showing
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }

    private void OnDestroy()
    {
        // Cleanup
        if (returnToMenuButton != null)
        {
            returnToMenuButton.clicked -= OnReturnToMenu;
        }
        if (playAgainButton != null)
        {
            playAgainButton.clicked -= OnPlayAgain;
        }
    }
}
