
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordSpendManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TMP_Dropdown categoryDropdown;
    [SerializeField] private TMP_InputField merchantNameInput;
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private Toggle exceptionToggle;

    [Header("Category API Codes")]
    [Tooltip("드롭다운 옵션 순서대로 API 코드를 입력하세요. 0번은 빈 문자열.")]
    [SerializeField]
    private List<string> categoryApiValues =
        new List<string>();

    [Header("Manager")]
    [SerializeField] private OpenRecordPanel panelManager;
    [SerializeField] private SpendTransactionManager transactionManager;

    [Header("Button")]
    [SerializeField] private Button saveButton;

    private bool isSaving = false;

    // 저장 결과가 불확실할 때 동일한 키로 재시도
    private CreateSpendingTransactionRequest pendingRequest;

    // POST 저장 성공 후 조회만 실패한 경우
    private bool pendingSaved = false;

    public async void Save()
    {
        if (isSaving)
            return;

        isSaving = true;

        if (saveButton != null)
            saveButton.interactable = false;

        try
        {
            if (!pendingSaved)
            {
                CreateSpendingTransactionRequest request =
                    GetOrCreateRequest();

                if (request == null)
                    return;

                if (ServiceRegistry.Instance == null ||
                    ServiceRegistry.Instance.Spending == null)
                {
                    throw new Exception(
                        "SpendingService를 사용할 수 없습니다.");
                }

                SpendingTransactionResponse response =
                    await ServiceRegistry.Instance.Spending
                        .CreateTransactionAsync(request);

                if (response == null || response.Id <= 0)
                {
                    throw new Exception(
                        "거래 저장 결과를 확인하지 못했습니다.");
                }

                pendingSaved = true;

                Debug.Log(
                    $"[수동 소비 저장 성공] ID={response.Id}");
            }

            // 저장 성공 후 서버의 실제 데이터로 갱신
            await RefreshServerData();

            // 저장과 조회가 모두 성공한 경우
            pendingRequest = null;
            pendingSaved = false;

            amountInput.text = "";

            if (merchantNameInput != null)
                merchantNameInput.text = "";

            if (descriptionInput != null)
                descriptionInput.text = "";

            if (exceptionToggle != null)
                exceptionToggle.SetIsOnWithoutNotify(false);

            categoryDropdown.value = 0;
            categoryDropdown.RefreshShownValue();

            if (panelManager != null)
                panelManager.BackToSpend();

            Debug.Log("수동 소비 저장 및 화면 갱신 완료");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "수동 소비 등록 처리 실패: " + e.Message);

            if (pendingSaved)
            {
                Debug.LogWarning(
                    "거래는 DB에 저장되었습니다. " +
                    "저장 버튼을 다시 누르면 목록과 예산만 갱신합니다.");
            }
            else
            {
                Debug.LogWarning(
                    "저장 여부를 확인하지 못했습니다. " +
                    "입력값을 유지한 채 재시도해주세요.");
            }
        }
        finally
        {
            isSaving = false;

            if (saveButton != null)
                saveButton.interactable = true;
        }
    }

    private CreateSpendingTransactionRequest GetOrCreateRequest()
    {
        if (amountInput == null ||
            categoryDropdown == null ||
            merchantNameInput == null)
        {
            Debug.LogWarning(
                "필수 입력 UI가 연결되지 않았습니다.");
            return null;
        }

        if (!int.TryParse(amountInput.text, out int amount) ||
            amount <= 0)
        {
            Debug.LogWarning(
                "0원보다 큰 금액을 입력해주세요.");
            return null;
        }

        int categoryIndex = categoryDropdown.value;

        if (categoryIndex <= 0 ||
            categoryIndex >= categoryApiValues.Count ||
            categoryIndex >= categoryDropdown.options.Count)
        {
            Debug.LogWarning(
                "카테고리 API 코드 설정을 확인해주세요.");
            return null;
        }

        string apiCategory =
            categoryApiValues[categoryIndex];

        if (string.IsNullOrWhiteSpace(apiCategory))
        {
            Debug.LogWarning(
                "선택한 카테고리의 API 코드가 비어 있습니다.");
            return null;
        }

        string merchantName = merchantNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(merchantName))
        {
            Debug.LogWarning("사용처를 입력해주세요.");
            return null;
        }

        string description = descriptionInput != null
            ? descriptionInput.text.Trim()
            : "";

        bool isException =
            exceptionToggle != null && exceptionToggle.isOn;

        // 이전 요청이 있다면 동일 거래인지 확인
        if (pendingRequest != null)
        {
            bool sameRequest =
                pendingRequest.Amount == amount &&
                pendingRequest.Category == apiCategory &&
                pendingRequest.MerchantName == merchantName &&
                pendingRequest.Description == description &&
                pendingRequest.IsException == isException;

            if (!sameRequest)
            {
                Debug.LogWarning(
                    "이전 거래의 저장 결과가 확인되지 않았습니다. " +
                    "중복 방지를 위해 원래 입력값으로 재시도해주세요.");
                return null;
            }

            return pendingRequest;
        }

        // 신규 거래 요청 생성
        pendingRequest = new CreateSpendingTransactionRequest
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            Amount = amount,
            MerchantName = merchantName,
            Category = apiCategory,
            TransactionType = "EXPENSE",
            Description = description,
            IsException = isException,
            ExceptionReason = isException
                ? "사용자 지정 예외"
                : null,
            RecordedAt =
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        return pendingRequest;
    }

    private async Task RefreshServerData()
    {
        if (transactionManager == null)
        {
            throw new Exception(
                "SpendTransactionManager가 연결되지 않았습니다.");
        }

        if (SpendBudgetManager.Instance == null)
        {
            throw new Exception(
                "SpendBudgetManager.Instance를 찾을 수 없습니다.");
        }

        await Task.WhenAll(
            transactionManager.LoadTransactions(true),
            SpendBudgetManager.Instance.LoadOverview(true)
        );
    }
}
