using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AchievementBackendManager : MonoBehaviour
{
    // 업적 목록 조회 요청
    public async Task<List<AchievementResponse>> FetchAchievementsAsync()
    {
        return await ServiceRegistry.Instance.Achievement.GetAchievementsAsync();
    }

    // 보상 수령 요청
    public async Task<AchievementRewardClaimResponse> ClaimRewardAsync(string achievementId)
    {
        var request = new AchievementRewardClaimRequest
        {
            UserId = ApiClient.Instance.CurrentUserId,
            AchievementId = achievementId
        };

        return await ServiceRegistry.Instance.Achievement.ClaimRewardAsync(request);
    }
}