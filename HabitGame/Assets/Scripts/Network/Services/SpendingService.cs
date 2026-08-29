using System.Collections.Generic;
using System.Threading.Tasks;



public class SpendingService
{
    private readonly ApiClient apiClient;

    public SpendingService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 소비 전체 내역 조회 ? 
    public Task<SpendingOverviewResponse> GetOverviewAsync(long userId)
    {
        return apiClient.GetAsync<SpendingOverviewResponse>(
            $"/spending/overview?userId={userId}");
    }
    // 소비 거래내역 조회
    public Task<List<SpendingTransactionResponse>> GetTransactionsAsync()
    {
        return apiClient.GetAsync<List<SpendingTransactionResponse>>(
            "/spending/transactions"
        );
    }

    // 주간 예산 설정
    public Task<SpendingBudgetResponse> CreateBudgetAsync(CreateSpendingBudgetRequest request)
    {
        if (request == null)
            throw new System.ArgumentNullException(nameof(request));

        request.UserId = apiClient.CurrentUserId;

        return apiClient.PostAsync<CreateSpendingBudgetRequest, SpendingBudgetResponse>(
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
