using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HabitUIManager : Singleton<HabitUIManager>
{
    [Header("Top Tab")]
    [SerializeField] private GameObject topTab;

    [Header("Main Panels")]
    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject spendPanel;

    [Header("Add Panels")]
    [SerializeField] private GameObject lifeAddPanel;
    [SerializeField] private GameObject spendAddPanel;

    [Header("Spending Panels")]
    [SerializeField] private GameObject budgetSettingPanel;
    [SerializeField] private GameObject spendHistoryPanel;

    [Header("Habit")]
    [SerializeField] private HabitListManager habitListManager;

    private void Start()
    {
        OpenLife();
    }

    // =========================================
    // Habit 탭 진입
    // MainTapManager 등에서 호출
    // =========================================
    public async Task OpenHabitTap()
    {
        OpenLife();

        if (habitListManager == null)
        {
            Debug.LogError(
                "HabitUIManager의 HabitListManager가 연결되지 않았습니다."
            );
            return;
        }

        try
        {
            Debug.Log(
                "===== Habit 목표 목록 조회 시작 ====="
            );

            List<HabitGoalResponse> habits =
                await ServiceRegistry.Instance.Habit
                    .GetGoalsAsync();

            if (habits == null)
            {
                Debug.LogWarning(
                    "Habit 목표 목록 API 응답이 비어있습니다."
                );
                return;
            }

            habitListManager.RefreshHabitList(habits);

            Debug.Log(
                $"[Habit] 습관 목표 {habits.Count}개 조회 완료"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "Habit 목표 목록 조회 실패\n" +
                e.Message
            );
        }
    }

    // =========================================
    // 생활습관 메인
    // =========================================
    public void OpenLife()
    {
        SetActive(topTab, true);

        SetActive(lifePanel, true);
        SetActive(spendPanel, false);

        SetActive(lifeAddPanel, false);
        SetActive(spendAddPanel, false);

        SetActive(budgetSettingPanel, false);
        SetActive(spendHistoryPanel, false);
    }

    // =========================================
    // 소비습관 메인
    // =========================================
    public void OpenSpend()
    {
        SetActive(topTab, true);

        SetActive(lifePanel, false);
        SetActive(spendPanel, true);

        SetActive(lifeAddPanel, false);
        SetActive(spendAddPanel, false);

        SetActive(budgetSettingPanel, false);
        SetActive(spendHistoryPanel, false);
    }

    // =========================================
    // 생활습관 추가 화면
    // =========================================
    public void OpenLifeAddPanel()
    {
        SetActive(topTab, false);

        SetActive(lifePanel, false);
        SetActive(spendPanel, false);

        SetActive(lifeAddPanel, true);
        SetActive(spendAddPanel, false);

        SetActive(budgetSettingPanel, false);
        SetActive(spendHistoryPanel, false);
    }

    // =========================================
    // 소비습관 추가 화면
    // =========================================
    public void OpenSpendAddPanel()
    {
        SetActive(topTab, false);

        SetActive(lifePanel, false);
        SetActive(spendPanel, false);

        SetActive(lifeAddPanel, false);
        SetActive(spendAddPanel, true);

        SetActive(budgetSettingPanel, false);
        SetActive(spendHistoryPanel, false);
    }

    // =========================================
    // 예산 설정 화면
    // =========================================
    public void OpenBudgetSetting()
    {
        SetActive(topTab, false);

        SetActive(lifePanel, false);
        SetActive(spendPanel, false);

        SetActive(lifeAddPanel, false);
        SetActive(spendAddPanel, false);

        SetActive(budgetSettingPanel, true);
        SetActive(spendHistoryPanel, false);
    }

    public void CloseBudgetSetting()
    {
        OpenSpend();
    }

    // =========================================
    // 생활습관 화면 복귀
    // =========================================
    public void BackToLife()
    {
        OpenLife();
    }

    // =========================================
    // 소비습관 화면 복귀
    // =========================================
    public void BackToSpend()
    {
        OpenSpend();
    }

    // =========================================
    // 소비 내역 화면
    // =========================================
    public void OpenSpendHistory()
    {
        SetActive(topTab, false);

        SetActive(lifePanel, false);
        SetActive(spendPanel, false);

        SetActive(lifeAddPanel, false);
        SetActive(spendAddPanel, false);

        SetActive(budgetSettingPanel, false);
        SetActive(spendHistoryPanel, true);
    }

    public void BackToSpendHistory()
    {
        OpenSpend();
    }

    // =========================================
    // 안전한 Panel 활성화/비활성화
    // =========================================
    private void SetActive(
        GameObject target,
        bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}