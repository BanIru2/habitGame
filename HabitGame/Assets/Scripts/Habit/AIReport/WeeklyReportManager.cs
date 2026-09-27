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

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        if (ServiceRegistry.Instance != null)
            weeklyReportService = ServiceRegistry.Instance.WeeklyReport;

        LoadWeeklyReport();
    }

    private async void LoadWeeklyReport()
    {
        // 백엔드 미구현 상태에서는 Dummy 데이터 사용
        if (useLocalMode)
        {
            LoadDummyReport();
            return;
        }

        if (weeklyReportService == null)
        {
            Debug.LogError("WeeklyReportService를 찾을 수 없습니다.");
            return;
        }

        try
        {
            WeeklyReportResponse response =
                await weeklyReportService.GetWeeklyReportAsync();

            if (response == null)
            {
                Debug.LogWarning("Weekly Report 응답이 없습니다.");
                return;
            }

            ApplyReport(response);

            Debug.Log("Weekly Report 서버 데이터 로드 완료");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Weekly Report 서버 데이터 로드 실패: {e.Message}"
            );
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
            $"Achievement Rate     {report.HabitAchievementRate}%";

        habitCompletedText.text =
            $"Completed     {report.CompletedHabitCount} / {report.TotalHabitCount}";

        // 소비습관
        spendingBudgetText.text =
            $"Weekly Budget     {report.WeeklyBudget:N0} Won";

        spendingSpentText.text =
            $"Spent     {report.WeeklySpent:N0} Won";

        spendingUsageText.text =
            $"Budget Usage     {report.BudgetUsageRate}%";

        // AI 주간 피드백
        aiSummaryText.text =
            $"Summary\n{report.AiSummary}";

        aiGoodPointText.text =
            $"Good Point\n{report.GoodPoint}";

        aiImprovementText.text =
            $"Improvement\n{report.Improvement}";

        aiNextWeekText.text =
            $"Next Week\n{report.NextWeekSuggestion}";
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