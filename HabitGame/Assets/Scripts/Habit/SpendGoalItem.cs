using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendGoalItem : MonoBehaviour
{
    [SerializeField] private Toggle completeToggle;
    [SerializeField] private TextMeshProUGUI rewardText;

    private SpendingService spendingService;

    private long specialGoalId = -1;

    private bool rewarded = false;
    private bool isSubmitting = false;

    private void Start()
    {
        spendingService =
            ServiceRegistry.Instance.Spending;

        if (completeToggle != null)
        {
            completeToggle.onValueChanged
                .AddListener(OnToggleChanged);
        }
    }

    public void SetSpecialGoalId(long id)
    {
        specialGoalId = id;

        Debug.Log(
            $"SpendGoalItem SpecialGoalId 설정 : {id}"
        );
    }

    private async void OnToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        if (rewarded || isSubmitting)
            return;

        if (specialGoalId <= 0)
        {
            Debug.LogWarning(
                "SpecialGoalId가 없어 " +
                "특수 목표 상태를 변경할 수 없습니다."
            );

            ResetToggle();
            return;
        }

        if (spendingService == null)
        {
            Debug.LogWarning(
                "SpendingService를 사용할 수 없습니다."
            );

            ResetToggle();
            return;
        }

        isSubmitting = true;

        if (completeToggle != null)
            completeToggle.interactable = false;

        try
        {
            // 1. 특수 목표 달성 상태 업데이트
            UpdateSpendingSpecialGoalStatusRequest
                statusRequest =
                    new UpdateSpendingSpecialGoalStatusRequest
                    {
                        SpecialGoalId = specialGoalId,
                        IsAchieved = true
                    };

            Debug.Log(
                "===== 특수 목표 달성 상태 업데이트 ====="
            );

            SpendingSpecialGoalResponse
                updatedGoal =
                    await spendingService
                        .UpdateSpecialGoalStatusAsync(
                            statusRequest
                        );

            if (updatedGoal == null)
            {
                Debug.LogWarning(
                    "특수 목표 상태 업데이트 응답이 비어있습니다."
                );

                ResetToggle();
                return;
            }

            Debug.Log(
                $"특수 목표 달성 처리 성공 : {specialGoalId}"
            );

            // 2. 달성 처리 성공 후 보상 요청
            SpendingSpecialGoalRewardClaimRequest
                rewardRequest =
                    new SpendingSpecialGoalRewardClaimRequest
                    {
                        SpecialGoalId = specialGoalId
                    };

            Debug.Log(
                "===== 특수 목표 보상 요청 시작 ====="
            );

            SpendingSpecialGoalRewardClaimResponse
                rewardResponse =
                    await spendingService
                        .ClaimSpecialGoalRewardAsync(
                            rewardRequest
                        );

            if (rewardResponse == null)
            {
                Debug.LogWarning(
                    "특수 목표 보상 API 응답이 비어있습니다."
                );

                // 목표 달성 자체는 서버에 이미 저장됐으므로
                // Toggle은 유지한다.
                return;
            }

            rewarded = true;

            if (SpendRewardManager.Instance != null)
            {
                SpendRewardManager.Instance.SetGold(
                    rewardResponse.Gold
                );
            }
            else
            {
                Debug.LogWarning(
                    "SpendRewardManager.Instance가 없습니다."
                );
            }

            Debug.Log(
                "===== 특수 목표 보상 지급 성공 ====="
            );

            Debug.Log(
                $"Special Goal ID : {specialGoalId}"
            );

            Debug.Log(
                $"Earned Gold : {rewardResponse.EarnedGold}"
            );

            Debug.Log(
                $"Total Gold : {rewardResponse.Gold}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "특수 목표 처리 실패\n" +
                e.Message
            );

            // 어느 API에서 실패했는지 확실하지 않으므로
            // UI에서 완료로 확정하지 않는다.
            if (!rewarded)
                ResetToggle();
        }
        finally
        {
            isSubmitting = false;

            if (completeToggle != null)
            {
                completeToggle.interactable =
                    !rewarded;
            }
        }
    }

    private void ResetToggle()
    {
        if (completeToggle != null)
        {
            completeToggle.SetIsOnWithoutNotify(false);
            completeToggle.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (completeToggle != null)
        {
            completeToggle.onValueChanged
                .RemoveListener(OnToggleChanged);
        }
    }
}