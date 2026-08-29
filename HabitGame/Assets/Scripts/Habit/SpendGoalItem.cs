using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendGoalItem : MonoBehaviour
{
    [SerializeField] private Toggle completeToggle;
    [SerializeField] private TextMeshProUGUI rewardText;

    private SpendingService spendingService;

    // 서버에서 받아오는 SpecialGoalId
    private long specialGoalId = -1;

    // 보상 지급 완료 여부
    private bool rewarded = false;

    // 중복 API 요청 방지
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

    // SpendListManager에서 Special Goal 생성/조회 후 호출
    public void SetSpecialGoalId(long id)
    {
        specialGoalId = id;
    }

    private async void OnToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        if (rewarded || isSubmitting)
            return;

        // 서버 SpecialGoalId가 없는 경우
        if (specialGoalId <= 0)
        {
            Debug.LogWarning(
                "SpecialGoalId가 없어 " +
                "특수 목표 보상을 요청할 수 없습니다."
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
            Debug.Log(
                "===== 특수 목표 보상 요청 시작 ====="
            );

            SpendingSpecialGoalRewardClaimRequest request =
                new SpendingSpecialGoalRewardClaimRequest
                {
                    SpecialGoalId = specialGoalId
                };

            SpendingSpecialGoalRewardClaimResponse response =
                await spendingService
                    .ClaimSpecialGoalRewardAsync(request);

            if (response == null)
            {
                Debug.LogWarning(
                    "특수 목표 보상 API 응답이 비어있습니다."
                );

                ResetToggle();
                return;
            }

            // 서버에서 정상 응답을 받은 뒤에만 보상 완료 처리
            rewarded = true;

            if (SpendRewardManager.Instance != null)
            {
                SpendRewardManager.Instance.SetGold(
                    response.Gold
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
                $"Earned Gold : {response.EarnedGold}"
            );

            Debug.Log(
                $"Total Gold : {response.Gold}"
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "특수 목표 보상 지급 실패\n" +
                e.Message
            );

            ResetToggle();
        }
        finally
        {
            isSubmitting = false;

            if (completeToggle != null && !rewarded)
            {
                completeToggle.interactable = true;
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