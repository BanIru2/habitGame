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
        return apiClient.PostAsync<CreateHabitGoalRequest, HabitGoalResponse>(
            "/habit-goals",
            request
        );
    }

    // 생활 습관 기록 제출 (인증)
    public Task<HabitRecordResponse> CreateRecordAsync(CreateHabitRecordRequest request)
    {
        return apiClient.PostAsync<CreateHabitRecordRequest, HabitRecordResponse>(
            "/habit-records",
            request
        );
    }

    // 생활 습관 보상 수령
    public Task<HabitRewardClaimResponse> ClaimRewardAsync(ClaimHabitRewardRequest request)
    {
        return apiClient.PostAsync<ClaimHabitRewardRequest, HabitRewardClaimResponse>(
            "/rewards/claim",
            request
        );
    }
}
