using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AchievementService
{
    private readonly ApiClient apiClient;

    public AchievementService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }

    // 내 업적 진행도 목록 조회
    public Task<List<AchievementResponse>> GetAchievementsAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<List<AchievementResponse>>(
            $"/achievements/me?userId={userId}"
        );
    }

    // 단일 업적 보상 수령 요청
    public Task<AchievementRewardClaimResponse> ClaimRewardAsync(AchievementRewardClaimRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<AchievementRewardClaimRequest, AchievementRewardClaimResponse>(
            "/achievements/claim",
            request
        );
    }
}