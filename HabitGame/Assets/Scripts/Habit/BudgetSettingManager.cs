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

    [Header("Test Mode")]
    [SerializeField] private bool useLocalMode = true;

    private SpendingService spendingService;
    private HabitUIManager uiManager;

    private const string BudgetWeekKey =
        "SpendingBudget_WeekKey";

    private void Start()
    {
        spendingService =
            ServiceRegistry.Instance.Spending;

        uiManager =
            HabitUIManager.Instance;

        if (saveButton != null)
            saveButton.onClick.AddListener(OnClickSave);

        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        RefreshBudgetSettingState();
    }

    private async void OnClickSave()
    {
        // =========================================
        // 이번 주 예산 중복 설정 확인
        // =========================================
        if (IsBudgetAlreadySetThisWeek())
        {
            Debug.LogWarning(
                "이번 주 주간예산은 이미 설정되었습니다. " +
                "다음 주 월요일부터 다시 설정할 수 있습니다."
            );

            RefreshBudgetSettingState();
            return;
        }

        if (budgetInput == null)
        {
            Debug.LogWarning(
                "Budget Input이 연결되지 않았습니다."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(budgetInput.text))
        {
            Debug.LogWarning(
                "예산을 입력해주세요."
            );
            return;
        }

        if (!int.TryParse(
                budgetInput.text,
                out int budget))
        {
            Debug.LogWarning(
                "예산은 숫자로 입력해주세요."
            );
            return;
        }

        if (budget <= 0)
        {
            Debug.LogWarning(
                "예산은 0원보다 커야 합니다."
            );
            return;
        }

        if (saveButton != null)
            saveButton.interactable = false;

        try
        {
            // =========================================
            // LOCAL MODE
            // 서버 없이 Unity에서만 예산 반영
            // =========================================
            if (useLocalMode)
            {
                Debug.Log(
                    "===== LOCAL MODE : 예산 설정 ====="
                );

                if (SpendBudgetManager.Instance == null)
                {
                    Debug.LogWarning(
                        "SpendBudgetManager.Instance가 없습니다."
                    );
                    return;
                }

                SpendBudgetManager.Instance.SetWeeklyBudget(
                    budget
                );

                SpendBudgetManager.Instance.SetBudgetId(
                    -1
                );

                SaveCurrentBudgetWeek();

                Debug.Log(
                    $"Local Budget Amount : {budget:N0}원"
                );

                Debug.Log(
                    $"Budget Week : {GetCurrentWeekKey()}"
                );

                budgetInput.text = "";

                // 예산 저장 후 특수목표 설정 화면으로 이동
                OpenSpecialGoalSetting();

                return;
            }

            // =========================================
            // SERVER MODE
            // 실제 Spring Boot API 저장
            // =========================================
            if (spendingService == null)
            {
                Debug.LogWarning(
                    "SpendingService를 사용할 수 없습니다."
                );
                return;
            }

            CreateSpendingBudgetRequest request =
                new CreateSpendingBudgetRequest
                {
                    BudgetAmount = budget,
                    Period = "weekly"
                };

            Debug.Log(
                "===== 예산 저장 요청 시작 ====="
            );

            SpendingBudgetResponse response =
                await spendingService.CreateBudgetAsync(
                    request
                );

            if (response == null)
            {
                Debug.LogWarning(
                    "예산 저장 API 응답이 비어있습니다."
                );
                return;
            }

            if (SpendBudgetManager.Instance == null)
            {
                Debug.LogWarning(
                    "SpendBudgetManager.Instance가 없습니다."
                );
                return;
            }

            SpendBudgetManager.Instance.SetBudgetId(
                response.Id
            );

            SpendBudgetManager.Instance.SetWeeklyBudget(
                response.BudgetAmount
            );

            SaveCurrentBudgetWeek();

            Debug.Log(
                "===== 예산 저장 성공 ====="
            );

            Debug.Log(
                $"Budget ID : {response.Id}"
            );

            Debug.Log(
                $"Budget Amount : {response.BudgetAmount:N0}원"
            );

            Debug.Log(
                $"Budget Week : {GetCurrentWeekKey()}"
            );

            budgetInput.text = "";

            // 예산 저장 후 특수목표 설정 화면으로 이동
            OpenSpecialGoalSetting();
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
            RefreshBudgetSettingState();
        }
    }

    private void OnClickBack()
    {
        if (budgetInput != null)
            budgetInput.text = "";

        CloseBudgetPanel();
    }

    // =========================================
    // 예산 저장 후 특수목표 설정 화면으로 이동
    // =========================================
    private void OpenSpecialGoalSetting()
    {
        if (uiManager != null)
        {
            uiManager.OpenSpendAddPanel();
        }
        else
        {
            Debug.LogWarning(
                "HabitUIManager를 찾을 수 없습니다."
            );
        }
    }

    // =========================================
    // 이번 주 예산 설정 여부
    // =========================================
    public bool IsBudgetAlreadySetThisWeek()
    {
        string savedWeekKey =
            PlayerPrefs.GetString(
                BudgetWeekKey,
                ""
            );

        string currentWeekKey =
            GetCurrentWeekKey();

        return savedWeekKey ==
               currentWeekKey;
    }

    // =========================================
    // 현재 주 저장
    // =========================================
    private void SaveCurrentBudgetWeek()
    {
        string currentWeekKey =
            GetCurrentWeekKey();

        PlayerPrefs.SetString(
            BudgetWeekKey,
            currentWeekKey
        );

        PlayerPrefs.Save();
    }

    // =========================================
    // 월요일 기준 Week Key
    // =========================================
    private string GetCurrentWeekKey()
    {
        DateTime today =
            DateTime.Now.Date;

        int daysSinceMonday =
            ((int)today.DayOfWeek + 6) % 7;

        DateTime monday =
            today.AddDays(
                -daysSinceMonday
            );

        return monday.ToString(
            "yyyy-MM-dd"
        );
    }

    // =========================================
    // UI 상태 갱신
    // =========================================
    private void RefreshBudgetSettingState()
    {
        bool alreadySet =
            IsBudgetAlreadySetThisWeek();

        if (saveButton != null)
        {
            saveButton.interactable =
                !alreadySet;
        }

        if (budgetInput != null)
        {
            budgetInput.interactable =
                !alreadySet;
        }

        if (alreadySet)
        {
            Debug.Log(
                "이번 주 주간예산 설정 완료 - " +
                "다음 주 월요일에 다시 설정 가능합니다."
            );
        }
    }

    private void CloseBudgetPanel()
    {
        if (uiManager != null)
        {
            uiManager.CloseBudgetSetting();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR

    [ContextMenu("TEST - Reset Weekly Budget Lock")]
    private void ResetWeeklyBudgetLockForTest()
    {
        PlayerPrefs.DeleteKey(
            BudgetWeekKey
        );

        PlayerPrefs.Save();

        RefreshBudgetSettingState();

        Debug.Log(
            "주간예산 설정 제한 테스트 데이터가 초기화되었습니다."
        );
    }

#endif

    private void OnDestroy()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(
                OnClickSave
            );
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                OnClickBack
            );
        }
    }
}