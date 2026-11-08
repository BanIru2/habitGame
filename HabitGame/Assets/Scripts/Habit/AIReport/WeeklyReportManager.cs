using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeeklyReportManager : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("Week")]
    [SerializeField] private TMP_Text weekText;

    [Header("Habit Report")]
    [SerializeField] private TMP_Text habitAchievementText;
    [SerializeField] private TMP_Text habitCompletedText;
    [SerializeField] private TMP_Text incompleteHabitText;

    [Header("Spending Report")]
    [SerializeField] private TMP_Text spendingBudgetText;
    [SerializeField] private TMP_Text spendingSpentText;
    [SerializeField] private TMP_Text spendingUsageText;

    [Header("AI Weekly Feedback")]
    [SerializeField] private TMP_Text aiSummaryText;
    [SerializeField] private TMP_Text aiGoodPointText;
    [SerializeField] private TMP_Text aiImprovementText;
    [SerializeField] private TMP_Text aiNextWeekText;

    [Header("Test Mode")]
    [SerializeField] private bool useLocalMode = true;

    private WeeklyReportService weeklyReportService;

    private void Awake()
    {
        ClearReport();
    }

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        if (ServiceRegistry.Instance != null)
            weeklyReportService = ServiceRegistry.Instance.WeeklyReport;
    }

    private void OnEnable()
    {
        ClearReport();

        if (ServiceRegistry.Instance != null)
            weeklyReportService = ServiceRegistry.Instance.WeeklyReport;

        LoadWeeklyReport();
    }

    private void ClearReport()
    {
        if (weekText != null)
            weekText.text = "주간 리포트를 불러오는 중...";

        if (habitAchievementText != null)
            habitAchievementText.text = "";

        if (habitCompletedText != null)
            habitCompletedText.text = "";

        if (incompleteHabitText != null)
            incompleteHabitText.text = "";

        if (spendingBudgetText != null)
            spendingBudgetText.text = "";

        if (spendingSpentText != null)
            spendingSpentText.text = "";

        if (spendingUsageText != null)
            spendingUsageText.text = "";

        if (aiSummaryText != null)
            aiSummaryText.text = "";

        if (aiGoodPointText != null)
            aiGoodPointText.text = "";

        if (aiImprovementText != null)
            aiImprovementText.text = "";

        if (aiNextWeekText != null)
            aiNextWeekText.text = "";
    }

    private async void LoadWeeklyReport()
    {
        // 로컬 테스트 모드
        if (useLocalMode)
        {
            LoadDummyReport();
            return;
        }

        // 서비스 연결 확인
        if (weeklyReportService == null)
        {
            Debug.LogError("WeeklyReportService를 찾을 수 없습니다.");

            if (weekText != null)
                weekText.text = "리포트를 불러오지 못했습니다.";

            return;
        }

        try
        {
            WeeklyReportResponse response =
                await weeklyReportService.GetWeeklyReportAsync();

            // 응답 데이터가 없는 경우
            if (response == null)
            {
                Debug.LogWarning("Weekly Report 응답이 없습니다.");

                if (weekText != null)
                    weekText.text = "리포트 데이터가 없습니다.";

                return;
            }

            // 정상 응답
            ApplyReport(response);

            Debug.Log("Weekly Report 서버 데이터 로드 완료");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Weekly Report 서버 데이터 로드 실패: {e.Message}"
            );

            if (weekText != null)
            {
                // 로그인 정보가 없는 경우
                if (e.Message.Contains("로그인이 필요"))
                    weekText.text = "로그인 후 리포트를 확인할 수 있습니다.";
                else
                    weekText.text = "리포트를 불러오지 못했습니다.";
            }
        }
    }


    private void LoadDummyReport()
    {
        DateTime today = DateTime.Now.Date;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;

        DateTime monday = today.AddDays(-daysSinceMonday);
        DateTime sunday = monday.AddDays(6);

        WeeklyReportResponse dummyReport = new WeeklyReportResponse
        {
            WeekStart = monday.ToString("yyyy-MM-dd"),
            WeekEnd = sunday.ToString("yyyy-MM-dd"),

            HabitAchievementRate = 75,
            CompletedHabitCount = 3,
            TotalHabitCount = 4,

            WeeklyBudget = 200000,
            WeeklySpent = 125000,
            BudgetUsageRate = 62,

            AiSummary =
                "You maintained your habits consistently this week.",

            GoodPoint =
                "You achieved most of your habit goals this week.",

            Improvement =
                "Your spending was slightly higher this week.",

            NextWeekSuggestion =
                "Keep your current habits and try to reduce unnecessary spending."
        };

        ApplyReport(dummyReport);

        Debug.Log("Weekly Report Local Mode 데이터 로드 완료");
    }

    private void ApplyReport(WeeklyReportResponse report)
    {
        if (report == null)
        {
            Debug.LogWarning("Weekly Report data is null.");
            return;
        }

        // 주간 날짜
        if (DateTime.TryParse(report.WeekStart, out DateTime weekStart) &&
            DateTime.TryParse(report.WeekEnd, out DateTime weekEnd))
        {
            weekText.text =
                $"{weekStart:MM.dd} ~ {weekEnd:MM.dd}";
        }
        else
        {
            weekText.text =
                $"{report.WeekStart} ~ {report.WeekEnd}";
        }

        // 생활습관
        habitAchievementText.text =
            $"목표 달성률     {report.HabitAchievementRate}%";

        habitCompletedText.text =
            $"완료한 목표     {report.CompletedHabitCount} / {report.TotalHabitCount}";

        // 미완료 목표 수 계산
        int incompleteCount = Mathf.Max(
            0,
            report.TotalHabitCount - report.CompletedHabitCount
        );

        if (incompleteHabitText != null)
        {
            incompleteHabitText.text =
                $"미완료 목표     {incompleteCount}개";
        }

        // 소비습관
        spendingBudgetText.text =
            $"주간 예산     {report.WeeklyBudget:N0}원";

        spendingSpentText.text =
            $"사용 금액     {report.WeeklySpent:N0}원";

        spendingUsageText.text =
            $"예산 사용률     {report.BudgetUsageRate}%";

        // AI 주간 피드백
        aiSummaryText.text =
            $"<b><color=#866020>한 주 요약</color></b>\n{report.AiSummary}";

        aiGoodPointText.text =
            $"<b><color=#866020>잘한 점</color></b>\n{report.GoodPoint}";

        aiImprovementText.text =
            $"<b><color=#866020>개선할 점</color></b>\n{report.Improvement}";

        aiNextWeekText.text =
            $"<b><color=#866020>다음 주 제안</color></b>\n{report.NextWeekSuggestion}";
    }

    private void OnClickBack()
    {
        Debug.Log("Weekly Report Back Button Clicked");

        // Weekly Report 진입 화면이 정해지면 실제 화면 이동 연결
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);
    }
}