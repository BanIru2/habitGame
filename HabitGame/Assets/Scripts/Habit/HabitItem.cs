using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HabitItem : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Toggle completeToggle;

    [Header("Detail")]
    [SerializeField] private Button labelButton;

    private HabitSummaryManager summaryManager;
    private HabitDetailManager detailManager;

    // 이 HabitItem의 습관 데이터
    private HabitGoalResponse habitData;

    // API 중복 요청 방지
    private bool isSubmitting = false;

    private void Start()
    {
        summaryManager =
            FindObjectOfType<HabitSummaryManager>();

        detailManager =
            FindObjectOfType<HabitDetailManager>();

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
    }

    // =========================================
    // Habit 데이터 연결
    // =========================================
    public void SetData(HabitGoalResponse data)
    {
        habitData = data;
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

        // 이미 서버 요청 중이면 중복 실행 방지
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

        // 로컬 테스트 데이터인 경우
        // 서버에서 생성된 ID가 없을 수 있음
        if (habitData.Id <= 0)
        {
            Debug.LogWarning(
                "Habit Goal ID가 없습니다.\n" +
                "현재 Habit이 로컬 테스트 데이터일 가능성이 있습니다."
            );

            RefreshSummary();
            return;
        }

        await SubmitHabitRecord();
    }

    // =========================================
    // Habit 실천 기록 저장
    // =========================================
    private async Task SubmitHabitRecord()
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

            // =========================================
            // 달성량 설정
            // =========================================
            if (habitData.RecordType == "check")
            {
                request.AchievedAmount = 1;
            }
            else
            {
                /*
                 * 현재 UI에는 실제 달성량 입력창이 없으므로
                 * Toggle 체크 시 목표량 전체를 완료한 것으로 처리.
                 *
                 * 추후 실제 달성량 입력 UI를 만들면
                 * 이 부분을 수정하면 됨.
                 */
                request.AchievedAmount =
                    habitData.TargetAmount;
            }

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
            // 서버에서 Record ID가 정상적으로 생성됐는지 확인
            // =========================================
            if (recordResponse.Id <= 0)
            {
                Debug.LogWarning(
                    "Habit Record ID가 올바르지 않아 " +
                    "보상을 요청할 수 없습니다."
                );

                RefreshSummary();
                return;
            }

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

            // =========================================
            // Summary 갱신
            // =========================================
            RefreshSummary();

            /*
             * 현재 기록 취소 API가 확인되지 않았기 때문에
             * 성공한 Habit은 다시 체크 해제하지 못하도록 함.
             */
            if (completeToggle != null)
            {
                completeToggle.interactable = false;
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
             * 실패해서 Toggle이 OFF 상태인 경우에만
             * 다시 클릭할 수 있도록 함.
             */
            if (completeToggle != null &&
                !completeToggle.isOn)
            {
                completeToggle.interactable = true;
            }
        }
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
             * 그래서 여기서는 Toggle 자체를
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
            /*
             * 이벤트를 다시 발생시키지 않고
             * Toggle만 OFF로 변경
             */
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