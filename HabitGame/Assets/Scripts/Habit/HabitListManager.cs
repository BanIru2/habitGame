
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitListManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField habitNameInput;

    [SerializeField]
    private Transform content;

    [SerializeField]
    private GameObject habitItemPrefab;

    [Header("Habit Period Sections")]
    [SerializeField]
    private Transform dailySection;

    [SerializeField]
    private Transform weeklySection;

    [SerializeField]
    private Transform longTermSection;

    [Header("Summary")]
    [SerializeField]
    private HabitSummaryManager summaryManager;

    [Header("Summary Display")]
    [SerializeField]
    private TextMeshProUGUI summaryTitleText;

    [SerializeField]
    private TextMeshProUGUI habitListTitleText;

    [SerializeField]
    private GameObject summaryStreakObject;

    [SerializeField]
    private TextMeshProUGUI summaryStreakText;

    [Header("Period Tab Buttons")]
    [SerializeField]
    private Button dailyTabButton;

    [SerializeField]
    private Button weeklyTabButton;

    [SerializeField]
    private Button longTermTabButton;

    [SerializeField]
    private GameObject importWeeklyButton;

    // 선택 / 미선택 색상
    private readonly Color selectedTabColor =
        new Color32(238, 252, 255, 255); // #EEFCFF

    private readonly Color unselectedTabColor =
        new Color32(244, 244, 244, 255); // #F4F4F4

    private string selectedPeriod = "DAILY";
    private int dailyStreak;
    private int weeklyStreak;

    public string SelectedPeriod => selectedPeriod;

    private void Start()
    {
        // 초기 화면은 일일 목표 탭
        ShowDaily();
    }

    // =========================================
    // Habit 하나 추가
    // =========================================
    public void AddHabit(
        HabitGoalResponse habit)
    {
        if (habit == null)
        {
            Debug.LogWarning(
                "추가할 습관 데이터가 없습니다."
            );
            return;
        }

        CreateHabitItem(habit);

        if (habitNameInput != null)
        {
            habitNameInput.text = "";
        }

        RefreshSummary();
    }

    // =========================================
    // 서버에서 받은 Habit 목록으로 UI 갱신
    // =========================================
    public void RefreshHabitList(
        List<HabitGoalResponse> habits)
    {
        if (content == null)
        {
            Debug.LogError(
                "HabitListManager의 Content가 연결되지 않았습니다."
            );
            return;
        }

        if (!ValidateSections())
        {
            return;
        }

        // 기존 Habit UI 제거
        // Section 오브젝트 자체는 삭제하지 않음
        ClearSection(dailySection);
        ClearSection(weeklySection);
        ClearSection(longTermSection);

        // Content에 직접 놓여 있는 기존 테스트 HabitItem 제거
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform existingHabit = content.GetChild(i);

            if (existingHabit.GetComponent<HabitItem>() != null)
            {
                existingHabit.SetParent(null, false);
                Destroy(existingHabit.gameObject);
            }
        }

        if (habits == null)
        {
            RefreshSummary();
            return;
        }

        // 서버에서 받은 사용자 전체 Streak 저장
        // 목표가 없는 경우 이전 사용자의 값이 남지 않도록 초기화
        dailyStreak = 0;
        weeklyStreak = 0;

        foreach (HabitGoalResponse habit in habits)
        {
            if (habit == null)
                continue;

            dailyStreak = habit.FinalizedDailyStreak ?? 0;
            weeklyStreak = habit.FinalizedWeeklyStreak ?? 0;
            break;
        }

        foreach (HabitGoalResponse habit in habits)
        {
            if (habit == null)
                continue;

            CreateHabitItem(habit);
        }

        RefreshSummary();
        UpdateSummaryDisplay();
    }

    // =========================================
    // Section 내부 Habit UI 제거
    // =========================================
    private void ClearSection(Transform section)
    {
        for (int i = section.childCount - 1; i >= 0; i--)
        {
            Transform existingHabit = section.GetChild(i);

            // 나중에 Section 제목 등이 생겨도 삭제하지 않도록
            // HabitItem 컴포넌트가 있는 오브젝트만 제거
            if (existingHabit.GetComponent<HabitItem>() == null)
                continue;

            existingHabit.SetParent(null, false);
            Destroy(existingHabit.gameObject);
        }
    }

    // =========================================
    // Habit Item 생성
    // =========================================
    private void CreateHabitItem(
        HabitGoalResponse habit)
    {
        if (habit == null)
        {
            return;
        }

        if (habitItemPrefab == null)
        {
            Debug.LogError(
                "HabitItemPrefab이 연결되지 않았습니다."
            );
            return;
        }

        if (!ValidateSections())
        {
            return;
        }

        Transform targetSection = GetSection(habit.Period);

        if (targetSection == null)
        {
            Debug.LogWarning(
                "알 수 없는 목표 주기입니다: " + habit.Period
            );
            return;
        }

        GameObject newHabit =
            Instantiate(
                habitItemPrefab,
                targetSection
            );

        // =========================================
        // Habit 이름 표시
        // =========================================
        Transform labelTransform =
            newHabit.transform.Find("Label");

        if (labelTransform != null)
        {
            TextMeshProUGUI label =
                labelTransform
                    .GetComponent<TextMeshProUGUI>();

            if (label != null)
            {
                label.text =
                    habit.GoalName;
            }
        }
        else
        {
            Debug.LogWarning(
                "HabitItem Prefab에서 Label을 찾을 수 없습니다."
            );
        }

        // =========================================
        // Toggle 기본 상태
        // =========================================
        Toggle toggle =
            newHabit.GetComponentInChildren<Toggle>();

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(
                habit.CompletedToday
            );

            toggle.interactable =
                !habit.CompletedToday;
        }

        // =========================================
        // Habit 데이터 연결
        // =========================================
        HabitItem habitItem =
            newHabit.GetComponent<HabitItem>();

        if (habitItem != null)
        {
            habitItem.SetData(habit);
        }
        else
        {
            Debug.LogError(
                "HabitItem Prefab 루트에 " +
                "HabitItem 스크립트가 없습니다."
            );
        }
    }

    // =========================================
    // 목표 주기에 따른 Section 반환
    // =========================================
    private Transform GetSection(string period)
    {
        string normalizedPeriod =
            (period ?? "").Trim().ToUpperInvariant();

        switch (normalizedPeriod)
        {
            case "DAILY":
                return dailySection;

            case "WEEKLY":
                return weeklySection;

            case "LONG_TERM":
            case "LONG-TERM":
            case "LONGTERM":
                return longTermSection;

            default:
                return null;
        }
    }

    // =========================================
    // 오늘 / 주간 / 장기 탭 전환
    // =========================================
    public void ShowDaily()
    {
        ShowPeriod("DAILY");
    }

    public void ShowWeekly()
    {
        ShowPeriod("WEEKLY");
    }

    public void ShowLongTerm()
    {
        ShowPeriod("LONG-TERM");
    }

    // =========================================
    // 선택한 탭 버튼 색상 갱신
    // =========================================
    private void UpdateTabColors()
    {
        if (dailyTabButton != null)
        {
            dailyTabButton.image.color =
                selectedPeriod == "DAILY"
                    ? selectedTabColor
                    : unselectedTabColor;
        }

        if (weeklyTabButton != null)
        {
            weeklyTabButton.image.color =
                selectedPeriod == "WEEKLY"
                    ? selectedTabColor
                    : unselectedTabColor;
        }

        if (longTermTabButton != null)
        {
            longTermTabButton.image.color =
                selectedPeriod == "LONG-TERM"
                    ? selectedTabColor
                    : unselectedTabColor;
        }
    }

    // =========================================
    // 선택한 탭의 요약 카드 표시 갱신
    // =========================================
    private void UpdateSummaryDisplay()
    {
        if (summaryTitleText != null)
        {
            switch (selectedPeriod)
            {
                case "DAILY":
                    summaryTitleText.text = "오늘의 할 일";
                    break;

                case "WEEKLY":
                    summaryTitleText.text = "이번 주 할 일";
                    break;

                case "LONG-TERM":
                    summaryTitleText.text = "장기 목표";
                    break;
            }
        }

        // 장기 목표에서는 Streak 표시하지 않음
        if (summaryStreakObject != null)
        {
            summaryStreakObject.SetActive(
                selectedPeriod != "LONG-TERM"
            );
        }

        if (summaryStreakText != null)
        {
            if (selectedPeriod == "DAILY")
            {
                summaryStreakText.text =
                    $"{dailyStreak}일 연속 달성";
            }
            else if (selectedPeriod == "WEEKLY")
            {
                summaryStreakText.text =
                    $"{weeklyStreak}주 연속 달성";
            }
        }

        if (habitListTitleText != null)
        {
            switch (selectedPeriod)
            {
                case "DAILY":
                    habitListTitleText.text = "오늘의 목표";
                    break;

                case "WEEKLY":
                    habitListTitleText.text = "이번 주 목표";
                    break;

                case "LONG-TERM":
                    habitListTitleText.text = "장기 목표";
                    break;
            }
        }
    }

    private void ShowPeriod(string period)
    {
        if (!ValidateSections())
        {
            return;
        }

        selectedPeriod = period;

        dailySection.gameObject.SetActive(
            selectedPeriod == "DAILY"
        );

        weeklySection.gameObject.SetActive(
            selectedPeriod == "WEEKLY"
        );

        longTermSection.gameObject.SetActive(
            selectedPeriod == "LONG-TERM"
        );

        // 선택한 탭의 Summary 갱신
        if (summaryManager != null)
        {
            summaryManager.SetSelectedSection(
                GetSection(selectedPeriod)
            );
        }

        if (importWeeklyButton != null)
        {
            importWeeklyButton.SetActive(
                selectedPeriod == "WEEKLY"
            );
        }
        UpdateTabColors();
        UpdateSummaryDisplay();

    }

    // =========================================
    // Section 연결 확인
    // =========================================
    private bool ValidateSections()
    {
        if (dailySection == null ||
            weeklySection == null ||
            longTermSection == null)
        {
            Debug.LogError(
                "HabitListManager의 목표 Section 연결이 누락되었습니다."
            );
            return false;
        }

        return true;
    }

    // =========================================
    // Summary 갱신
    // =========================================
    private void RefreshSummary()
    {
        if (summaryManager != null)
        {
            summaryManager.RefreshSummary();
        }
    }
}
