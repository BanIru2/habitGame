using System;    // Exception 사용
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendingTest : MonoBehaviour
{
    private SpendingService spendingService;
    private SpendingOverviewResponse response;
    private SpendingBudgetResponse budgetResponse;

    [SerializeField]
    private Button button;
    [SerializeField]
    private TextMeshProUGUI budgetText;

    private void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;
    }

    private void Awake()
    {
        // 버튼 리스너 연결
        if(button != null)
        {
            button.onClick.AddListener(OnClickGetSpendingOverview);
        }
    }

    private async void OnClickGetSpendingOverview ()
    {
        try
        {
            // 로딩 중 버튼 비활성화
            button.interactable = false;
            // Service -> ApiClient를 거쳐 was로 Request DTO 전달 후 Response 받아오기
            response = await spendingService.GetOverviewAsync();
            if (response != null)
            {
                if (response.Budget != null)
                {
                    budgetResponse = response.Budget;
                    UpdateDisplay();
                }
                else
                {
                    Debug.LogWarning("Budget 정보 없음");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"통신 에러 발생 : {e.Message}");
        }
        finally    // 무조건 실행
        {
            // 동작 후 버튼 재활성화
            button.interactable = true;
        }
    }

    private void UpdateDisplay()
    {
        budgetText.text = budgetResponse.BudgetAmount.ToString();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClickGetSpendingOverview);
    }
}
