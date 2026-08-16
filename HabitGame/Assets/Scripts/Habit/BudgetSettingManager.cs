using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BudgetSettingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField budgetInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;

    private SpendingService spendingService;
    private HabitUIManager uiManager;

    private void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;
        uiManager = FindObjectOfType<HabitUIManager>();

        saveButton.onClick.AddListener(OnClickSave);
        backButton.onClick.AddListener(OnClickBack); 
    }

    private async void OnClickSave()
    {
        if (string.IsNullOrWhiteSpace(budgetInput.text))
        {
            Debug.Log("예산을 입력해주세요.");
            return;
        }

        if (!int.TryParse(budgetInput.text, out int budget))
        {
            Debug.Log("숫자만 입력 가능합니다.");
            return;
        }

        saveButton.interactable = false;

        // ⭐ UI 먼저 변경
        if (SpendBudgetManager.Instance != null)
        {
            SpendBudgetManager.Instance.SetWeeklyBudget(budget);
        }

        CreateSpendingBudgetRequest request = new CreateSpendingBudgetRequest
        {
            BudgetAmount = budget,
            Period = "weekly"
        };

        try
        {
            Debug.Log("===== Save 버튼 클릭 =====");

            SpendingBudgetResponse response =
    await spendingService.CreateBudgetAsync(request);

            SpendBudgetManager.Instance.SetBudgetId(response.Id);

            SpendBudgetManager.Instance.SetWeeklyBudget(response.BudgetAmount);
        }
        catch (Exception e)
        {
            // 서버가 꺼져있어도 정상
            Debug.LogWarning(e.Message);
        }

        budgetInput.text = "";

        if (uiManager != null)
            uiManager.CloseBudgetSetting();
        else
            gameObject.SetActive(false);

        saveButton.interactable = true;
    }

    private void OnClickBack()
    {
        budgetInput.text = "";

        if (uiManager != null)
            uiManager.CloseBudgetSetting();
        else
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        saveButton.onClick.RemoveListener(OnClickSave);
        backButton.onClick.RemoveListener(OnClickBack);
    }
}