using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecordSpendManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TMP_Dropdown categoryDropdown;

    [Header("Manager")]
    [SerializeField] private OpenRecordPanel panelManager;
    [SerializeField] private SpendTransactionManager transactionManager;

    public void Save()
    {
        // 금액 입력 확인
        if (string.IsNullOrWhiteSpace(amountInput.text))
        {
            Debug.Log("금액을 입력해주세요.");
            return;
        }

        // 숫자 확인
        if (!int.TryParse(amountInput.text, out int amount))
        {
            Debug.Log("숫자만 입력 가능합니다.");
            return;
        }

        // 0 이하 금액 방지
        if (amount <= 0)
        {
            Debug.Log("0원보다 큰 금액을 입력해주세요.");
            return;
        }

        // 카테고리 선택 확인
        // 0번은 Select Category
        if (categoryDropdown.value == 0)
        {
            Debug.Log("카테고리를 선택해주세요.");
            return;
        }

        // 선택된 카테고리 가져오기
        string selectedCategory =
            categoryDropdown.options[
                categoryDropdown.value
            ].text;

        Debug.Log("===== 소비 기록 =====");
        Debug.Log($"Amount : {amount}");
        Debug.Log($"Category : {selectedCategory}");

        // 1. 현재 사용 금액 + 퍼센트 갱신
        if (SpendBudgetManager.Instance != null)
        {
            SpendBudgetManager.Instance.AddSpending(amount);
        }
        else
        {
            Debug.LogWarning(
                "SpendBudgetManager.Instance가 없습니다."
            );
        }

        // 2. 거래내역 History에 추가
        if (transactionManager != null)
        {
            transactionManager.AddTransaction(
                selectedCategory,
                amount
            );
        }
        else
        {
            Debug.LogWarning(
                "SpendTransactionManager가 연결되지 않았습니다."
            );
        }

        // 3. 입력값 초기화
        amountInput.text = "";

        categoryDropdown.value = 0;
        categoryDropdown.RefreshShownValue();

        // 4. 소비 홈으로 복귀
        if (panelManager != null)
        {
            panelManager.BackToSpend();
        }
    }
}