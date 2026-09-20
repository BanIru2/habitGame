using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitItem : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Toggle completeToggle;

    [Header("Detail")]
    [SerializeField] private Button labelButton;

    [Header("Progress")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Streak")]
    [SerializeField] private TextMeshProUGUI streakText;

    [Header("Streak Reward")]
    [SerializeField] private StreakRewardPopup streakRewardPopup;

    // 같은 실행 중 동일 Streak 마일스톤 중복 처리 방지
    // 영구적인 중복 지급 방지는 서버에서 처리해야 함.
    private int lastProcessedStreakMilestone = -1;

    private HabitSummaryManager summaryManager;
    private HabitDetailManager detailManager;
    private PhotoVerificationManager photoVerificationManager;

    // 이 HabitItem의 습관 데이터
    private HabitGoalResponse habitData;
    public HabitGoalResponse HabitData => habitData;

    // 현재 누적 달성량
    // 현재는 클라이언트 실행 중에만 유지되는 임시 값
    private int currentAmount = 0;

    // 최근 입력한 달성량 저장
    private int pendingAchievedAmount = 1;

    // API 중복 요청 방지
    private bool isSubmitting = false;
    // 인증 성공 여부 저장
    private bool isSuccessVerify = false;

    private void Start()
    {
        summaryManager =
            FindObjectOfType<HabitSummaryManager>();

        detailManager =
            FindObjectOfType<HabitDetailManager>();

        photoVerificationManager = FindObjectOfType<PhotoVerificationManager>();

        if (streakRewardPopup == null)
        {
            streakRewardPopup = FindObjectOfType<StreakRewardPopup>(true);
        }

        // =========================================
        // Toggle 이벤트
        // =========================================
        if (completeToggle != null)
        {
            completeToggle.onValueChanged
                .AddListener(OnToggleChanged);
        }

        // =========================================
        // Label 클릭 이벤트
        // =========================================
        if (labelButton != null)
        {
            labelButton.onClick
                .AddListener(OnClickLabel);
        }

        RefreshProgressUI();
    }

    // =========================================
    // Habit 데이터 연결
    // =========================================
    public void SetData(HabitGoalResponse data)
    {
        habitData = data;

        /*
         * 현재 HabitGoalResponse에는
         * 서버에 저장된 현재 누적 달성량이 없음.
         *
         * 따라서 새로 생성된 HabitItem은
         * 우선 0부터 시작.
         *
         * 추후 백엔드에서 currentAmount 같은 값을
         * 내려주면 이 부분에서 연결하면 됨.
         */
        currentAmount = 0;

        RefreshProgressUI();
        RefreshStreakUI();
    }

    // =========================================
    // Toggle 변경
    // =========================================
    private async void OnToggleChanged(bool isOn)
    {
        // 체크 해제 시에는 서버 요청하지 않음
        if (!isOn)
        {
            RefreshSummary();
            return;
        }

        // 이미 처리 중이면 중복 실행 방지
        if (isSubmitting)
        {
            return;
        }

        if (habitData == null)
        {
            Debug.LogWarning(
                "HabitItem에 습관 데이터가 연결되지 않았습니다."
            );

            ResetToggle();
            return;
        }

        // 이미 목표 달성 상태라면 추가 기록 방지
        if (IsGoalCompleted())
        {
            SetCompletedState();
            return;
        }

        // =========================================
        // Value 타입
        // 실제 달성량 입력 Overlay 표시
        // =========================================
        if (habitData.RecordType == "value")
        {
            if (AchievedAmountOverlay.Instance == null)
            {
                Debug.LogWarning(
                    "AchievedAmountOverlay를 찾을 수 없습니다."
                );

                ResetToggle();
                return;
            }

            if (completeToggle != null)
            {
                completeToggle.interactable = false;
            }

            AchievedAmountOverlay.Instance.Open(
                habitData,
                OnAchievedAmountConfirmed,
                OnAchievedAmountCancelled
            );

            return;
        }

        // =========================================
        // Check 타입
        // 한 번 체크할 때마다 1씩 달성
        // =========================================

        // 사진 인증 목표인 경우
        if (habitData.VerificationType == "photo")
        {
            TryVerifyPhoto(1);
            return;
        }

        // 로컬 Habit
        if (habitData.Id <= 0)
        {
            HandleLocalHabitRecord(1);
            return;
        }

        // 실제 서버 Habit
        await SubmitHabitRecord(1);
    }

    // =========================================
    // 달성량 입력 Confirm
    // =========================================
    private async void OnAchievedAmountConfirmed(
        int achievedAmount)
    {
        if (habitData == null)
        {
            Debug.LogWarning(
                "Habit 데이터가 없습니다."
            );

            ResetToggle();
            return;
        }

        if (achievedAmount <= 0)
        {
            Debug.LogWarning(
                "달성량은 0보다 커야 합니다."
            );

            ResetToggle();
            return;
        }

        // 사진 인증 목표인 경우
        if (habitData.VerificationType == "photo")
        {
            TryVerifyPhoto(achievedAmount);
            return;
        }

        // =========================================
        // 로컬 Habit
        // =========================================
        if (habitData.Id <= 0)
        {
            HandleLocalHabitRecord(
                achievedAmount
            );

            return;
        }

        // =========================================
        // 실제 서버 Habit
        // =========================================
        await SubmitHabitRecord(
            achievedAmount
        );
    }

    // =========================================
    // 달성량 입력 Cancel
    // =========================================
    private void OnAchievedAmountCancelled()
    {
        ResetToggle();
    }

    // =========================================
    // 로컬 Habit 기록 처리
    // =========================================
    private void HandleLocalHabitRecord(
        int achievedAmount)
    {
        Debug.Log(
            "===== LOCAL Habit Record ====="
        );

        Debug.Log(
            "Habit Name : " +
            habitData.GoalName
        );

        Debug.Log(
            "Record Type : " +
            habitData.RecordType
        );

        Debug.Log(
            "Achieved Amount : " +
            achievedAmount
        );

        Debug.Log(
            "Unit : " +
            habitData.Unit
        );

        Debug.Log(
            "서버 ID가 없는 로컬 Habit이므로 " +
            "API 요청 없이 UI 테스트만 처리합니다."
        );

        // 달성량 누적
        AddProgress(
            achievedAmount
        );
    }

    private void TryVerifyPhoto(int achievedAmount)
    {
        pendingAchievedAmount = achievedAmount;

        if (photoVerificationManager == null)
        {
            photoVerificationManager = FindObjectOfType<PhotoVerificationManager>();
        }

        // 체크박스 끄기
        if (completeToggle != null) completeToggle.SetIsOnWithoutNotify(false);

        if (photoVerificationManager != null)
        {
            photoVerificationManager.ActivatePhotoVerificationPopup(this, habitData.Id, achievedAmount);
        }
    }

    public void GetSuccessOrFailToVerify(bool result, int currentStreak = 0)
    {
        if (completeToggle == null) return;

        if (result)
        {
            if (currentStreak > 0 && habitData != null)
            {
                habitData.StreakCount = currentStreak;

                // 사진 인증 응답으로 Streak가 갱신된 경우
                // Daily 10일 / Weekly 2주 단위 보상 마일스톤 확인
                CheckStreakReward();
            }

            RefreshStreakUI();
            AddProgress(pendingAchievedAmount);
            return;
        }

        ResetToggle();
    }

    // =========================================
    // Habit 실천 기록 저장
    // =========================================
    private async Task SubmitHabitRecord(
        int achievedAmount)
    {
        isSubmitting = true;

        if (completeToggle != null)
        {
            completeToggle.interactable = false;
        }

        try
        {
            // =========================================
            // Record Request 생성
            // =========================================
            CreateHabitRecordRequest request =
                new CreateHabitRecordRequest();

            request.GoalId =
                habitData.Id;

            /*
             * UserId는 HabitService에서
             * 현재 로그인 사용자 ID로 설정함.
             */

            request.AchievedAmount =
                achievedAmount;

            // 현재 인증 이미지 기능은 사용하지 않음
            request.ProofImageUrl = null;

            Debug.Log(
                "===== Habit Record 요청 ====="
            );

            Debug.Log(
                "Goal ID : " +
                request.GoalId
            );

            Debug.Log(
                "Achieved Amount : " +
                request.AchievedAmount
            );

            // =========================================
            // Habit Record API 호출
            // =========================================
            HabitRecordResponse recordResponse =
                await ServiceRegistry.Instance.Habit
                    .CreateRecordAsync(request);

            if (recordResponse == null)
            {
                Debug.LogWarning(
                    "Habit Record API 응답이 비어있습니다."
                );

                ResetToggle();
                return;
            }

            Debug.Log(
                "===== Habit Record 저장 성공 ====="
            );

            Debug.Log(
                "Record ID : " +
                recordResponse.Id
            );

            Debug.Log(
                "Goal ID : " +
                recordResponse.GoalId
            );

            Debug.Log(
                "Achieved Amount : " +
                recordResponse.AchievedAmount
            );

            Debug.Log(
                "Verified : " +
                recordResponse.IsVerified
            );

            Debug.Log(
                "Message : " +
                recordResponse.Message
            );

            // =========================================
            // Record ID 확인
            // =========================================
            if (recordResponse.Id <= 0)
            {
                Debug.LogWarning(
                    "Habit Record ID가 올바르지 않아 " +
                    "보상을 요청할 수 없습니다."
                );

                ResetToggle();
                return;
            }

            // =========================================
            // 기록 성공 → 현재 진행률 반영
            // =========================================
            AddProgress(
                achievedAmount
            );

            // =========================================
            // 이미 보상을 받은 Record라면
            // 중복 Claim 요청하지 않음
            // =========================================
            if (recordResponse.RewardClaimed)
            {
                Debug.Log(
                    "이미 보상이 지급된 Habit Record입니다."
                );
            }
            else
            {
                // =========================================
                // Habit Reward 자동 수령
                // =========================================
                await ClaimReward(
                    recordResponse.Id
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "Habit Record 처리 실패\n" +
                e.Message
            );

            ResetToggle();
        }
        finally
        {
            isSubmitting = false;

            /*
             * 목표 미달 상태라면
             * 다음 기록을 다시 입력할 수 있게 함.
             */
            if (completeToggle != null &&
                !IsGoalCompleted())
            {
                completeToggle.interactable = true;
            }
        }
    }

    // =========================================
    // 진행률 누적
    // =========================================
    private void AddProgress(
        int achievedAmount)
    {
        currentAmount += achievedAmount;

        int targetAmount =
            GetTargetAmount();

        // 목표량을 초과해서 표시하지 않도록 제한
        if (currentAmount > targetAmount)
        {
            currentAmount = targetAmount;
        }

        Debug.Log(
            "===== Habit Progress ====="
        );

        Debug.Log(
            "Current : " +
            currentAmount
        );

        Debug.Log(
            "Target : " +
            targetAmount
        );

        RefreshProgressUI();

        // =========================================
        // 목표 달성 여부에 따라 Toggle 상태 변경
        // =========================================
        if (IsGoalCompleted())
        {
            SetCompletedState();
        }
        else
        {
            SetInProgressState();
        }

        RefreshSummary();
    }

    // =========================================
    // 목표량 반환
    // =========================================
    private int GetTargetAmount()
    {
        if (habitData == null)
        {
            return 1;
        }

        if (habitData.TargetAmount <= 0)
        {
            return 1;
        }

        return habitData.TargetAmount;
    }

    // =========================================
    // 목표 완료 여부
    // =========================================
    private bool IsGoalCompleted()
    {
        int targetAmount =
            GetTargetAmount();

        return currentAmount >= targetAmount;
    }

    // =========================================
    // 진행 중 상태
    // =========================================
    private void SetInProgressState()
    {
        if (completeToggle != null)
        {
            completeToggle
                .SetIsOnWithoutNotify(false);

            completeToggle.interactable =
                true;
        }
    }

    // =========================================
    // 완료 상태
    // =========================================
    private void SetCompletedState()
    {
        if (completeToggle != null)
        {
            completeToggle
                .SetIsOnWithoutNotify(true);

            completeToggle.interactable =
                false;
        }

        RefreshProgressUI();
        RefreshSummary();
    }

    // =========================================
    // 진행률 UI 갱신
    // =========================================
    private void RefreshProgressUI()
    {
        if (habitData == null)
        {
            if (progressBar != null)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = 1f;
                progressBar.value = 0f;
            }

            if (progressText != null)
            {
                progressText.text =
                    "0 / 0";
            }

            return;
        }

        int targetAmount =
            GetTargetAmount();

        float progress =
            targetAmount <= 0
                ? 0f
                : (float)currentAmount /
                  targetAmount;

        progress =
            Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text =
                GetProgressText(
                    currentAmount,
                    targetAmount
                );
        }
        RefreshStreakUI();
    }
    // =========================================
    // Streak UI 갱신
    // =========================================
    private void RefreshStreakUI()
    {
        if (streakText == null)
            return;

        if (habitData == null)
        {
            streakText.gameObject.SetActive(false);
            return;
        }

        bool isWeekly =
            !string.IsNullOrEmpty(habitData.Period) &&
            habitData.Period.Trim().Equals(
                "weekly",
                System.StringComparison.OrdinalIgnoreCase
            );

        streakText.gameObject.SetActive(isWeekly);

        if (isWeekly)
        {
            streakText.text =
                $"{habitData.StreakCount} Week Streak";
        }

        Debug.Log(
            $"[Habit Streak] {habitData.GoalName} / " +
            $"Period={habitData.Period} / " +
            $"Streak={habitData.StreakCount} / " +
            $"Weekly={isWeekly}"
        );
    }
    // =========================================
    // Streak 보상 마일스톤 확인
    // =========================================
    private void CheckStreakReward()
    {
        if (habitData == null)
            return;

        int streakCount = habitData.StreakCount;

        if (streakCount <= 0)
            return;

        string period = habitData.Period;

        if (string.IsNullOrWhiteSpace(period))
            return;

        bool isDaily =
            period.Trim().Equals(
                "daily",
                System.StringComparison.OrdinalIgnoreCase
            );

        bool isWeekly =
            period.Trim().Equals(
                "weekly",
                System.StringComparison.OrdinalIgnoreCase
            );

        bool isRewardMilestone = false;

        // Daily: 10 / 20 / 30 ... 일마다 보상
        if (isDaily)
        {
            isRewardMilestone =
                streakCount >= 10 &&
                streakCount % 10 == 0;
        }

        // Weekly: 2 / 4 / 6 ... 주마다 보상
        if (isWeekly)
        {
            isRewardMilestone =
                streakCount >= 2 &&
                streakCount % 2 == 0;
        }

        if (!isRewardMilestone)
            return;

        // 현재 실행 중 같은 HabitItem에서 동일 마일스톤 중복 처리 방지
        if (lastProcessedStreakMilestone == streakCount)
        {
            Debug.Log(
                $"[Streak Reward] 이미 처리한 마일스톤입니다. " +
                $"Goal={habitData.GoalName}, Streak={streakCount}"
            );
            return;
        }

        lastProcessedStreakMilestone = streakCount;

        HandleStreakReward(
            streakCount,
            isDaily,
            isWeekly
        );
    }

    // =========================================
    // Streak 보상 처리
    // =========================================
    private void HandleStreakReward(
        int streakCount,
        bool isDaily,
        bool isWeekly)
    {
        Debug.Log("===== Habit Streak Reward =====");
        Debug.Log($"Goal ID : {habitData.Id}");
        Debug.Log($"Goal Name : {habitData.GoalName}");
        Debug.Log($"Period : {habitData.Period}");
        Debug.Log($"Streak : {streakCount}");
        Debug.Log("Reward : Trait Ticket x1");

        /*
         * TODO - Backend 연동
         *
         * 현재 InventoryService에는 탐색권을 '지급'하는 API가 없음.
         * 현재 단계에서는 Streak 마일스톤 감지,
         * 클라이언트 중복 실행 방지, 보상 팝업 표시까지만 처리함.
         *
         * 추후 서버 지급 API가 추가되면 이 위치에서 호출하고,
         * 실제 지급 성공 응답을 받은 뒤 팝업을 표시하도록 변경하면 됨.
         *
         * 영구 중복 지급 방지는
         * userId + goalId + milestone 기준으로 서버에서 처리 필요.
         */

        ShowStreakRewardPopup(
            streakCount,
            isDaily,
            isWeekly
        );
    }

    // =========================================
    // Streak 보상 팝업 표시
    // =========================================
    private void ShowStreakRewardPopup(
        int streakCount,
        bool isDaily,
        bool isWeekly)
    {
        if (streakRewardPopup == null)
        {
            streakRewardPopup =
                FindObjectOfType<StreakRewardPopup>(true);
        }

        if (streakRewardPopup == null)
        {
            Debug.LogWarning(
                "StreakRewardPopup을 찾을 수 없습니다."
            );
            return;
        }

        if (isDaily)
        {
            streakRewardPopup.ShowDailyReward(
                streakCount
            );
            return;
        }

        if (isWeekly)
        {
            streakRewardPopup.ShowWeeklyReward(
                streakCount
            );
        }
    }

    // =========================================
    // 진행률 Text 생성
    // =========================================
    private string GetProgressText(
        int current,
        int target)
    {
        if (habitData == null)
        {
            return "0 / 0";
        }

        string unit =
            habitData.Unit;

        // Complete형에서 unit이 check면
        // 화면에는 표시하지 않음
        if (string.IsNullOrWhiteSpace(unit) ||
            unit.ToLower() == "check")
        {
            return
                $"{current} / {target}";
        }

        return
            $"{current} / {target} {unit}";
    }

    // =========================================
    // Habit 보상 수령
    // =========================================
    private async Task ClaimReward(long recordId)
    {
        try
        {
            ClaimHabitRewardRequest request =
                new ClaimHabitRewardRequest();

            request.RecordId =
                recordId;

            /*
             * UserId는 HabitService에서
             * 현재 로그인 사용자 ID로 설정함.
             */

            Debug.Log(
                "===== Habit Reward 요청 ====="
            );

            Debug.Log(
                "Record ID : " +
                request.RecordId
            );

            // =========================================
            // Reward API 호출
            // =========================================
            HabitRewardClaimResponse response =
                await ServiceRegistry.Instance.Habit
                    .ClaimRewardAsync(request);

            if (response == null)
            {
                Debug.LogWarning(
                    "Habit Reward API 응답이 비어있습니다."
                );

                return;
            }

            Debug.Log(
                "===== Habit Reward 수령 성공 ====="
            );

            Debug.Log(
                "Record ID : " +
                response.RecordId
            );

            Debug.Log(
                "Goal ID : " +
                response.GoalId
            );

            Debug.Log(
                "Gold Reward : " +
                response.GoldReward
            );

            Debug.Log(
                "Attribute Type : " +
                response.AttributeType
            );

            Debug.Log(
                "Attribute EXP : " +
                response.EarnedAttributeExp
            );

            Debug.Log(
                "Reward Claimed : " +
                response.RewardClaimed
            );

            Debug.Log(
                "Message : " +
                response.Message
            );
        }
        catch (System.Exception e)
        {
            /*
             * Record 저장은 성공했는데
             * Reward만 실패할 수도 있음.
             *
             * 그래서 여기서는 Habit 기록 자체를
             * 실패 처리하지 않음.
             */
            Debug.LogWarning(
                "Habit Reward 수령 실패\n" +
                e.Message
            );
        }
    }

    // =========================================
    // Toggle 원상복구
    // =========================================
    private void ResetToggle()
    {
        if (completeToggle != null)
        {
            completeToggle
                .SetIsOnWithoutNotify(false);

            completeToggle.interactable =
                true;
        }

        RefreshSummary();
    }

    // =========================================
    // Summary 갱신
    // =========================================
    private void RefreshSummary()
    {
        if (summaryManager != null)
        {
            summaryManager.RefreshSummary();
        }
    }

    // =========================================
    // Label 클릭 → 상세 화면
    // =========================================
    private void OnClickLabel()
    {
        if (habitData == null)
        {
            Debug.LogWarning(
                "HabitItem에 습관 데이터가 연결되지 않았습니다."
            );

            return;
        }

        if (detailManager == null)
        {
            Debug.LogWarning(
                "HabitDetailManager를 찾을 수 없습니다."
            );

            return;
        }

        detailManager.OpenDetail(
            habitData
        );
    }
    // =========================================
    // TEST - Daily Streak Reward
    // 최종 정리 시 삭제
    // =========================================
    [ContextMenu("TEST Daily 10 Day Streak Reward")]
    private void TestDailyStreakReward()
    {
        if (habitData == null)
        {
            Debug.LogWarning("Habit 데이터가 없어 테스트할 수 없습니다.");
            return;
        }

        habitData.Period = "daily";
        habitData.StreakCount = 10;

        RefreshStreakUI();
        CheckStreakReward();
    }
    // =========================================
    // TEST - Weekly Streak Reward
    // 최종 정리 시 삭제
    // =========================================
    [ContextMenu("TEST Weekly 2 Week Streak Reward")]
    private void TestWeeklyStreakReward()
    {
        if (habitData == null)
        {
            Debug.LogWarning("Habit 데이터가 없어 테스트할 수 없습니다.");
            return;
        }

        habitData.Period = "weekly";
        habitData.StreakCount = 2;

        RefreshStreakUI();
        CheckStreakReward();
    }
    // =========================================
    // 이벤트 제거
    // =========================================
    private void OnDestroy()
    {
        if (completeToggle != null)
        {
            completeToggle.onValueChanged
                .RemoveListener(OnToggleChanged);
        }

        if (labelButton != null)
        {
            labelButton.onClick
                .RemoveListener(OnClickLabel);
        }
    }
}