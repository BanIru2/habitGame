using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendTransactionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject transactionItemPrefab;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TMP_FontAsset itemFont;
    [SerializeField] private Sprite itemCardSprite;

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
    public async System.Threading.Tasks.Task LoadTransactions(
    bool throwOnFailure = false)
    {
        try
        {
            List<SpendingTransactionResponse> transactions =
                await ServiceRegistry.Instance.Spending
                    .GetTransactionsAsync();

            if (transactions == null)
            {
                if (throwOnFailure)
                    throw new Exception("거래내역 조회 응답이 없습니다.");

                Debug.LogWarning("거래내역 조회 응답이 비어있습니다.");
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

            SetEmptyStateVisible(transactions.Count == 0);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "거래내역 API 조회 실패\n" + e.Message
            );

            if (throwOnFailure)
                throw;
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

        ApplyRuntimeStyle(newItem);
        SetEmptyStateVisible(false);

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
                    GetCategoryKorean(transaction.Category);
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

        SetEmptyStateVisible(true);
    }

    private void SetEmptyStateVisible(bool visible)
    {
        if (emptyState != null)
        {
            emptyState.SetActive(visible);
        }
    }

    private void ApplyRuntimeStyle(GameObject item)
    {
        if (item == null)
            return;

        Image cardImage = item.GetComponent<Image>();
        if (cardImage != null && itemCardSprite != null)
        {
            cardImage.sprite = itemCardSprite;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;
        }

        LayoutElement layout = item.GetComponent<LayoutElement>();
        if (layout == null)
            layout = item.AddComponent<LayoutElement>();

        layout.minHeight = 200f;
        layout.preferredHeight = 200f;
        layout.flexibleHeight = 0f;

        foreach (TextMeshProUGUI text in
                 item.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (itemFont != null)
                text.font = itemFont;

            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;

            if (text.name == "AmountText")
            {
                text.color = new Color32(0x5B, 0x36, 0x39, 0xFF);
                text.fontStyle = FontStyles.Bold;
            }
            else if (text.name == "DateText")
            {
                text.color = new Color32(0x86, 0x68, 0x5D, 0xFF);
            }
        }
    }


    private bool isRefreshing = false;

    // 거래내역 새로고침 버튼용
    public async void RefreshTransactions()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;

        try
        {
            await LoadTransactions(true);

            if (SpendBudgetManager.Instance != null)
            {
                await SpendBudgetManager.Instance.LoadOverview(true);
            }

            Debug.Log("소비내역 새로고침 완료");
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "소비내역 새로고침 실패: " + e.Message
            );
        }
        finally
        {
            isRefreshing = false;
        }
    }


    private string GetCategoryKorean(string category)
    {
        switch (category?.Trim().ToUpperInvariant())
        {
            case "FOOD":
                return "식비";

            case "CAFE":
                return "카페";

            case "TRANSPORT":
                return "교통";

            case "SHOPPING":
                return "쇼핑";

            case "LIVING":
                return "생활";

            case "EDUCATION":
                return "교육";

            case "OTHER":
                return "기타";

            default:
                return string.IsNullOrWhiteSpace(category)
                    ? "-"
                    : category;
        }
    }


#if UNITY_EDITOR

    [Header("Editor Test Data")]
    [SerializeField, Range(1, 100)]
    private int testDataCount = 20;

    private bool isGeneratingTestData = false;

    [ContextMenu("TEST - Generate Spending Transactions")]
    private async void GenerateTestTransactions()
    {
        if (isGeneratingTestData)
            return;

        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Play 모드에서만 실행할 수 있습니다."
            );
            return;
        }

        isGeneratingTestData = true;

        try
        {
            DateTime today = DateTime.Today;
            int daysSinceMonday =
                ((int)today.DayOfWeek + 6) % 7;

            DateTime monday = today.AddDays(-daysSinceMonday);

            var request = new GenerateSpendingTestDataRequest
            {
                Count = testDataCount,
                StartDate = monday.ToString("yyyy-MM-dd"),
                EndDate = today.ToString("yyyy-MM-dd")
            };

            var response =
                await ServiceRegistry.Instance.Spending
                    .GenerateTestDataAsync(request);

            if (response == null || !response.Success)
            {
                Debug.LogWarning(
                    "더미 거래내역 생성에 실패했습니다."
                );
                return;
            }

            Debug.Log(
                $"더미 거래내역 {response.GeneratedCount}건 생성 완료"
            );

            // 생성 후 서버 목록과 소비 현황 새로고침
            await LoadTransactions(true);

            if (SpendBudgetManager.Instance != null)
                await SpendBudgetManager.Instance.LoadOverview(true);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "더미 거래내역 생성 실패: " + e.Message
            );
        }
        finally
        {
            isGeneratingTestData = false;
        }
    }

#endif

}
