using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the renewed spending main screen.
/// This component never accesses game managers, services, or an API.
/// </summary>
public sealed class SpendingReportPreviewController : MonoBehaviour
{
    [SerializeField] private TMP_Text budgetText;
    [SerializeField] private TMP_Text usedText;
    [SerializeField] private Slider budgetSlider;
    [SerializeField] private TMP_Text goalStatusText;
    [SerializeField] private TMP_Text rewardStatusText;
    [SerializeField] private TMP_Text localNoticeText;
    [SerializeField] private Button budgetSettingButton;
    [SerializeField] private Button recordButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button reportButton;
    [SerializeField] private Button toggleBudgetButton;
    [SerializeField] private Button toggleGoalButton;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private ScrollRect mainScroll;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject budgetSettingPanel;
    [SerializeField] private TMP_InputField budgetInput;
    [SerializeField] private Button budgetSaveButton;
    [SerializeField] private Button budgetBackButton;
    [SerializeField] private TMP_Text budgetSettingNoticeText;
    [SerializeField] private GameObject recordSpendPanel;
    [SerializeField] private TMP_InputField recordAmountInput;
    [SerializeField] private TMP_Dropdown recordCategoryDropdown;
    [SerializeField] private Button recordSaveButton;
    [SerializeField] private Button recordBackButton;
    [SerializeField] private TMP_Text recordNoticeText;
    [SerializeField] private Button openGoalSettingButton;
    [SerializeField] private GameObject goalSettingPanel;
    [SerializeField] private TMP_InputField goalNameInput;
    [SerializeField] private TMP_InputField goalLimitInput;
    [SerializeField] private Button dailyButton;
    [SerializeField] private Button weeklyButton;
    [SerializeField] private Button addGoalButton;
    [SerializeField] private Button skipGoalButton;
    [SerializeField] private Button completeGoalButton;
    [SerializeField] private TMP_Text goalSettingNoticeText;
    [SerializeField] private RectTransform goalPreviewContent;
    [SerializeField] private ScrollRect goalPreviewScroll;
    [SerializeField] private GameObject goalPreviewItemTemplate;
    [SerializeField] private GameObject spendHistoryPanel;
    [SerializeField] private RectTransform transactionPreviewContent;
    [SerializeField] private ScrollRect transactionPreviewScroll;
    [SerializeField] private GameObject transactionPreviewItemTemplate;
    [SerializeField] private Button historyBackButton;
    [SerializeField] private TMP_Text historyNoticeText;
    [SerializeField] private GameObject weeklyReportPanel;
    [SerializeField] private ScrollRect weeklyReportScroll;
    [SerializeField] private Button weeklyReportBackButton;
    [SerializeField] private TMP_Text reportWeekText;
    [SerializeField] private TMP_Text reportHabitAchievementText;
    [SerializeField] private TMP_Text reportHabitCompletedText;
    [SerializeField] private TMP_Text reportSpendingBudgetText;
    [SerializeField] private TMP_Text reportSpendingSpentText;
    [SerializeField] private TMP_Text reportSpendingUsageText;
    [SerializeField] private TMP_Text reportAiSummaryText;
    [SerializeField] private TMP_Text reportAiGoodPointText;
    [SerializeField] private TMP_Text reportAiImprovementText;
    [SerializeField] private TMP_Text reportAiNextWeekText;
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private int weeklyBudget = 500000;
    private int usedMoney = 320000;
    private bool goalAchieved;
    private bool rewardClaimed;
    private bool weeklyCycle;
    private readonly List<string> pendingGoals = new List<string>();
    private readonly List<PreviewTransaction> previewTransactions = new List<PreviewTransaction>();

    private sealed class PreviewTransaction
    {
        public string Category;
        public int Amount;
        public string Date;
        public bool IsException;
    }

    private void Awake()
    {
        Add(budgetSettingButton, OpenBudgetSetting);
        Add(recordButton, OpenRecordSpend);
        Add(historyButton, OpenSpendHistory);
        Add(reportButton, OpenWeeklyReport);
        Add(toggleBudgetButton, ToggleBudgetUsage);
        Add(toggleGoalButton, ToggleGoalState);
        Add(claimRewardButton, ClaimRewardLocally);
        Add(budgetSaveButton, SaveBudgetLocally);
        Add(budgetBackButton, CloseBudgetSettingWithoutSaving);
        Add(recordSaveButton, SaveRecordSpendLocally);
        Add(recordBackButton, CloseRecordSpendWithoutSaving);
        Add(openGoalSettingButton, OpenGoalSetting);
        Add(dailyButton, () => SelectGoalCycle(false));
        Add(weeklyButton, () => SelectGoalCycle(true));
        Add(addGoalButton, AddGoalLocally);
        Add(skipGoalButton, SkipGoalSetting);
        Add(completeGoalButton, CompleteGoalSetting);
        Add(historyBackButton, CloseSpendHistory);
        Add(weeklyReportBackButton, CloseWeeklyReport);
    }

    private void OpenWeeklyReport()
    {
        SetText(reportWeekText, "2026.10.12 ~ 2026.10.18");
        SetText(reportHabitAchievementText, "습관 달성률 82%");
        SetText(reportHabitCompletedText, "완료 18회 / 계획 22회");
        SetText(reportSpendingBudgetText, $"주간 예산 {weeklyBudget:N0}원");
        SetText(reportSpendingSpentText, $"사용 금액 {usedMoney:N0}원");
        float usageRate = weeklyBudget <= 0 ? 0f : (float)usedMoney / weeklyBudget * 100f;
        SetText(reportSpendingUsageText, $"예산 사용률 {usageRate:0}%");
        SetText(reportAiSummaryText,
            "AI 요약\n이번 주는 아침 물 마시기와 저녁 스트레칭을 꾸준히 실천했고, 주간 예산도 안정적으로 지켰습니다. 주 중반부터 소비가 조금 늘었지만 예산 범위 안에서 조절했습니다.");
        SetText(reportAiGoodPointText,
            "잘한 점\n하루 단위로 실천 가능한 습관을 선택해 연속 달성을 유지했습니다. 카페와 배달 소비를 기록한 뒤 즉시 조정한 덕분에 예산이 급격히 줄지 않았습니다.");
        SetText(reportAiImprovementText,
            "개선할 점\n금요일과 토요일에 식비가 모여 있어 주말 예산을 미리 나누어 두면 더 안정적입니다. 습관은 요일별 편차가 있었으므로 실패한 날을 포기로 여기지 말고 다음 회차를 바로 시작해보세요. 이 문장은 긴 한국어 피드백이 카드 너비에 맞게 자동으로 줄바꿈되고 카드 높이가 늘어나는지 확인하기 위한 고정 Preview 테스트 데이터입니다.");
        SetText(reportAiNextWeekText,
            "다음 주 제안\n월요일에 주말 식비 한도를 먼저 정하고, 아침 습관은 알림 직후 5분 안에 실천해보세요. 주간 중간에 한 번 예산과 습관 진행률을 함께 점검하면 주말의 부담을 줄일 수 있습니다.");

        if (mainPanel != null) mainPanel.SetActive(false);
        if (weeklyReportPanel != null) weeklyReportPanel.SetActive(true);
        RebuildWeeklyReportLayout();
    }

    private void CloseWeeklyReport()
    {
        if (weeklyReportPanel != null) weeklyReportPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        ShowLocalGuide("주간 리포트 Preview에서 소비 메인으로 돌아왔습니다.");
    }

    private void RebuildWeeklyReportLayout()
    {
        if (weeklyReportScroll == null || weeklyReportScroll.content == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(weeklyReportScroll.content);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(weeklyReportScroll.content);
        weeklyReportScroll.StopMovement();
        weeklyReportScroll.verticalNormalizedPosition = 1f;
    }

    private void OpenRecordSpend()
    {
        if (recordAmountInput != null) recordAmountInput.text = string.Empty;
        if (recordCategoryDropdown != null)
        {
            recordCategoryDropdown.value = 0;
            recordCategoryDropdown.RefreshShownValue();
        }
        SetText(recordNoticeText, "금액과 카테고리를 선택해주세요.");
        if (mainPanel != null) mainPanel.SetActive(false);
        if (recordSpendPanel != null) recordSpendPanel.SetActive(true);
    }

    private void SaveRecordSpendLocally()
    {
        string amountText = recordAmountInput != null ? recordAmountInput.text : string.Empty;
        if (!int.TryParse(amountText, out int amount) || amount <= 0)
        {
            SetText(recordNoticeText, "0원보다 큰 숫자 금액을 입력해주세요.");
            return;
        }
        if (recordCategoryDropdown == null || recordCategoryDropdown.value == 0)
        {
            SetText(recordNoticeText, "소비 카테고리를 선택해주세요.");
            return;
        }

        usedMoney += amount;
        previewTransactions.Insert(0, new PreviewTransaction
        {
            Category = recordCategoryDropdown.options[recordCategoryDropdown.value].text,
            Amount = amount,
            Date = System.DateTime.Now.ToString("yyyy.MM.dd"),
            IsException = false
        });
        RefreshBudget();
        CloseRecordSpend();
        ShowLocalGuide($"가상 사용액에 {amount:N0}원을 더했습니다. 실제 소비는 저장되지 않습니다.");
    }

    private void CloseRecordSpendWithoutSaving()
    {
        CloseRecordSpend();
        ShowLocalGuide("저장하지 않은 소비 기록 입력을 취소했습니다.");
    }

    private void CloseRecordSpend()
    {
        if (recordAmountInput != null) recordAmountInput.text = string.Empty;
        if (recordCategoryDropdown != null)
        {
            recordCategoryDropdown.value = 0;
            recordCategoryDropdown.RefreshShownValue();
        }
        if (recordSpendPanel != null) recordSpendPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void OpenBudgetSetting()
    {
        if (budgetInput != null) budgetInput.text = weeklyBudget.ToString();
        SetText(budgetSettingNoticeText, "현재 가상 예산이 입력되어 있습니다.");
        if (mainPanel != null) mainPanel.SetActive(false);
        if (budgetSettingPanel != null) budgetSettingPanel.SetActive(true);
    }

    private void SaveBudgetLocally()
    {
        if (budgetInput == null || string.IsNullOrWhiteSpace(budgetInput.text))
        {
            SetText(budgetSettingNoticeText, "예산을 입력해주세요.");
            return;
        }

        if (!int.TryParse(budgetInput.text, out int newBudget))
        {
            SetText(budgetSettingNoticeText, "예산은 숫자로 입력해주세요.");
            return;
        }

        if (newBudget <= 0)
        {
            SetText(budgetSettingNoticeText, "예산은 0원보다 커야 합니다.");
            return;
        }

        weeklyBudget = newBudget;
        RefreshBudget();
        CloseBudgetSetting();
        ShowLocalGuide($"가상 주간 예산을 {weeklyBudget:N0}원으로 변경했습니다.");
    }

    private void CloseBudgetSettingWithoutSaving()
    {
        CloseBudgetSetting();
        ShowLocalGuide("저장하지 않은 예산 변경을 취소했습니다.");
    }

    private void CloseBudgetSetting()
    {
        if (budgetSettingPanel != null) budgetSettingPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void OpenGoalSetting()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (goalSettingPanel != null) goalSettingPanel.SetActive(true);
        SetText(goalSettingNoticeText, pendingGoals.Count == 0
            ? "가상 소비 목표를 추가하거나 건너뛸 수 있습니다."
            : $"가상 목표 {pendingGoals.Count}개가 아직 완료되지 않았습니다.");
        RefreshGoalCycleButtons();
        RebuildGoalPreviewList();
    }

    private void SelectGoalCycle(bool weekly)
    {
        weeklyCycle = weekly;
        RefreshGoalCycleButtons();
        SetText(goalSettingNoticeText, weekly ? "Weekly 목표를 선택했습니다." : "Daily 목표를 선택했습니다.");
    }

    private void AddGoalLocally()
    {
        string goalName = goalNameInput != null ? goalNameInput.text.Trim() : string.Empty;
        string limitText = goalLimitInput != null ? goalLimitInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(goalName))
        {
            SetText(goalSettingNoticeText, "소비 목표 이름을 입력해주세요.");
            return;
        }
        if (!int.TryParse(limitText, out int limitAmount))
        {
            SetText(goalSettingNoticeText, "소비 한도는 숫자로 입력해주세요.");
            return;
        }
        if (limitAmount <= 0)
        {
            SetText(goalSettingNoticeText, "소비 한도는 0원보다 커야 합니다.");
            return;
        }

        pendingGoals.Add($"{goalName}|{limitAmount}|{(weeklyCycle ? "Weekly" : "Daily")}");
        if (goalNameInput != null) goalNameInput.text = string.Empty;
        if (goalLimitInput != null) goalLimitInput.text = string.Empty;
        RebuildGoalPreviewList();
        SetText(goalSettingNoticeText, $"가상 목표를 추가했습니다. 현재 {pendingGoals.Count}개");
    }

    private void SkipGoalSetting()
    {
        if (pendingGoals.Count > 0)
        {
            SetText(goalSettingNoticeText, "추가한 목표가 있습니다. 완료하려면 Complete를 눌러주세요.");
            return;
        }
        CloseGoalSetting();
        ShowLocalGuide("목표를 추가하지 않고 돌아왔습니다. 기존 가상 상태는 유지됩니다.");
    }

    private void CompleteGoalSetting()
    {
        if (pendingGoals.Count == 0)
        {
            SetText(goalSettingNoticeText, "추가한 목표가 없습니다. 건너뛰려면 Skip을 눌러주세요.");
            return;
        }

        string[] latest = pendingGoals[pendingGoals.Count - 1].Split('|');
        if (latest.Length == 3 && int.TryParse(latest[1], out int limitAmount))
            SetText(goalStatusText, $"{latest[0]} {limitAmount:N0}원 이하 · {latest[2]} · 가상 목표");
        int completedCount = pendingGoals.Count;
        pendingGoals.Clear();
        CloseGoalSetting();
        ShowLocalGuide($"가상 소비 목표 {completedCount}개를 등록하고 소비 메인으로 돌아왔습니다.");
    }

    private void CloseGoalSetting()
    {
        if (goalSettingPanel != null) goalSettingPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void RefreshGoalCycleButtons()
    {
        SetButtonColor(dailyButton, weeklyCycle ? Color.white : new Color32(0xFF, 0xE2, 0x91, 0xFF));
        SetButtonColor(weeklyButton, weeklyCycle ? new Color32(0xFF, 0xE2, 0x91, 0xFF) : Color.white);
    }

    private void RebuildGoalPreviewList()
    {
        if (goalPreviewContent == null || goalPreviewItemTemplate == null) return;
        for (int index = goalPreviewContent.childCount - 1; index >= 0; index--)
        {
            Transform child = goalPreviewContent.GetChild(index);
            if (child.gameObject != goalPreviewItemTemplate) Destroy(child.gameObject);
        }
        goalPreviewItemTemplate.SetActive(false);
        foreach (string value in pendingGoals)
        {
            string[] parts = value.Split('|');
            GameObject item = Instantiate(goalPreviewItemTemplate, goalPreviewContent);
            item.name = "GoalPreviewItem";
            TMP_Text itemText = item.GetComponentInChildren<TMP_Text>(true);
            SetText(itemText, parts.Length == 3
                ? $"{parts[0]}\n{int.Parse(parts[1]):N0}원 이하 · {parts[2]} · +300 G"
                : value);
            item.SetActive(true);
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(goalPreviewContent);
        if (goalPreviewScroll != null)
        {
            goalPreviewScroll.StopMovement();
            goalPreviewScroll.verticalNormalizedPosition = 0f;
        }
    }

    private void Start()
    {
        EnsurePreviewTransactions();
        RefreshBudget();
        RefreshGoal();
        RefreshReward();
        ShowLocalGuide("Preview 전용 가상 데이터 · 서버와 게임 Manager를 호출하지 않습니다.");
        if (mainScroll != null && mainScroll.content != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainScroll.content);
            mainScroll.StopMovement();
            mainScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void OpenSpendHistory()
    {
        EnsurePreviewTransactions();
        RebuildTransactionPreviewList();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (spendHistoryPanel != null) spendHistoryPanel.SetActive(true);
    }

    private void CloseSpendHistory()
    {
        if (spendHistoryPanel != null) spendHistoryPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        ShowLocalGuide("가상 소비 내역의 예외 상태를 유지한 채 소비 메인으로 돌아왔습니다.");
    }

    private void EnsurePreviewTransactions()
    {
        if (previewTransactions.Count > 0) return;
        AddPreviewTransaction("식비", 12800, "2026.10.20", false);
        AddPreviewTransaction("카페", 5500, "2026.10.20", false);
        AddPreviewTransaction("교통", 1450, "2026.10.19", false);
        AddPreviewTransaction("생활", 28900, "2026.10.19", true);
        AddPreviewTransaction("쇼핑", 42000, "2026.10.18", false);
        AddPreviewTransaction("건강", 18000, "2026.10.18", false);
        AddPreviewTransaction("문화", 15000, "2026.10.17", true);
        AddPreviewTransaction("식비", 9600, "2026.10.17", false);
        AddPreviewTransaction("카페", 4800, "2026.10.16", false);
        AddPreviewTransaction("교통", 3200, "2026.10.16", false);
        AddPreviewTransaction("생활", 11900, "2026.10.15", false);
        AddPreviewTransaction("기타", 7500, "2026.10.14", false);
    }

    private void AddPreviewTransaction(string category, int amount, string date, bool isException)
    {
        previewTransactions.Add(new PreviewTransaction
        {
            Category = category,
            Amount = amount,
            Date = date,
            IsException = isException
        });
    }

    private void RebuildTransactionPreviewList()
    {
        if (transactionPreviewContent == null || transactionPreviewItemTemplate == null) return;
        for (int index = transactionPreviewContent.childCount - 1; index >= 0; index--)
        {
            Transform child = transactionPreviewContent.GetChild(index);
            if (child.gameObject != transactionPreviewItemTemplate) Destroy(child.gameObject);
        }
        transactionPreviewItemTemplate.SetActive(false);
        for (int index = 0; index < previewTransactions.Count; index++)
        {
            int transactionIndex = index;
            PreviewTransaction transaction = previewTransactions[index];
            GameObject item = Instantiate(transactionPreviewItemTemplate, transactionPreviewContent);
            item.name = "TransactionItem";
            SetText(item.transform.Find("CategoryText")?.GetComponent<TMP_Text>(), transaction.Category);
            SetText(item.transform.Find("AmountText")?.GetComponent<TMP_Text>(), $"-{transaction.Amount:N0}원");
            SetText(item.transform.Find("DateText")?.GetComponent<TMP_Text>(), transaction.Date);
            Toggle toggle = item.transform.Find("ExceptionToggle")?.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(transaction.IsException);
                RefreshExceptionToggle(toggle, transaction.IsException);
                toggle.onValueChanged.AddListener(isOn =>
                {
                    previewTransactions[transactionIndex].IsException = isOn;
                    RefreshExceptionToggle(toggle, isOn);
                    SetText(historyNoticeText, isOn
                        ? $"{transaction.Category} {transaction.Amount:N0}원을 예외 처리했습니다. Preview 로컬 상태만 변경됩니다."
                        : $"{transaction.Category} {transaction.Amount:N0}원의 예외 처리를 해제했습니다. Preview 로컬 상태만 변경됩니다.");
                });
            }
            item.SetActive(true);
        }
        SetText(historyNoticeText, $"가상 거래 {previewTransactions.Count}건 · 서버와 실제 예산 계산은 호출하지 않습니다.");
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transactionPreviewContent);
        if (transactionPreviewScroll != null)
        {
            transactionPreviewScroll.StopMovement();
            transactionPreviewScroll.verticalNormalizedPosition = 1f;
        }
    }

    private static void RefreshExceptionToggle(Toggle toggle, bool isOn)
    {
        Image background = toggle != null ? toggle.targetGraphic as Image : null;
        if (background != null)
            background.color = isOn ? new Color32(0xE7, 0xA8, 0x35, 0xFF) : new Color32(0xFF, 0xF9, 0xEA, 0xFF);
        TMP_Text label = toggle != null ? toggle.GetComponentInChildren<TMP_Text>(true) : null;
        SetText(label, isOn ? "예외 적용" : "예외 처리");
    }

    private void ToggleBudgetUsage()
    {
        usedMoney = usedMoney == 320000 ? 475000 : usedMoney == 475000 ? 550000 : 320000;
        RefreshBudget();
        ShowLocalGuide("가상 사용액을 변경했습니다. 실제 예산 데이터에는 반영되지 않습니다.");
    }

    private void ToggleGoalState()
    {
        goalAchieved = !goalAchieved;
        RefreshGoal();
        ShowLocalGuide(goalAchieved
            ? "가상 소비 목표를 달성 상태로 표시했습니다."
            : "가상 소비 목표를 진행 중 상태로 표시했습니다.");
    }

    private void ClaimRewardLocally()
    {
        rewardClaimed = !rewardClaimed;
        RefreshReward();
        ShowLocalGuide(rewardClaimed
            ? "Preview에서만 보상 수령 상태로 바뀌었습니다. 실제 골드는 지급되지 않습니다."
            : "가상 보상 상태를 다시 수령 가능으로 되돌렸습니다.");
    }

    private void RefreshBudget()
    {
        float rate = weeklyBudget <= 0 ? 0f : (float)usedMoney / weeklyBudget;
        SetText(budgetText, $"{weeklyBudget:N0}원");
        SetText(usedText, $"{usedMoney:N0}원 ({rate * 100f:0}%)");
        if (budgetSlider != null) budgetSlider.value = Mathf.Clamp01(rate);
    }

    private void RefreshGoal()
    {
        SetText(goalStatusText, goalAchieved
            ? "외식비 80,000원 이하 · 달성 완료 · +300 G"
            : "외식비 80,000원 이하 · 62,000원 사용 · 진행 중");
    }

    private void RefreshReward()
    {
        SetText(rewardStatusText, rewardClaimed
            ? "수령 완료 · 실제 골드 변동 없음"
            : "3주 연속 달성 · 예상 보상 860 G");
        if (claimRewardButton != null)
        {
            TMP_Text label = claimRewardButton.GetComponentInChildren<TMP_Text>(true);
            SetText(label, rewardClaimed ? "수령 상태 되돌리기" : "가상 보상 수령");
        }
    }

    private void ShowLocalGuide(string message)
    {
        SetText(localNoticeText, message);
    }

    private static void Add(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        Image image = button != null ? button.GetComponent<Image>() : null;
        if (image != null) image.color = color;
    }
}
