using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendListManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField goalNameInput;
    [SerializeField] private TMP_InputField goldInput;

    [Header("Preview List - 특수목표 설정 화면")]
    [SerializeField] private Transform previewContent;
    [SerializeField] private GameObject specialGoalPreviewPrefab;

    [Header("Main Spend List - 소비 메인 화면")]
    [SerializeField] private Transform mainContent;
    [SerializeField] private GameObject spendItemPrefab;

    [Header("Bottom Buttons")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Button completeButton;

    [Header("Reward")]
    [SerializeField] private int reward = 300;

    [Header("Test")]
    [Tooltip("체크하면 서버 호출 없이 로컬에서만 테스트합니다.")]
    [SerializeField] private bool useLocalMode = true;

    // 이번 설정 과정에서 추가한 특수목표
    private readonly List<CreateSpendingSpecialGoalData>
        pendingSpecialGoals =
            new List<CreateSpendingSpecialGoalData>();

    // 실행 중 생성된 Preview Item
    private readonly List<GameObject>
        runtimePreviewItems =
            new List<GameObject>();

    // 이번 주 특수목표 설정 완료 여부
    private const string SpecialGoalWeekKey =
        "SpendingSpecialGoal_WeekKey";

    private HabitUIManager uiManager;

    // Complete 중복 클릭 방지
    private bool isSaving = false;

    private void Start()
    {
        uiManager = HabitUIManager.Instance;

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipSpecialGoals);

        if (completeButton != null)
            completeButton.onClick.AddListener(CompleteSpecialGoals);

        RefreshButtonState();
    }

    // =========================================================
    // Add Goal
    // 한 번의 주간 설정 과정에서 여러 개 추가 가능
    // =========================================================
    public void AddSpendGoal()
    {
        if (IsSpecialGoalSettingFinishedThisWeek())
        {
            Debug.LogWarning(
                "이번 주 특수목표 설정은 이미 완료되었습니다. " +
                "다음 주 월요일부터 다시 설정할 수 있습니다."
            );

            return;
        }

        if (isSaving)
        {
            Debug.LogWarning(
                "특수목표를 저장 중입니다."
            );

            return;
        }

        if (goalNameInput == null || goldInput == null)
        {
            Debug.LogWarning(
                "소비 목표 Input이 연결되지 않았습니다."
            );

            return;
        }

        string goalName =
            goalNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(goalName))
        {
            Debug.LogWarning(
                "소비 목표 이름을 입력해주세요."
            );

            return;
        }

        if (!int.TryParse(
                goldInput.text,
                out int limitAmount))
        {
            Debug.LogWarning(
                "목표 금액은 숫자로 입력해주세요."
            );

            return;
        }

        if (limitAmount <= 0)
        {
            Debug.LogWarning(
                "목표 금액은 0원보다 커야 합니다."
            );

            return;
        }

        if (previewContent == null)
        {
            Debug.LogWarning(
                "Goal Preview Content가 연결되지 않았습니다."
            );

            return;
        }

        if (specialGoalPreviewPrefab == null)
        {
            Debug.LogWarning(
                "SpecialGoalPreviewItem Prefab이 연결되지 않았습니다."
            );

            return;
        }

        CreateSpendingSpecialGoalData specialGoal =
            new CreateSpendingSpecialGoalData
            {
                UserId = ApiClient.Instance.CurrentUserId,

                GoalName = goalName,
                LimitAmount = limitAmount,
                RewardGold = reward,

                Title = goalName,

                TargetDescription =
                    $"Weekly spending limit: {limitAmount} KRW",

                BonusGold = reward
            };

        pendingSpecialGoals.Add(
            specialGoal
        );

        CreatePreviewItem(
            goalName,
            limitAmount
        );

        Debug.Log(
            $"특수목표 추가 : {goalName} / " +
            $"{limitAmount:N0}원 / Reward +{reward}"
        );

        Debug.Log(
            $"현재 추가된 특수목표 수 : " +
            $"{pendingSpecialGoals.Count}"
        );

        goalNameInput.text = "";
        goldInput.text = "";

        RefreshButtonState();
    }

    // =========================================================
    // 설정 화면 Preview 생성
    // =========================================================
    private void CreatePreviewItem(
        string goalName,
        int limitAmount)
    {
        GameObject newItem =
            Instantiate(
                specialGoalPreviewPrefab,
                previewContent
            );

        runtimePreviewItems.Add(
            newItem
        );

        Transform goalNameTransform =
            newItem.transform.Find(
                "GoalNameText"
            );

        if (goalNameTransform != null)
        {
            TextMeshProUGUI goalNameText =
                goalNameTransform
                    .GetComponent<TextMeshProUGUI>();

            if (goalNameText != null)
            {
                goalNameText.text =
                    goalName;
            }
        }

        Transform limitTransform =
            newItem.transform.Find(
                "LimitText"
            );

        if (limitTransform != null)
        {
            TextMeshProUGUI limitText =
                limitTransform
                    .GetComponent<TextMeshProUGUI>();

            if (limitText != null)
            {
                limitText.text =
                    $"Limit : {limitAmount:N0} KRW";
            }
        }

        Transform rewardTransform =
            newItem.transform.Find(
                "RewardText"
            );

        if (rewardTransform != null)
        {
            TextMeshProUGUI rewardText =
                rewardTransform
                    .GetComponent<TextMeshProUGUI>();

            if (rewardText != null)
            {
                rewardText.text =
                    $"Reward : +{reward} Gold";
            }
        }
    }

    // =========================================================
    // 소비 메인 화면 Additional Goal 생성
    // =========================================================
    private void CreateMainGoalItem(
        CreateSpendingSpecialGoalData goal,
        long specialGoalId = -1)
    {
        if (goal == null)
            return;

        if (mainContent == null)
        {
            Debug.LogWarning(
                "Spend 메인 Additional Goal Content가 연결되지 않았습니다."
            );

            return;
        }

        if (spendItemPrefab == null)
        {
            Debug.LogWarning(
                "Spend Item Prefab이 연결되지 않았습니다."
            );

            return;
        }

        GameObject newItem =
            Instantiate(
                spendItemPrefab,
                mainContent
            );

        Transform goalNameTransform =
            newItem.transform.Find(
                "GoalName"
            );

        if (goalNameTransform != null)
        {
            TextMeshProUGUI goalNameText =
                goalNameTransform
                    .GetComponent<TextMeshProUGUI>();

            if (goalNameText != null)
            {
                goalNameText.text =
                    goal.GoalName;
            }
        }

        Transform descriptionTransform =
            newItem.transform.Find(
                "GoalDescription"
            );

        if (descriptionTransform != null)
        {
            TextMeshProUGUI descriptionText =
                descriptionTransform
                    .GetComponent<TextMeshProUGUI>();

            if (descriptionText != null)
            {
                descriptionText.text =
                    $"Spend less than {goal.LimitAmount:N0} won";
            }
        }

        Transform rewardTransform =
            newItem.transform.Find(
                "RewardText"
            );

        if (rewardTransform != null)
        {
            TextMeshProUGUI rewardText =
                rewardTransform
                    .GetComponent<TextMeshProUGUI>();

            if (rewardText != null)
            {
                rewardText.text =
                    $"+ {goal.RewardGold}";
            }
        }

        Toggle toggle =
            newItem.GetComponentInChildren<Toggle>();

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
        }

        // 서버에서 생성된 ID가 있다면 SpendGoalItem에 전달
        if (specialGoalId > 0)
        {
            SpendGoalItem spendGoalItem =
                newItem.GetComponent<SpendGoalItem>();

            if (spendGoalItem != null)
            {
                spendGoalItem.SetSpecialGoalId(
                    specialGoalId
                );
            }
        }

        Debug.Log(
            $"Spend 메인 목표 표시 완료 : " +
            $"{goal.GoalName} / ID : {specialGoalId}"
        );
    }

    // =========================================================
    // Skip
    // 이번 주 특수목표 없음으로 확정
    // =========================================================
    public void SkipSpecialGoals()
    {
        if (IsSpecialGoalSettingFinishedThisWeek())
        {
            Debug.LogWarning(
                "이번 주 특수목표 설정은 이미 완료되었습니다."
            );

            return;
        }

        if (isSaving)
        {
            Debug.LogWarning(
                "특수목표를 저장 중입니다."
            );

            return;
        }

        if (pendingSpecialGoals.Count > 0)
        {
            Debug.LogWarning(
                "이미 추가한 특수목표가 있습니다. " +
                "목표를 확정하려면 Complete를 눌러주세요."
            );

            return;
        }

        SaveCurrentSpecialGoalWeek();

        Debug.Log(
            "이번 주는 특수목표 없이 진행합니다."
        );

        OpenSpendMain();
    }

    // =========================================================
    // Complete 버튼
    // async void는 Unity Button 이벤트용
    // =========================================================
    public async void CompleteSpecialGoals()
    {
        if (IsSpecialGoalSettingFinishedThisWeek())
        {
            Debug.LogWarning(
                "이번 주 특수목표 설정은 이미 완료되었습니다."
            );

            return;
        }

        if (isSaving)
        {
            Debug.LogWarning(
                "이미 특수목표를 저장 중입니다."
            );

            return;
        }

        if (pendingSpecialGoals.Count == 0)
        {
            Debug.LogWarning(
                "추가된 특수목표가 없습니다. " +
                "특수목표 없이 진행하려면 Skip을 눌러주세요."
            );

            return;
        }

        isSaving = true;
        RefreshButtonState();

        try
        {
            // =================================================
            // Local Mode
            // 서버 없이 기존 UI 테스트
            // =================================================
            if (useLocalMode)
            {
                foreach (
                    CreateSpendingSpecialGoalData goal
                    in pendingSpecialGoals)
                {
                    Debug.Log(
                        $"[LOCAL] 특수목표 확정 : " +
                        $"{goal.GoalName} / " +
                        $"{goal.LimitAmount:N0}원"
                    );

                    CreateMainGoalItem(
                        goal
                    );
                }

                FinishSpecialGoalSetting();

                return;
            }

            // =================================================
            // Server Mode
            // POST /spending/goals
            // =================================================

            if (ApiClient.Instance.CurrentUserId <= 0)
            {
                Debug.LogError(
                    "로그인된 사용자 정보가 없습니다."
                );

                return;
            }

            foreach (
                CreateSpendingSpecialGoalData goal
                in pendingSpecialGoals)
            {
                CreateSpendingGoalRequest request =
                    new CreateSpendingGoalRequest
                    {
                        UserId =
                            ApiClient.Instance.CurrentUserId,

                        GoalName =
                            goal.GoalName,

                        LimitAmount =
                            goal.LimitAmount,

                        RewardGold =
                            goal.RewardGold
                    };

                Debug.Log(
                    $"[API] 특수목표 저장 요청 : " +
                    $"{request.GoalName} / " +
                    $"{request.LimitAmount:N0}원"
                );

                SpendingSpecialGoalResponse response =
                    await ServiceRegistry.Instance
                        .Spending
                        .CreateSpecialGoalAsync(
                            request
                        );

                if (response == null)
                {
                    throw new Exception(
                        $"특수목표 저장 응답이 없습니다. " +
                        $"Goal : {goal.GoalName}"
                    );
                }

                Debug.Log(
                    $"[API] 특수목표 저장 성공 : " +
                    $"{goal.GoalName} / " +
                    $"ID : {response.Id}"
                );

                CreateMainGoalItem(
                    goal,
                    response.Id
                );
            }

            // 모든 POST가 성공한 경우에만 확정
            FinishSpecialGoalSetting();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "특수목표 저장 실패 : " +
                e.Message
            );

            // 실패 시 Week Lock을 저장하지 않음
            // pendingSpecialGoals도 유지해서 재시도 가능
        }
        finally
        {
            isSaving = false;
            RefreshButtonState();
        }
    }

    // =========================================================
    // 모든 목표 저장 성공 후 최종 처리
    // =========================================================
    private void FinishSpecialGoalSetting()
    {
        int completedCount =
            pendingSpecialGoals.Count;

        SaveCurrentSpecialGoalWeek();

        pendingSpecialGoals.Clear();

        Debug.Log(
            $"이번 주 특수목표 설정 완료 : " +
            $"{completedCount}개"
        );

        OpenSpendMain();
    }

    // =========================================================
    // Spend 메인 화면으로 복귀
    // =========================================================
    private void OpenSpendMain()
    {
        if (uiManager == null)
        {
            uiManager =
                HabitUIManager.Instance;
        }

        if (uiManager != null)
        {
            uiManager.OpenSpend();
        }
        else
        {
            Debug.LogWarning(
                "HabitUIManager를 찾을 수 없습니다."
            );
        }
    }

    // =========================================================
    // 이번 주 설정 완료 여부
    // =========================================================
    public bool IsSpecialGoalSettingFinishedThisWeek()
    {
        string savedWeekKey =
            PlayerPrefs.GetString(
                SpecialGoalWeekKey,
                ""
            );

        string currentWeekKey =
            GetCurrentWeekKey();

        return savedWeekKey ==
               currentWeekKey;
    }

    // =========================================================
    // 이번 주 설정 완료 저장
    // =========================================================
    private void SaveCurrentSpecialGoalWeek()
    {
        string currentWeekKey =
            GetCurrentWeekKey();

        PlayerPrefs.SetString(
            SpecialGoalWeekKey,
            currentWeekKey
        );

        PlayerPrefs.Save();

        RefreshButtonState();

        Debug.Log(
            $"Special Goal Week : {currentWeekKey}"
        );
    }

    // =========================================================
    // 월요일 기준 Week Key
    // =========================================================
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

    // =========================================================
    // 버튼 상태
    // =========================================================
    private void RefreshButtonState()
    {
        bool finished =
            IsSpecialGoalSettingFinishedThisWeek();

        if (skipButton != null)
        {
            skipButton.interactable =
                !finished &&
                !isSaving;
        }

        if (completeButton != null)
        {
            completeButton.interactable =
                !finished &&
                !isSaving &&
                pendingSpecialGoals.Count > 0;
        }
    }

    // =========================================================
    // Pending 목표 반환
    // =========================================================
    public List<CreateSpendingSpecialGoalData>
        GetPendingSpecialGoals()
    {
        return new List<CreateSpendingSpecialGoalData>(
            pendingSpecialGoals
        );
    }

#if UNITY_EDITOR

    [ContextMenu("TEST - Reset Weekly Special Goal Lock")]
    private void ResetWeeklySpecialGoalLockForTest()
    {
        PlayerPrefs.DeleteKey(
            SpecialGoalWeekKey
        );

        PlayerPrefs.Save();

        RefreshButtonState();

        Debug.Log(
            "주간 특수목표 설정 제한 테스트 데이터가 초기화되었습니다."
        );
    }

#endif

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                SkipSpecialGoals
            );
        }

        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(
                CompleteSpecialGoals
            );
        }
    }
}