using System;
using UnityEngine;
using UnityEngine.UI;

public class SpendTransactionItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle exceptionToggle;

    [Header("Test Mode")]
    [SerializeField] private bool useLocalMode = true;

    private SpendingService spendingService;

    private long transactionId = -1;
    private int transactionAmount = 0;

    private bool currentExceptionState = false;
    private bool isSubmitting = false;

    private void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;

        if (exceptionToggle != null)
        {
            exceptionToggle.onValueChanged.AddListener(
                OnExceptionToggleChanged
            );
        }
    }

    public void SetData(SpendingTransactionResponse transaction)
    {
        if (transaction == null)
        {
            Debug.LogWarning(
                "SpendingTransactionResponse가 없습니다."
            );
            return;
        }

        transactionId = transaction.Id;
        transactionAmount = transaction.Amount;
        currentExceptionState = transaction.IsException;

        if (exceptionToggle != null)
        {
            exceptionToggle.SetIsOnWithoutNotify(
                currentExceptionState
            );
        }
    }

    private async void OnExceptionToggleChanged(bool isException)
    {
        if (isSubmitting)
            return;

        // =========================================
        // LOCAL MODE
        // 서버 없이 예외 처리 + 사용금액 반영
        // =========================================
        if (useLocalMode)
        {
            if (SpendBudgetManager.Instance == null)
            {
                Debug.LogWarning(
                    "SpendBudgetManager.Instance가 없습니다."
                );

                RestoreToggle();
                return;
            }

            // false → true
            // 예외 처리된 금액은 사용금액에서 제외
            if (!currentExceptionState && isException)
            {
                SpendBudgetManager.Instance.AddSpending(
                    -transactionAmount
                );
            }

            // true → false
            // 예외 해제하면 사용금액에 다시 포함
            else if (currentExceptionState && !isException)
            {
                SpendBudgetManager.Instance.AddSpending(
                    transactionAmount
                );
            }

            currentExceptionState = isException;

            Debug.Log(
                "===== LOCAL MODE : 소비 예외 처리 ====="
            );

            Debug.Log(
                $"Transaction ID : {transactionId}"
            );

            Debug.Log(
                $"Amount : {transactionAmount:N0}₩"
            );

            Debug.Log(
                $"Exception : {currentExceptionState}"
            );

            return;
        }

        // =========================================
        // SERVER MODE
        // =========================================
        if (transactionId <= 0)
        {
            Debug.LogWarning(
                "TransactionId가 없어 예외 처리할 수 없습니다."
            );

            RestoreToggle();
            return;
        }

        if (spendingService == null)
        {
            Debug.LogWarning(
                "SpendingService를 사용할 수 없습니다."
            );

            RestoreToggle();
            return;
        }

        isSubmitting = true;

        if (exceptionToggle != null)
            exceptionToggle.interactable = false;

        try
        {
            UpdateSpendingExceptionRequest request =
                new UpdateSpendingExceptionRequest
                {
                    TransactionId = transactionId,
                    IsException = isException,
                    ExceptionReason = isException
                        ? "사용자 지정 예외"
                        : null
                };

            Debug.Log(
                "===== 소비 예외 처리 요청 시작 ====="
            );

            SpendingOverviewResponse response =
                await spendingService.UpdateExceptionAsync(
                    request
                );

            if (response == null)
            {
                Debug.LogWarning(
                    "소비 예외 처리 응답이 비어있습니다."
                );

                RestoreToggle();
                return;
            }

            currentExceptionState = isException;

            Debug.Log(
                "===== 소비 예외 처리 성공 ====="
            );

            Debug.Log(
                $"Transaction ID : {transactionId}"
            );

            Debug.Log(
                $"Exception : {currentExceptionState}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "소비 예외 처리 실패\n" +
                e.Message
            );

            RestoreToggle();
        }
        finally
        {
            isSubmitting = false;

            if (exceptionToggle != null)
                exceptionToggle.interactable = true;
        }
    }

    private void RestoreToggle()
    {
        if (exceptionToggle != null)
        {
            exceptionToggle.SetIsOnWithoutNotify(
                currentExceptionState
            );
        }
    }

    private void OnDestroy()
    {
        if (exceptionToggle != null)
        {
            exceptionToggle.onValueChanged.RemoveListener(
                OnExceptionToggleChanged
            );
        }
    }
}