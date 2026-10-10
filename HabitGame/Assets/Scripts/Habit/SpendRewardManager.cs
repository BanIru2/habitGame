using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendRewardManager : MonoBehaviour
{
    public static SpendRewardManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI totalGoldText;
    [SerializeField] private TextMeshProUGUI baseGoldText;
    [SerializeField] private TextMeshProUGUI bonusGoldText;

    [Header("Streak UI")]
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI streakBonusText;

    [Header("Reward Claim")]
    [SerializeField] private Button claimRewardButton;

    [Header("Reward Setting")]
    [SerializeField] private int maxWeeklyGold = 1000;

    [Header("Streak Reward Setting")]
    [SerializeField] private int twoWeekBonus = 100;
    [SerializeField] private int threeWeekBonus = 200;
    [SerializeField] private int fourWeekBonus = 300;

    // 예산 절약에 따른 기본 보상
    private int baseGold = 0;

    // 특수 목표 보너스
    private int bonusGold = 0;

    // 연속 달성 보너스
    private int streakBonusGold = 0;

    // 연속 달성 주차
    private int streakCount = 0;

    // 사용률
    private float usedRate = 0f;

    // 보상 수령 처리 중 여부
    private bool isClaimingReward = false;

    // 현재 예산 보상 수령 여부
    private bool rewardClaimed = false;

    // 서버에서 조회한 지난주 미수령 정산 데이터
    // 이번 주 예산과 구분하여 보상 수령에 사용
    private SpendingPendingSettlementResponse pendingSettlement;


    // 최종 획득 가능 골드
    public int TotalGold =>
        baseGold +
        bonusGold +
        streakBonusGold;

    public int BaseGold => baseGold;
    public int BonusGold => bonusGold;
    public int StreakBonusGold => streakBonusGold;
    public int StreakCount => streakCount;

    private void Awake()
    {
        Instance = this;

        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.AddListener(
                ClaimReward
            );
        }
    }

    // =========================================================
    // 서버 SpendingOverviewResponse 반영
    // =========================================================
    public void ApplyOverview(SpendingOverviewResponse response)
    {
        if (response == null)
        {
            Debug.LogWarning(
                "SpendingOverviewResponse가 null입니다."
            );

            return;
        }

        SetStreakCount(
            response.StreakCount
        );

        Debug.Log(
            $"서버 소비 Streak 반영 : " +
            $"{response.StreakCount}주"
        );

        // =========================================
        // 지난주 미수령 정산 데이터 저장
        // =========================================
        pendingSettlement = response.PendingSettlement;

        // 지난주 미수령 정산이 있는지 확인
        if (pendingSettlement != null)
        {
            rewardClaimed = pendingSettlement.RewardClaimed;

            Debug.Log(
                $"[Spend Reward] 지난주 정산 확인 : " +
                $"Budget ID={pendingSettlement.BudgetId}, " +
                $"Gold={pendingSettlement.ExpectedGold}"
            );
        }
        else
        {
            rewardClaimed = false;

            Debug.Log(
                "[Spend Reward] 지난주 미수령 정산 없음"
            );
        }

        // 지난주 정산 정보를 반영한 화면 갱신
        UpdateUI();

    }

    // =========================================================
    // 소비 보상 수령
    // =========================================================
    public async void ClaimReward()
    {
        if (isClaimingReward)
        {
            return;
        }

        if (rewardClaimed)
        {
            Debug.LogWarning(
                "이미 수령한 소비 보상입니다."
            );

            return;
        }

        if (SpendBudgetManager.Instance == null)
        {
            Debug.LogWarning(
                "SpendBudgetManager.Instance를 찾을 수 없습니다."
            );

            return;
        }

        // =========================================
        // 지난주 미수령 정산의 Budget ID 사용
        // 이번 주 예산 ID와 혼동하지 않도록 분리
        // =========================================
        if (pendingSettlement == null)
        {
            Debug.LogWarning(
                "수령 가능한 지난주 소비 정산이 없습니다."
            );
            return;
        }

        long budgetId =
            pendingSettlement.BudgetId;


        if (budgetId <= 0)
        {
            Debug.LogWarning(
                "유효한 Budget ID가 없습니다."
            );

            return;
        }

        SpendingRewardClaimRequest request =
            new SpendingRewardClaimRequest
            {
                BudgetId = budgetId
            };

        try
        {
            isClaimingReward = true;

            if (claimRewardButton != null)
            {
                claimRewardButton.interactable = false;
            }

            Debug.Log(
                "===== 소비 보상 수령 요청 ====="
            );

            Debug.Log(
                "Budget ID : " +
                budgetId
            );

            SpendingRewardClaimResponse response =
                await ServiceRegistry.Instance.Spending
                    .ClaimRewardAsync(request);

            if (response == null)
            {
                Debug.LogWarning(
                    "소비 보상 수령 응답이 비어있습니다."
                );

                return;
            }

            rewardClaimed =
                response.RewardClaimed;

            Debug.Log(
                "===== 소비 보상 수령 응답 ====="
            );

            Debug.Log(
                "Budget ID : " +
                response.BudgetId
            );

            Debug.Log(
                "Earned Gold : " +
                response.EarnedGold
            );

            Debug.Log(
                "Total User Gold : " +
                response.Gold
            );

            Debug.Log(
                "Reward Claimed : " +
                response.RewardClaimed
            );


            if (response.RewardClaimed)
            {
                Debug.Log(
                    $"소비 보상 수령 완료 : " +
                    $"+{response.EarnedGold} Gold"
                );

                // =========================================
                // 지난주 보상 수령 완료 처리
                // =========================================

                // 이미 수령한 정산을 다시 요청하지 않도록 상태 저장
                rewardClaimed = true;

                // 지난주 미수령 정산 정보 제거
                pendingSettlement = null;

                // 이번 주 예산을 기준으로 예상 보상 표시
                // (이번 주 예산이 없으면 0 Gold)
                if (SpendBudgetManager.Instance != null)
                {
                    CalculateReward(
                        SpendBudgetManager.Instance.WeeklyBudget,
                        SpendBudgetManager.Instance.UsedMoney
                    );
                }

                // 보상 수령 완료 후 화면 갱신
                UpdateUI();

                Debug.Log(
                    "[Spend Reward] 지난주 정산 수령 완료 및 화면 갱신"
                );
            }

        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "소비 보상 수령 실패\n" +
                e.Message
            );
        }

        finally
        {
            // 보상 수령 요청 처리 종료
            isClaimingReward = false;

            // 서버 정산 상태를 기준으로 버튼 및 보상 UI 갱신
            // 지난주 미수령 정산이 없으면 버튼은 비활성화
            UpdateUI();
        }

    }

    // =========================================================
    // 예산 / 소비금액 기준 기본 보상 계산
    // =========================================================
    public void CalculateReward(int budget, int spent)
    {
        if (budget <= 0)
        {
            usedRate = 0f;
            baseGold = 0;

            UpdateUI();
            return;
        }

        usedRate =
            (float)spent / budget;

        int savedMoney =
            budget - spent;

        if (savedMoney < 0)
        {
            savedMoney = 0;
        }

        float savingRate =
            (float)savedMoney / budget;

        savingRate =
            Mathf.Clamp01(savingRate);

        baseGold =
            Mathf.RoundToInt(
                maxWeeklyGold *
                savingRate
            );

        Debug.Log(
            "===== 소비 보상 계산 ====="
        );

        Debug.Log(
            $"Budget : {budget:N0}"
        );

        Debug.Log(
            $"Spent : {spent:N0}"
        );

        Debug.Log(
            $"Used Rate : " +
            $"{usedRate * 100f:0}%"
        );

        Debug.Log(
            $"Saving Rate : " +
            $"{savingRate * 100f:0}%"
        );

        Debug.Log(
            $"Base Gold : {baseGold}"
        );

        Debug.Log(
            $"Special Goal Bonus : " +
            $"{bonusGold}"
        );

        Debug.Log(
            $"Streak Bonus : " +
            $"{streakBonusGold}"
        );

        Debug.Log(
            $"Total Gold : {TotalGold}"
        );

        UpdateUI();
    }

    // =========================================================
    // 특수 목표 보상 추가
    // =========================================================
    public void AddBonusGold(int value)
    {
        bonusGold += value;

        if (bonusGold < 0)
        {
            bonusGold = 0;
        }

        UpdateUI();
    }

    // =========================================================
    // 특수 목표 보상 직접 설정
    // =========================================================
    public void SetBonusGold(int value)
    {
        bonusGold =
            Mathf.Max(
                0,
                value
            );

        UpdateUI();
    }

    // =========================================================
    // 연속 달성 주차 설정
    // =========================================================
    public void SetStreakCount(int count)
    {
        streakCount =
            Mathf.Max(
                0,
                count
            );

        CalculateStreakBonus();
        UpdateUI();

        Debug.Log(
            $"소비습관 연속 달성 : " +
            $"{streakCount}주 / " +
            $"추가 보상 +{streakBonusGold} Gold"
        );
    }

    // =========================================================
    // 연속 달성 보상 계산
    // =========================================================
    private void CalculateStreakBonus()
    {
        if (streakCount >= 4)
        {
            streakBonusGold =
                fourWeekBonus;
        }
        else if (streakCount == 3)
        {
            streakBonusGold =
                threeWeekBonus;
        }
        else if (streakCount == 2)
        {
            streakBonusGold =
                twoWeekBonus;
        }
        else
        {
            streakBonusGold = 0;
        }
    }

    // =========================================================
    // 기존 SpendGoalItem 호환용
    // =========================================================
    public void AddGold(int value)
    {
        AddBonusGold(value);
    }

    // =========================================================
    // 기존 SpendGoalItem 호환용
    // =========================================================
    public void SetGold(int totalGold)
    {
        bonusGold =
            Mathf.Max(
                0,
                totalGold -
                baseGold -
                streakBonusGold
            );

        UpdateUI();
    }

    public int GetBaseGold()
    {
        return baseGold;
    }

    public int GetBonusGold()
    {
        return bonusGold;
    }

    public int GetStreakBonusGold()
    {
        return streakBonusGold;
    }

    public int GetStreakCount()
    {
        return streakCount;
    }

    public int GetTotalGold()
    {
        return TotalGold;
    }

    // =========================================================
    // UI 갱신
    // =========================================================
    private void UpdateUI()
    {
        if (totalGoldText != null)
        {
            totalGoldText.text =
                $"{TotalGold:N0} Gold";
        }

        if (baseGoldText != null)
        {
            baseGoldText.text =
                $"Base Reward " +
                $"({usedRate * 100f:0}% Used) : " +
                $"{baseGold:N0}";
        }

        if (bonusGoldText != null)
        {
            bonusGoldText.text =
                $"Bonus Goal : " +
                $"+{bonusGold:N0}";
        }

        if (streakText != null)
        {
            streakText.text =
                $"Weekly Streak : " +
                $"{streakCount} Weeks";
        }

        if (streakBonusText != null)
        {
            streakBonusText.text =
                $"Streak Bonus : " +
                $"+{streakBonusGold:N0}";
        }

        // =========================================
        // 지난주 미수령 정산 정보 표시
        // =========================================
        if (pendingSettlement != null && !rewardClaimed)
        {
            // 지난주에 확정된 실제 지급 예정 금액
            if (totalGoldText != null)
            {
                totalGoldText.text =
                    $"{pendingSettlement.ExpectedGold:N0} Gold";
            }

            // 지난주 확정 기본 보상
            if (baseGoldText != null)
            {
                baseGoldText.text =
                    $"기본 보상 : {pendingSettlement.BaseReward:N0} Gold";
            }

            // 지난주 특수 목표 보상
            if (bonusGoldText != null)
            {
                bonusGoldText.text =
                    $"특수 목표 보상 : +{pendingSettlement.SpecialGoalReward:N0} Gold";
            }

            // 지난주 연속 달성 횟수
            if (streakText != null)
            {
                streakText.text =
                    $"주간 연속 달성 : {pendingSettlement.StreakCount}주";
            }

            // 지난주 연속 달성 추가 보상
            if (streakBonusText != null)
            {
                streakBonusText.text =
                    $"연속 달성 보너스 : +{pendingSettlement.StreakBonus:N0} Gold";
            }
        }

        // =========================================
        // 지난주 미수령 정산이 있을 때만
        // 보상 수령 버튼 활성화
        // =========================================
        if (claimRewardButton != null)
        {
            claimRewardButton.interactable =
                pendingSettlement != null &&
                !rewardClaimed &&
                !isClaimingReward;
        }

    }

#if UNITY_EDITOR

    [ContextMenu("TEST - Streak 2 Weeks")]
    private void TestStreak2Weeks()
    {
        SetStreakCount(2);
    }

    [ContextMenu("TEST - Streak 3 Weeks")]
    private void TestStreak3Weeks()
    {
        SetStreakCount(3);
    }

    [ContextMenu("TEST - Streak 4 Weeks")]
    private void TestStreak4Weeks()
    {
        SetStreakCount(4);
    }

    [ContextMenu("TEST - Reset Streak")]
    private void TestResetStreak()
    {
        SetStreakCount(0);
    }

#endif

    private void OnDestroy()
    {
        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveListener(
                ClaimReward
            );
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}