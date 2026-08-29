using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpendTransactionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject transactionItemPrefab;

    [Header("Test Data")]
    [SerializeField] private bool useTestData = false;

    private async void Start()
    {
        if (useTestData)
        {
            ClearTransactions();
            CreateTestTransactions();
        }
        else
        {
            await LoadTransactions();
        }
    }

    // =========================================
    // 실제 API 거래내역 조회
    // =========================================
    public async System.Threading.Tasks.Task LoadTransactions()
    {
        try
        {
            List<SpendingTransactionResponse> transactions =
                await ServiceRegistry.Instance.Spending
                    .GetTransactionsAsync();

            if (transactions == null)
            {
                Debug.LogWarning(
                    "거래내역 응답이 비어있습니다."
                );
                return;
            }

            // 기존 UI 제거 후 서버 데이터로 다시 표시
            ClearTransactions();

            Debug.Log(
                $"거래내역 조회 성공 : {transactions.Count}건"
            );

            foreach (SpendingTransactionResponse transaction in transactions)
            {
                if (transaction == null)
                    continue;

                string date =
                    transaction.RecordedAt;

                if (DateTime.TryParse(
                    transaction.RecordedAt,
                    out DateTime parsedDate))
                {
                    date =
                        parsedDate.ToString("yyyy.MM.dd");
                }

                AddTransaction(
                    transaction.Category,
                    transaction.Amount,
                    date
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "거래내역 API 조회 실패\n" +
                e.Message
            );
        }
    }

    // =========================================
    // 테스트용 거래내역
    // =========================================
    private void CreateTestTransactions()
    {
        AddTransaction(
            "Cafe",
            5000,
            "2026.08.17"
        );

        AddTransaction(
            "Food",
            12000,
            "2026.08.17"
        );

        AddTransaction(
            "Transport",
            1500,
            "2026.08.16"
        );

        AddTransaction(
            "Shopping",
            30000,
            "2026.08.15"
        );

        AddTransaction(
            "Convenience Store",
            8500,
            "2026.08.14"
        );
    }

    // =========================================
    // 사용자가 직접 소비 기록 추가할 때
    // =========================================
    public void AddTransaction(
        string category,
        int amount)
    {
        string date =
            DateTime.Now.ToString("yyyy.MM.dd");

        AddTransaction(
            category,
            amount,
            date
        );
    }

    // =========================================
    // 거래내역 Item 생성 공통 함수
    // =========================================
    private void AddTransaction(
        string category,
        int amount,
        string date)
    {
        if (content == null)
        {
            Debug.LogWarning(
                "Transaction Content가 연결되지 않았습니다."
            );
            return;
        }

        if (transactionItemPrefab == null)
        {
            Debug.LogWarning(
                "TransactionItem Prefab이 연결되지 않았습니다."
            );
            return;
        }

        GameObject newItem =
            Instantiate(
                transactionItemPrefab,
                content
            );

        Transform categoryTransform =
            newItem.transform.Find(
                "CategoryText"
            );

        Transform amountTransform =
            newItem.transform.Find(
                "AmountText"
            );

        Transform dateTransform =
            newItem.transform.Find(
                "DateText"
            );

        if (categoryTransform != null)
        {
            TextMeshProUGUI categoryText =
                categoryTransform
                    .GetComponent<TextMeshProUGUI>();

            if (categoryText != null)
            {
                categoryText.text =
                    string.IsNullOrWhiteSpace(category)
                        ? "-"
                        : category;
            }
        }

        if (amountTransform != null)
        {
            TextMeshProUGUI amountText =
                amountTransform
                    .GetComponent<TextMeshProUGUI>();

            if (amountText != null)
            {
                amountText.text =
                    "-" +
                    amount.ToString("N0") +
                    "₩";
            }
        }

        if (dateTransform != null)
        {
            TextMeshProUGUI dateText =
                dateTransform
                    .GetComponent<TextMeshProUGUI>();

            if (dateText != null)
            {
                dateText.text =
                    string.IsNullOrWhiteSpace(date)
                        ? "-"
                        : date;
            }
        }

        Debug.Log(
            $"거래내역 추가 : {category} / {amount:N0}₩ / {date}"
        );
    }

    // =========================================
    // 현재 표시 중인 거래내역 제거
    // =========================================
    private void ClearTransactions()
    {
        if (content == null)
            return;

        for (int i = content.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                content.GetChild(i).gameObject
            );
        }
    }
}