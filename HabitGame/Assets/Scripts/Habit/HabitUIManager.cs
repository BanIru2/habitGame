using UnityEngine;

public class HabitUIManager : MonoBehaviour
{
    [SerializeField] private GameObject topTab;

    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject spendPanel;

    [SerializeField] private GameObject lifeAddPanel;
    [SerializeField] private GameObject spendAddPanel;

    [SerializeField] private GameObject budgetSettingPanel;

    private void Start()
    {
        OpenLife();
    }

    public void OpenLife()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(true);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
    }

    public void OpenSpend()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(false);
        spendPanel.SetActive(true);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
    }

    public void OpenLifeAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(true);
        spendAddPanel.SetActive(false);
        budgetSettingPanel.SetActive(false);
    }

    public void OpenSpendAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(true);
        budgetSettingPanel.SetActive(false);
    }

    // ⭐ 예산 설정창
    public void OpenBudgetSetting()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);

        budgetSettingPanel.SetActive(true);
    }

    // ⭐ 예산창 닫고 Spend으로
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
}