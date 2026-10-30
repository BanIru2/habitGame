using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendingDashboardPresenter : MonoBehaviour
{
    [Header("Special Goal Empty State")]
    [SerializeField] private Transform goalContent;
    [SerializeField] private GameObject goalTemplate;
    [SerializeField] private GameObject goalEmptyState;
    [SerializeField] private ScrollRect goalScroll;
    [SerializeField] private LayoutElement goalCardLayout;

    [Header("Reward Labels")]
    [SerializeField] private TMP_Text baseRewardText;
    [SerializeField] private TMP_Text bonusRewardText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text streakBonusText;
    [SerializeField] private TMP_Text baseRewardDisplayLabel;
    [SerializeField] private TMP_Text bonusRewardDisplayLabel;
    [SerializeField] private TMP_Text streakDisplayLabel;
    [SerializeField] private TMP_Text streakBonusDisplayLabel;
    [SerializeField] private TMP_Text baseRewardDisplayText;
    [SerializeField] private TMP_Text bonusRewardDisplayText;
    [SerializeField] private TMP_Text streakDisplayText;
    [SerializeField] private TMP_Text streakBonusDisplayText;

    private const float EmptyGoalCardHeight = 310f;
    private const float PopulatedGoalCardHeight = 520f;

    private void OnEnable()
    {
        RefreshGoalState();
        LocalizeRewardText();
        SyncRewardDisplays();
    }

    private void LateUpdate()
    {
        RefreshGoalState();
        LocalizeRewardText();
        SyncRewardDisplays();
    }

    private void RefreshGoalState()
    {
        if (goalContent == null)
            return;

        bool hasGoal = false;
        for (int index = 0; index < goalContent.childCount; index++)
        {
            GameObject child = goalContent.GetChild(index).gameObject;
            if (child != goalTemplate && child != goalEmptyState && child.activeSelf)
            {
                hasGoal = true;
                LocalizeGoalDescription(child.transform);
            }
        }

        if (goalEmptyState != null && goalEmptyState.activeSelf == hasGoal)
            goalEmptyState.SetActive(!hasGoal);

        if (goalScroll != null && goalScroll.gameObject.activeSelf != hasGoal)
            goalScroll.gameObject.SetActive(hasGoal);

        if (goalCardLayout != null)
        {
            float height = hasGoal ? PopulatedGoalCardHeight : EmptyGoalCardHeight;
            if (!Mathf.Approximately(goalCardLayout.preferredHeight, height))
            {
                goalCardLayout.minHeight = height;
                goalCardLayout.preferredHeight = height;
            }
        }
    }

    private static void LocalizeGoalDescription(Transform goalItem)
    {
        TMP_Text description = goalItem.Find("GoalDescription")?.GetComponent<TMP_Text>();
        if (description == null)
            return;

        const string prefix = "Spend less than ";
        const string suffix = " won";
        string value = description.text;
        if (!value.StartsWith(prefix) || !value.EndsWith(suffix))
            return;

        string amount = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
        description.text = $"{amount}원 이하 사용";
    }

    private void LocalizeRewardText()
    {
        ReplaceText(baseRewardText,
            ("Base Reward", "기본 보상"),
            ("% Used", "% 사용"));
        ReplaceText(bonusRewardText,
            ("Bonus Goal", "추가 보상"));
        ReplaceText(streakText,
            ("Weekly Streak", "연속 달성"),
            (" Weeks", "주"));
        ReplaceText(streakBonusText,
            ("Streak Bonus", "연속 보너스"));
    }

    private void SyncRewardDisplays()
    {
        CopyText(baseRewardText, baseRewardDisplayLabel, baseRewardDisplayText);
        CopyText(bonusRewardText, bonusRewardDisplayLabel, bonusRewardDisplayText);
        CopyText(streakText, streakDisplayLabel, streakDisplayText);
        CopyText(streakBonusText, streakBonusDisplayLabel, streakBonusDisplayText);
    }

    private static void CopyText(TMP_Text source, TMP_Text label, TMP_Text value)
    {
        if (source == null || label == null || value == null)
            return;

        string sourceText = source.text ?? string.Empty;
        int separatorIndex = sourceText.IndexOf('\n');
        string labelText = separatorIndex >= 0
            ? sourceText.Substring(0, separatorIndex)
            : sourceText;
        string valueText = separatorIndex >= 0
            ? sourceText.Substring(separatorIndex + 1)
            : string.Empty;

        if (label.text != labelText)
            label.text = labelText;
        if (value.text != valueText)
            value.text = valueText;
    }

    private static void ReplaceText(TMP_Text target,
        params (string English, string Korean)[] replacements)
    {
        if (target == null || string.IsNullOrEmpty(target.text))
            return;

        string localized = target.text;
        foreach ((string english, string korean) in replacements)
            localized = localized.Replace(english, korean);

        int separatorIndex = localized.IndexOf(" : ", System.StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            localized = localized.Substring(0, separatorIndex) + "\n" +
                        localized.Substring(separatorIndex + 3);
        }

        if (localized != target.text)
            target.text = localized;
    }
}
