using UnityEngine;

/// <summary>
/// 배틀 결과 백엔드 호출 관리
/// </summary>
public class BattleBackendManager : Singleton<BattleBackendManager>
{
    [SerializeField]
    private ServiceRegistry serviceRegistry;

    public async void SubmitBattleResult(string battleId, string result, long enemyUserId, BattleUnit myUnit, BattleUnit oppUnit)
    {
        double myPower = CalculatePower(myUnit);
        double enemyPower = CalculatePower(oppUnit);

        SubmitBattleResultRequest request = new SubmitBattleResultRequest
        {
            BattleId = battleId,
            UserId = ApiClient.Instance.CurrentUserId,
            EnemyUserId = enemyUserId,
            Result = result,
            MyPower = myPower,
            EnemyPower = enemyPower,
            GainedExp = result == "WIN" ? 30 : 0,
            GainedGold = result == "WIN" ? 100 : 0
        };

        BattleResultReportResponse reportResponse =
            await serviceRegistry.Battle.SubmitResultAsync(request);

        BattleResultResponse uiResponse = new BattleResultResponse
        {
            Result = reportResponse.Result,
            IsWin = reportResponse.Result == "WIN",

            ScoreBefore = 0,
            ScoreAfter = 0,
            ScoreDelta = 0,

            RankBefore = 0,
            RankAfter = 0
        };

        BattleUIManager.Instance.FinishBattle(uiResponse);
    }

    private double CalculatePower(BattleUnit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.maxHp
               + unit.atk
               + unit.def
               + unit.spd;
    }
}
