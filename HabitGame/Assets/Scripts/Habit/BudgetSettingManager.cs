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

        if (saveButton != null)
            saveButton.onClick.AddListener(OnClickSave);

        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);
    }

    private async void OnClickSave()
    {
        if (budgetInput == null)
        {
            Debug.LogWarning("Budget Input이 연결되지 않았습니다.");
            return;
        }

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

        if (budget <= 0)
        {
            Debug.Log("예산은 0원보다 커야 합니다.");
            return;
        }

        if (spendingService == null)
        {
            Debug.LogWarning("SpendingService를 사용할 수 없습니다.");
            return;
        }

        if (saveButton != null)
            saveButton.interactable = false;

        CreateSpendingBudgetRequest request =
            new CreateSpendingBudgetRequest
            {
                BudgetAmount = budget,
                Period = "weekly"
            };

        try
        {
            Debug.Log("===== 예산 저장 요청 시작 =====");

            SpendingBudgetResponse response =
                await spendingService.CreateBudgetAsync(request);

            if (response == null)
            {
                Debug.LogWarning(
                    "예산 저장 API 응답이 비어있습니다."
                );
                return;
            }

            if (SpendBudgetManager.Instance != null)
            {
                SpendBudgetManager.Instance.SetBudgetId(
                    response.Id
                );

                SpendBudgetManager.Instance.SetWeeklyBudget(
                    response.BudgetAmount
                );
            }
            else
            {
                Debug.LogWarning(
                    "SpendBudgetManager.Instance가 없습니다."
                );
            }

            Debug.Log("===== 예산 저장 성공 =====");
            Debug.Log($"Budget ID : {response.Id}");
            Debug.Log($"Budget Amount : {response.BudgetAmount}");

            budgetInput.text = "";

            if (uiManager != null)
                uiManager.CloseBudgetSetting();
            else
                gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "예산 저장 실패\n" +
                e.Message
            );
        }
        finally
        {
            if (saveButton != null)
                saveButton.interactable = true;
        }
    }

    private void OnClickBack()
    {
        if (budgetInput != null)
            budgetInput.text = "";

        if (uiManager != null)
            uiManager.CloseBudgetSetting();
        else
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnClickSave);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);
    }
}