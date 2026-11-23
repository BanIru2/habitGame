using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StreakRewardPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Close);
    }

    // Daily Streak 보상 팝업
    public void ShowDailyReward(int streakCount)
    {
        Show($"{streakCount}일 연속 달성!", "특성 탐색권 1장");
    }

    // Weekly Streak 보상 팝업
    public void ShowWeeklyReward(int streakCount)
    {
        Show($"{streakCount}주 연속 달성!", "특성 탐색권 1장");
    }

    private void Show(string streakMessage, string rewardMessage)
    {
        if (streakText != null)
            streakText.text = streakMessage;

        if (rewardText != null)
            rewardText.text = rewardMessage;

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Close);
    }

    // Unity Inspector에서 팝업 테스트용
    [ContextMenu("TEST Daily 10 Day Reward")]
    private void TestDailyReward()
    {
        ShowDailyReward(10);
    }

    [ContextMenu("TEST Weekly 2 Week Reward")]
    private void TestWeeklyReward()
    {
        ShowWeeklyReward(2);
    }
}