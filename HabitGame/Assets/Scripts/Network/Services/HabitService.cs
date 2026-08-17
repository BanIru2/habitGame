using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HabitService
{
    private readonly ApiClient apiClient;

    public HabitService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 생활 습관 목표 생성
    public Task<HabitGoalResponse> CreateGoalAsync(CreateHabitGoalRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<CreateHabitGoalRequest, HabitGoalResponse>(
            "/habit-goals",
            request
        );
    }
    // ⭐ 생활 습관 목표 조회
    public Task<List<HabitGoalResponse>> GetGoalsAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<List<HabitGoalResponse>>(
            $"/habit-goals?userId={userId}"
        );
    }
    // 생활 습관 기록 제출 (인증)
    public Task<HabitRecordResponse> CreateRecordAsync(CreateHabitRecordRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<CreateHabitRecordRequest, HabitRecordResponse>(
            "/habit-records",
            request
        );
    }

    // 생활 습관 보상 수령
    public Task<HabitRewardClaimResponse> ClaimRewardAsync(ClaimHabitRewardRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<ClaimHabitRewardRequest, HabitRewardClaimResponse>(
            "/rewards/claim",
            request
        );
    }

    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }
}
