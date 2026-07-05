using System.Collections.Generic;
using System.Threading.Tasks;

public class BattleService
{
    private readonly ApiClient apiClient;

    public BattleService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // PvP 배틀 결과 제출
    public Task<BattleResultReportResponse> SubmitResultAsync(SubmitBattleResultRequest request)
    {
        return apiClient.PostAsync<SubmitBattleResultRequest, BattleResultReportResponse>(
            "/battle/results",
            request
        );
    }

    // PvP 배틀 결과 목록 조회
    public Task<List<BattleResultReportResponse>> GetResultsAsync(long userId)
    {
        return apiClient.GetAsync<List<BattleResultReportResponse>>(
            $"/battle/results?userId={userId}"
        );
    }
}