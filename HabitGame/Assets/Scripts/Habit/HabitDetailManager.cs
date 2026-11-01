using TMPro;
using UnityEngine;

public class HabitDetailManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject topTab;

    [Header("Detail UI")]
    [SerializeField] private TextMeshProUGUI habitNameText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI recordTypeText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI periodText;
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("Optional RPG summary badges")]
    [SerializeField] private TextMeshProUGUI categoryBadgeText;
    [SerializeField] private TextMeshProUGUI periodBadgeText;

    // 현재 상세 화면에 표시 중인 Habit
    private HabitGoalResponse currentHabit;

    // =========================================
    // Habit 상세 화면 열기
    // =========================================
    public void OpenDetail(HabitGoalResponse data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "HabitDetail 데이터가 없습니다."
            );
            return;
        }

        currentHabit = data;

        // =========================================
        // Habit 이름
        // =========================================
        if (habitNameText != null)
        {
            habitNameText.text =
                string.IsNullOrWhiteSpace(data.GoalName)
                    ? "-"
                    : data.GoalName;
        }

        // =========================================
        // Category
        // =========================================
        string categoryLabel = DisplayCategory(data.Category);
        if (categoryText != null) categoryText.text = categoryLabel;
        if (categoryBadgeText != null) categoryBadgeText.text = categoryLabel;

        // =========================================
        // Record Type
        // =========================================
        if (recordTypeText != null)
            recordTypeText.text = DisplayRecordType(data.RecordType);

        // =========================================
        // Target
        // =========================================
        if (targetText != null)
        {
            if (string.Equals(data.RecordType, "check", System.StringComparison.OrdinalIgnoreCase))
            {
                targetText.text = "완료";
            }
            else
            {
                string unit =
                    string.IsNullOrWhiteSpace(data.Unit)
                        ? ""
                        : $" {data.Unit}";

                targetText.text = $"{data.TargetAmount}{unit}";
            }
        }

        // =========================================
        // Period
        // =========================================
        string periodLabel = DisplayPeriod(data.Period);
        if (periodText != null) periodText.text = periodLabel;
        if (periodBadgeText != null) periodBadgeText.text = periodLabel;

        // =========================================
        // Reward
        // =========================================
        if (rewardText != null)
        {
            /*
             * HabitGoalResponse에는 현재
             * 보상 관련 필드가 존재하지 않음.
             *
             * 실제 보상은 HabitRecord 생성 후
             * HabitRewardClaimResponse를 통해
             * 받아오는 구조이므로 여기서는
             * 임의의 보상값을 표시하지 않음.
             */
            rewardText.text = "목표를 완료하고 보상을 수령하세요.";
        }

        // =========================================
        // Panel 전환
        // =========================================
        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "HabitDetailManager의 DetailPanel이 연결되지 않았습니다."
            );
        }

        if (lifePanel != null)
        {
            lifePanel.SetActive(false);
        }

        if (topTab != null)
        {
            topTab.SetActive(false);
        }
    }

    // =========================================
    // 상세 화면 닫기
    // =========================================
    public void CloseDetail()
    {
        currentHabit = null;

        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        if (lifePanel != null)
        {
            lifePanel.SetActive(true);
        }

        if (topTab != null)
        {
            topTab.SetActive(true);
        }
    }

    // =========================================
    // 현재 상세 Habit 반환
    // 추후 보상 기능에서 사용 가능
    // =========================================
    public HabitGoalResponse GetCurrentHabit()
    {
        return currentHabit;
    }

    // =========================================
    // 빈 문자열 표시 방지
    // =========================================
    // Display-only localization. Unknown server values are shown unchanged.
    private string DisplayCategory(string value)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "physical": return "운동";
            case "mental": return "정신";
            case "study": return "학습";
            default: return GetDisplayValue(value);
        }
    }

    private string DisplayRecordType(string value)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "check": return "완료 체크";
            case "value":
            case "number": return "수치 입력";
            case "photo": return "사진 인증";
            default: return GetDisplayValue(value);
        }
    }

    private string DisplayPeriod(string value)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "daily": return "매일";
            case "weekly": return "매주";
            case "long_term":
            case "longterm": return "장기";
            default: return GetDisplayValue(value);
        }
    }

    private string GetDisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "-"
            : value;
    }
}