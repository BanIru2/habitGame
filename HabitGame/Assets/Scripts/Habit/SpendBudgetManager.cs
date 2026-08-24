using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendBudgetManager : MonoBehaviour
{
    public static SpendBudgetManager Instance;

    private SpendingService spendingService;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI budgetText;
    [SerializeField] private TextMeshProUGUI usedText;
    [SerializeField] private Slider budgetSlider;

    private int weeklyBudget = 500000;
    private int usedMoney = 320000;

    // 현재 주간 예산 ID
    public long BudgetId { get; private set; }

    // ⭐ 다른 Manager에서 현재 예산/사용금액을 읽을 수 있도록
    public int WeeklyBudget => weeklyBudget;
    public int UsedMoney => usedMoney;

    private void Awake()
    {
        Instance = this;
    }

    private async void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;

        RefreshUI();

        await LoadOverview();
    }

    private async Task LoadOverview()
    {
        Debug.Log($"Current User : {ApiClient.Instance.CurrentUserId}");

        try
        {
            SpendingOverviewResponse response =
                await spendingService.GetOverviewAsync(
                    ApiClient.Instance.CurrentUserId
                );

            if (response != null)
            {
                BudgetId = response.BudgetId;

                weeklyBudget = response.BudgetAmount;
                usedMoney = response.CurrentSpent;

                RefreshUI();

                Debug.Log("Overview 불러오기 성공");
            }
        }
        catch (System.Exception e)
        {
            // 서버가 꺼져있으면 정상
            Debug.LogWarning(e.Message);
        }
    }

    public void SetBudgetId(long budgetId)
    {
        BudgetId = budgetId;
    }

    public void SetWeeklyBudget(int budget)
    {
        weeklyBudget = budget;

        // 새로운 주간 예산 설정 시 사용 금액 초기화
        usedMoney = 0;

        RefreshUI();
    }

    public void SetUsedMoney(int money)
    {
        usedMoney = money;

        RefreshUI();
    }

    public void AddSpending(int amount)
    {
        usedMoney += amount;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // 예산 표시
        if (budgetText != null)
        {
            budgetText.text = $"{weeklyBudget:N0}₩";
        }

        // 사용률 계산
        float percent = weeklyBudget <= 0
            ? 0f
            : (float)usedMoney / weeklyBudget;

        // 텍스트에는 실제 사용률 표시
        float displayPercent = percent * 100f;

        if (usedText != null)
        {
            usedText.text =
                $"{usedMoney:N0}₩ ({displayPercent:0}%)";
        }

        // Slider는 0 ~ 1 범위로 제한
        if (budgetSlider != null)
        {
            budgetSlider.value = Mathf.Clamp01(percent);
        }

        // ⭐ 사용금액이 변할 때마다 예상 보상도 다시 계산
        if (SpendRewardManager.Instance != null)
        {
            SpendRewardManager.Instance.CalculateReward(
                weeklyBudget,
                usedMoney
            );
        }
    }
}