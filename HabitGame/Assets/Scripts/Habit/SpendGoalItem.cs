using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendGoalItem : MonoBehaviour
{
    [SerializeField] private Toggle completeToggle;
    [SerializeField] private TextMeshProUGUI rewardText;

    private SpendingService spendingService;

    // 서버에서 받아올 SpecialGoalId
    private long specialGoalId = -1;

    private bool rewarded = false;

    private void Start()
    {
        spendingService = ServiceRegistry.Instance.Spending;

        completeToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    // SpendListManager에서 Goal 생성 후 호출 예정
    public void SetSpecialGoalId(long id)
    {
        specialGoalId = id;
    }

    private async void OnToggleChanged(bool isOn)
    {
        if (!isOn || rewarded)
            return;

        rewarded = true;

        try
        {
            // GoalId가 있을 때만 API 호출
            if (specialGoalId > 0)
            {
                SpendingSpecialGoalRewardClaimResponse response =
                    await spendingService.ClaimSpecialGoalRewardAsync(
                        new SpendingSpecialGoalRewardClaimRequest
                        {
                            SpecialGoalId = specialGoalId
                        });

                SpendRewardManager.Instance.SetGold(response.Gold);

                Debug.Log($"특수 목표 보상 지급 : +{response.EarnedGold} Gold");
            }
            else
            {
                // 아직 GoalId가 없으므로 로컬 테스트
                int reward = int.Parse(rewardText.text.Replace("+", ""));
                SpendRewardManager.Instance.AddGold(reward);

                Debug.Log("Local Reward");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);

            int reward = int.Parse(rewardText.text.Replace("+", ""));
            SpendRewardManager.Instance.AddGold(reward);
        }
    }

    private void OnDestroy()
    {
        completeToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }
}