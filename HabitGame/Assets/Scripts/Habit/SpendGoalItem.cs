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

    public void Initialize(long id, bool isCompleted)
    {
        specialGoalId = id;
        rewarded = isCompleted;

        if (completeToggle != null)
        {
            completeToggle.SetIsOnWithoutNotify(isCompleted);
            completeToggle.interactable = id > 0 && !isCompleted;
        }

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
            UpdateSpendingSpecialGoalStatusRequest
                request =
                    new UpdateSpendingSpecialGoalStatusRequest
                    {
                        GoalId = specialGoalId
                    };

            Debug.Log(
                "===== 특수 목표 달성 상태 업데이트 ====="
            );

            SpendingSpecialGoalRewardClaimResponse
                response =
                    await spendingService
                        .CompleteGoalAsync(
                            request
                        );

            if (response == null || !response.IsCompleted)
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

            rewarded = true;

            if (SpendRewardManager.Instance != null)
            {
                SpendRewardManager.Instance.SetGold(
                    response.TotalGold
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
                $"Earned Gold : {response.RewardGold}"
            );

            Debug.Log(
                $"Total Gold : {response.TotalGold}"
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