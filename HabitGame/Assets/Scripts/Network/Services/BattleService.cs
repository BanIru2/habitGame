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
    public Task<BattleResultResponse> SubmitResultAsync(SubmitBattleResultRequest request)
    {
        return apiClient.PostAsync<SubmitBattleResultRequest, BattleResultResponse>(
            "/battle/results",
            request
        );
    }

    // PvP 배틀 결과 목록 조회
    // 과거 전투 로그 상세 조회 기능이 도입될 것이 아닌 이상 필요 없을 것으로 보임
    public Task<List<BattleResultReportResponse>> GetResultsAsync(long userId)
    {
        return apiClient.GetAsync<List<BattleResultReportResponse>>(
            $"/battle/results?userId={userId}"
        );
    }
}