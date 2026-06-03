using System.Threading.Tasks;

public class SpendingService
{
    private readonly ApiClient apiClient;

    public SpendingService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 거래 내역 조회
    public Task<SpendingOverviewResponse> GetOverviewAsync()
    {
        return apiClient.GetAsync<SpendingOverviewResponse>("/spending/overview");
    }

    // 주간 예산 설정
    public Task<SpendingOverviewResponse> CreateBudgetAsync(CreateSpendingBudgetRequest request)
    {
        return apiClient.PostAsync<CreateSpendingBudgetRequest, SpendingOverviewResponse>(
            "/spending/budgets",
            request
        );
    }

    // 예외 비용 처리
    public Task<SpendingOverviewResponse> UpdateExceptionAsync(UpdateSpendingExceptionRequest request)
    {
        return apiClient.PatchAsync<UpdateSpendingExceptionRequest, SpendingOverviewResponse>(
            "/spending/transactions/exception",
            request
        );
    }

    // 특수 목표 성공/실패 결정
    public Task<SpendingSpecialGoalResponse> UpdateSpecialGoalStatusAsync(UpdateSpendingSpecialGoalStatusRequest request)
    {
        return apiClient.PatchAsync<UpdateSpendingSpecialGoalStatusRequest, SpendingSpecialGoalResponse>(
            "/spending/special-goals/status",
            request
        );
    }

    // 소비 습관ㅍ보상 수령
    public Task<SpendingRewardClaimResponse> ClaimRewardAsync(SpendingRewardClaimRequest request)
    {
        return apiClient.PostAsync<SpendingRewardClaimRequest, SpendingRewardClaimResponse>(
            "/spending/rewards/claim",
            request
        );
    }

    // 특수 목표 보상 수령
    public Task<SpendingSpecialGoalRewardClaimResponse> ClaimSpecialGoalRewardAsync(SpendingSpecialGoalRewardClaimRequest request)
    {
        return apiClient.PostAsync<SpendingSpecialGoalRewardClaimRequest, SpendingSpecialGoalRewardClaimResponse>(
            "/spending/special-goals/rewards/claim",
            request
        );
    }
}
