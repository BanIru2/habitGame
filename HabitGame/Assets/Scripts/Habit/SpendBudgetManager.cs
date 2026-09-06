using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendBudgetManager : MonoBehaviour
{
    public static SpendBudgetManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI budgetText;
    [SerializeField] private TextMeshProUGUI usedText;
    [SerializeField] private Slider budgetSlider;

    // 서버 연결 실패 시 보여줄 임시 기본값
    private int weeklyBudget = 500000;
    private int usedMoney = 320000;

    // 현재 주간 예산 ID
    public long BudgetId { get; private set; }

    // 다른 Manager에서 현재 예산/사용금액 조회
    public int WeeklyBudget => weeklyBudget;
    public int UsedMoney => usedMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "SpendBudgetManager Instance가 중복 생성되었습니다."
            );
        }

        Instance = this;
    }

    private async void Start()
    {
        RefreshUI();

        await LoadOverview();
    }

    // =========================================
    // 서버에서 현재 주간 소비 Overview 조회
    // =========================================
    public async Task LoadOverview()
    {
        try
        {
            Debug.Log(
                "===== Spending Overview 조회 시작 ====="
            );

            SpendingOverviewResponse response =
                await ServiceRegistry.Instance.Spending
                    .GetOverviewAsync(
                        ApiClient.Instance.CurrentUserId
                    );

            if (response == null)
            {
                Debug.LogWarning(
                    "Spending Overview API 응답이 비어있습니다."
                );
                return;
            }

            BudgetId =
                response.BudgetId;

            weeklyBudget =
                response.BudgetAmount;

            usedMoney =
                response.CurrentSpent;

            RefreshUI();

            Debug.Log(
                "===== Spending Overview 조회 성공 ====="
            );

            Debug.Log(
                "Budget ID : " +
                BudgetId
            );

            Debug.Log(
                "Weekly Budget : " +
                weeklyBudget
            );

            Debug.Log(
                "Used Money : " +
                usedMoney
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "Spending Overview 조회 실패\n" +
                e.Message
            );
        }
    }

    // =========================================
    // Budget ID 설정
    // =========================================
    public void SetBudgetId(long budgetId)
    {
        BudgetId = budgetId;
    }

    // =========================================
    // 주간 예산 설정
    // =========================================
    public void SetWeeklyBudget(int budget)
    {
        weeklyBudget =
            Mathf.Max(0, budget);

        // 새로운 주간 예산 설정 시 사용 금액 초기화
        usedMoney = 0;

        RefreshUI();
    }

    // =========================================
    // 사용 금액 설정
    // =========================================
    public void SetUsedMoney(int money)
    {
        usedMoney =
            Mathf.Max(0, money);

        RefreshUI();
    }

    // =========================================
    // 사용 금액 증감
    // 양수 = 소비 추가
    // 음수 = 예외 처리 등으로 소비 제외
    // =========================================
    public void AddSpending(int amount)
    {
        usedMoney += amount;

        // 사용 금액은 0 아래로 내려가지 않게 보호
        usedMoney =
            Mathf.Max(0, usedMoney);

        RefreshUI();

        Debug.Log(
            $"사용 금액 변경 : {amount:N0}₩ / " +
            $"현재 사용 금액 : {usedMoney:N0}₩"
        );
    }

    // =========================================
    // UI 갱신
    // =========================================
    private void RefreshUI()
    {
        if (budgetText != null)
        {
            budgetText.text =
                $"{weeklyBudget:N0}₩";
        }

        float percent =
            weeklyBudget <= 0
                ? 0f
                : (float)usedMoney / weeklyBudget;

        float displayPercent =
            percent * 100f;

        if (usedText != null)
        {
            usedText.text =
                $"{usedMoney:N0}₩ ({displayPercent:0}%)";
        }

        if (budgetSlider != null)
        {
            budgetSlider.value =
                Mathf.Clamp01(percent);
        }

        if (SpendRewardManager.Instance != null)
        {
            SpendRewardManager.Instance
                .CalculateReward(
                    weeklyBudget,
                    usedMoney
                );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}