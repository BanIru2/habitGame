using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class BudgetTest : MonoBehaviour
{
    private SpendingService spendingService;
    private CreateSpendingBudgetRequest request;
    private SpendingOverviewResponse response;
    private SpendingBudgetResponse budgetResponse;

    [SerializeField]
    private Button button;
    // ----------------------임시 요청용 데이터셋-------------------------
    [SerializeField]
    private int budgetAmount;
    [SerializeField]
    private string weekStart;
    [SerializeField]
    private string weekEnd;
    [SerializeField]
    private List<CreateSpendingSpecialGoalData> specialGoals = new List<CreateSpendingSpecialGoalData>();
    // ---------------------------------------------------------------------

    private void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;
    }

    private void Awake()
    {
        button.onClick.AddListener(OnClickCreateBudget);
    }

    private async void OnClickCreateBudget()
    {
        try
        {
            button.interactable = false;
            request = new CreateSpendingBudgetRequest { BudgetAmount = budgetAmount, WeekStart = weekStart, WeekEnd = weekEnd, SpecialGoals = specialGoals };
            response = await spendingService.CreateBudgetAsync(request);
            if(response != null)
            {
                Debug.Log($"예산 생성 성공 : {response.Budget.BudgetAmount}원");
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"[Budgettest]예산 설정 에러 발생 : {e.Message}");
        }
        finally
        {
            button.interactable = true;
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClickCreateBudget);
    }
}
