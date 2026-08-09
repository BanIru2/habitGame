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
                await spendingService.GetOverviewAsync(ApiClient.Instance.CurrentUserId);

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
        budgetText.text = $"{weeklyBudget:N0}₩";

        float percent = weeklyBudget <= 0
            ? 0f
            : (float)usedMoney / weeklyBudget;

        // 혹시 100%를 넘어가면 Slider 오류 방지
        percent = Mathf.Clamp01(percent);

        usedText.text = $"{usedMoney:N0}₩ ({percent * 100f:0}%)";

        budgetSlider.value = percent;
    }
}