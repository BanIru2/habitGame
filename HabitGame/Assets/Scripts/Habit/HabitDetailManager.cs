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

    // '목표량' 제목과 값이 함께 들어 있는 한 줄
    [SerializeField] private GameObject targetRow;

    [Header("Optional RPG summary badges")]
    [SerializeField] private TextMeshProUGUI categoryBadgeText;
    [SerializeField] private TextMeshProUGUI periodBadgeText;

    private HabitGoalResponse currentHabit;

    public void OpenDetail(HabitGoalResponse data)
    {
        if (data == null)
        {
            Debug.LogWarning("HabitDetail 데이터가 없습니다.");
            return;
        }

        currentHabit = data;

        if (habitNameText != null)
            habitNameText.text = GetDisplayValue(data.GoalName);

        ShowCategory(data.Category);

        string recordType = Normalize(data.RecordType);
        string period = Normalize(data.Period);

        if (recordTypeText != null)
            recordTypeText.text = DisplayRecordType(data.RecordType);

        string periodLabel = DisplayPeriod(data.Period);

        if (periodText != null)
            periodText.text = periodLabel;

        if (periodBadgeText != null)
            periodBadgeText.text = periodLabel;

        // 일일 완료형은 횟수를 입력하지 않으므로 목표량 행을 숨김
        bool isDailyCheck = recordType == "check" && period == "daily";

        //if (targetRow != null)
        //    targetRow.SetActive(!isDailyCheck);

        if (targetText != null)
        {
            // 행이 아직 연결되지 않은 경우에도 '1check'가 보이지 않게 함
            if (isDailyCheck)
                targetText.text = "1회";
            else
                targetText.text = $"{data.TargetAmount}{data.Unit?.Trim()}";
        }

        if (detailPanel != null)
            detailPanel.SetActive(true);
        else
            Debug.LogWarning("HabitDetailManager의 DetailPanel이 연결되지 않았습니다.");

        if (lifePanel != null)
            lifePanel.SetActive(false);

        if (topTab != null)
            topTab.SetActive(false);
    }

    // 저장된 특성으로 배지, 능력치 설명, 예상 보상을 함께 표시
    private void ShowCategory(string value)
    {
        string name;
        string stats = "";
        string element = "";

        switch (Normalize(value))
        {
            case "physical":
                name = "신체활동";
                stats = "ATK · HP 상승";
                element = "FIRE";
                break;

            case "rhythm":
                name = "생체리듬";
                stats = "DEF · HP 상승";
                element = "WATER";
                break;

            case "eco":
                name = "환경관리";
                stats = "DEF · SPD 상승";
                element = "GRASS";
                break;

            case "growth":
                name = "자기계발";
                stats = "ATK · CRIT 상승";
                element = "AURORA";
                break;

            case "mental":
                name = "정신";
                break;

            case "study":
                name = "학습";
                break;

            default:
                name = GetDisplayValue(value);
                break;
        }

        if (categoryBadgeText != null)
            categoryBadgeText.text = name;

        if (categoryText != null)
            categoryText.text = stats == "" ? name : $"{name} ({stats})";

        if (rewardText != null)
        {
            rewardText.text = element == ""
                ? "목표를 완료한 뒤 보상을 확인할 수 있어요."
                : $"{element} EXP +5~15";
        }
    }

    private string DisplayRecordType(string value)
    {
        switch (Normalize(value))
        {
            case "check":
                return "완료형 (유/무)";

            case "value":
            case "number":
                return "수치형 (목표량)";

            case "photo":
                return "사진 인증";

            default:
                return GetDisplayValue(value);
        }
    }

    private string DisplayPeriod(string value)
    {
        switch (Normalize(value))
        {
            case "daily":
                return "일일";

            case "weekly":
                return "주간";

            case "long-term":
            case "long_term":
            case "longterm":
                return "장기";

            default:
                return GetDisplayValue(value);
        }
    }

    public void CloseDetail()
    {
        currentHabit = null;

        if (detailPanel != null)
            detailPanel.SetActive(false);

        if (lifePanel != null)
            lifePanel.SetActive(true);

        if (topTab != null)
            topTab.SetActive(true);
    }

    public HabitGoalResponse GetCurrentHabit()
    {
        return currentHabit;
    }

    private static string Normalize(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static string GetDisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}