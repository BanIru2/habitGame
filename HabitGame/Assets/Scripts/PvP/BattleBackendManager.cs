using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 배틀 결과 백엔드 호출 관리
/// </summary>
public class BattleBackendManager : Singleton<BattleBackendManager>
{
    public async Task GetRemainCount()
    {
        DailyPvpLimitResponse response = await ServiceRegistry.Instance.Battle.GetPvPLimitAsync(ApiClient.Instance.CurrentUserId);
        RankingboardManager.Instance.ApplyRemainCount(response);
    }

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

        BattleResultResponse response =
            await ServiceRegistry.Instance.Battle.SubmitResultAsync(request);

        BattleUIManager.Instance.FinishBattle(response);
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
