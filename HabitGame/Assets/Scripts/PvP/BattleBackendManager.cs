using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 관련 백엔드 호출 기능
/// </summary>
public class BattleBackendManager : Singleton<BattleBackendManager>
{
    [SerializeField]
    private ServiceRegistry serviceRegistry;

    public async void SubmitBattleResult(string battleId, string result, long oppUserId)
    {
        BattleResultRequest request = new BattleResultRequest
        {
            BattleId = battleId,
            OpponentUserId = oppUserId,
            Result = result,
            SelectedAttribute = ""
        };

        BattleResultResponse response = await serviceRegistry.Battle.SubmitResultAsync(request);

        // 백엔드 실 연결 전 테스트용
/*        BattleResultResponse tmpResponse = new BattleResultResponse
        {
            Result = result,
            ScoreBefore = 1000,
            ScoreDelta = result == "WIN" ? 20 : result == "LOSE" ? -15 : 0,
            ScoreAfter = 1020,
            RankBefore = 10,
            RankAfter = 9
        };*/

        BattleUIManager.Instance.FinishBattle(response);
    }
}
