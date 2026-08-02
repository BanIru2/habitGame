using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SpendBudgetManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI budgetText;
    [SerializeField] private TextMeshProUGUI usedText;
    [SerializeField] private Slider budgetSlider;

    private int weeklyBudget = 500000;
    private int usedMoney = 320000;

    void Start()
    {
        RefreshUI();
    }

    public void SetWeeklyBudget(int budget)
    {
        weeklyBudget = budget;
        RefreshUI();
    }

    public void SetUsedMoney(int money)
    {
        usedMoney = money;
        RefreshUI();
    }

    private void RefreshUI()
    {
        budgetText.text = weeklyBudget.ToString("N0") + "₩";

        float percent = (float)usedMoney / weeklyBudget;

        usedText.text = $"Used : {usedMoney:N0} ({percent * 100f:0}%)";

        budgetSlider.value = percent;
    }
}
