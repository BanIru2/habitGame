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

            ClearTransactions();

            Debug.Log(
                $"거래내역 조회 성공 : {transactions.Count}건"
            );

            foreach (SpendingTransactionResponse transaction
                     in transactions)
            {
                if (transaction == null)
                    continue;

                CreateTransactionItem(transaction);
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
    // 서버 거래내역 Item 생성
    // =========================================
    private void CreateTransactionItem(
        SpendingTransactionResponse transaction)
    {
        if (transaction == null)
            return;

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

        // -----------------------------------------
        // Category
        // -----------------------------------------
        Transform categoryTransform =
            newItem.transform.Find(
                "CategoryText"
            );

        if (categoryTransform != null)
        {
            TextMeshProUGUI categoryText =
                categoryTransform
                    .GetComponent<TextMeshProUGUI>();

            if (categoryText != null)
            {
                categoryText.text =
                    string.IsNullOrWhiteSpace(
                        transaction.Category)
                        ? "-"
                        : transaction.Category;
            }
        }

        // -----------------------------------------
        // Amount
        // -----------------------------------------
        Transform amountTransform =
            newItem.transform.Find(
                "AmountText"
            );

        if (amountTransform != null)
        {
            TextMeshProUGUI amountText =
                amountTransform
                    .GetComponent<TextMeshProUGUI>();

            if (amountText != null)
            {
                amountText.text =
                    "-" +
                    transaction.Amount.ToString("N0") +
                    "₩";
            }
        }

        // -----------------------------------------
        // Date
        // -----------------------------------------
        Transform dateTransform =
            newItem.transform.Find(
                "DateText"
            );

        if (dateTransform != null)
        {
            TextMeshProUGUI dateText =
                dateTransform
                    .GetComponent<TextMeshProUGUI>();

            if (dateText != null)
            {
                string date =
                    transaction.RecordedAt;

                if (DateTime.TryParse(
                        transaction.RecordedAt,
                        out DateTime parsedDate))
                {
                    date =
                        parsedDate.ToString(
                            "yyyy.MM.dd"
                        );
                }

                dateText.text =
                    string.IsNullOrWhiteSpace(date)
                        ? "-"
                        : date;
            }
        }

        // -----------------------------------------
        // 거래 ID + 기존 예외 상태 전달
        // -----------------------------------------
        SpendTransactionItem transactionItem =
            newItem.GetComponent<SpendTransactionItem>();

        if (transactionItem != null)
        {
            transactionItem.SetData(
                transaction
            );
        }
        else
        {
            Debug.LogWarning(
                "TransactionItem Prefab에 " +
                "SpendTransactionItem이 없습니다."
            );
        }

        Debug.Log(
            $"거래내역 추가 : " +
            $"{transaction.Id} / " +
            $"{transaction.Category} / " +
            $"{transaction.Amount:N0}₩ / " +
            $"Exception={transaction.IsException}"
        );
    }

    // =========================================
    // 테스트용 거래내역
    // =========================================
    private void CreateTestTransactions()
    {
        CreateTransactionItem(
            new SpendingTransactionResponse
            {
                Id = 1,
                Category = "Cafe",
                Amount = 5000,
                RecordedAt = "2026-08-17",
                IsException = false,
                ExceptionReason = ""
            }
        );

        CreateTransactionItem(
            new SpendingTransactionResponse
            {
                Id = 2,
                Category = "Food",
                Amount = 12000,
                RecordedAt = "2026-08-17",
                IsException = false,
                ExceptionReason = ""
            }
        );

        CreateTransactionItem(
            new SpendingTransactionResponse
            {
                Id = 3,
                Category = "Transport",
                Amount = 1500,
                RecordedAt = "2026-08-16",
                IsException = true,
                ExceptionReason = "사용자 지정 예외"
            }
        );
    }

    // =========================================
    // 사용자가 직접 소비 기록 추가할 때
    // =========================================
    public void AddTransaction(
        string category,
        int amount)
    {
        SpendingTransactionResponse transaction =
            new SpendingTransactionResponse
            {
                // 직접 입력 소비는 아직 서버 ID가 없음
                Id = -1,

                Category = category,
                Amount = amount,
                RecordedAt =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd"
                    ),

                IsException = false,
                ExceptionReason = ""
            };

        CreateTransactionItem(
            transaction
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