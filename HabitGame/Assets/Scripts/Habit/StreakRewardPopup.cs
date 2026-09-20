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

    // Daily Streak º¸»ó ÆË¾÷
    public void ShowDailyReward(int streakCount)
    {
        Show(
            $"{streakCount} Day Streak!",
            "Trait Ticket x1"
        );
    }

    // Weekly Streak º¸»ó ÆË¾÷
    public void ShowWeeklyReward(int streakCount)
    {
        Show(
            $"{streakCount} Week Streak!",
            "Trait Ticket x1"
        );
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

    // Unity Inspector¿¡¼­ ÆË¾÷ Å×½ºÆ®¿ë
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