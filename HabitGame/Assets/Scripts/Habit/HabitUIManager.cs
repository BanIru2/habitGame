using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HabitUIManager : Singleton<HabitUIManager>
{
    [SerializeField] private GameObject topTab;

    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject spendPanel;

    [SerializeField] private GameObject lifeAddPanel;
    [SerializeField] private GameObject spendAddPanel;

    [SerializeField] private GameObject budgetSettingPanel;
    [SerializeField] private GameObject spendHistoryPanel;

    // ⭐ HabitListManager 연결
    [SerializeField] private HabitListManager habitListManager;

    private HabitService habitService;

    private void Start()
    {
        habitService = new HabitService(ApiClient.Instance);

        OpenLife();
    }

    // ⭐ MainTapManager에서 호출하는 습관 탭 진입 함수
    public async Task OpenHabitTap()
    {
        // 1. DB에서 습관 목표 조회
        List<HabitGoalResponse> habits =
            await habitService.GetGoalsAsync();

        // 2. 조회한 데이터를 Life UI에 반영
        habitListManager.RefreshHabitList(habits);

        Debug.Log($"[Habit] 습관 목표 {habits.Count}개 조회 완료");
    }

    public void OpenLife()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(true);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
        spendHistoryPanel.SetActive(false);
    }

    public void OpenSpend()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(false);
        spendPanel.SetActive(true);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
        spendHistoryPanel.SetActive(false);
    }

    public void OpenLifeAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(true);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
        spendHistoryPanel.SetActive(false);
    }

    public void OpenSpendAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(true);
        budgetSettingPanel.SetActive(false);
        spendHistoryPanel.SetActive(false);
    }

    public void OpenBudgetSetting()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);

        budgetSettingPanel.SetActive(true);
        spendHistoryPanel.SetActive(false);
    }

    public void CloseBudgetSetting()
    {
        OpenSpend();
    }

    public void BackToLife()
    {
        OpenLife();
    }

    public void BackToSpend()
    {
        OpenSpend();
    }

    public void OpenSpendHistory()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);

        spendHistoryPanel.SetActive(true);
    }

    public void BackToSpendHistory()
    {
        OpenSpend();
    }
}