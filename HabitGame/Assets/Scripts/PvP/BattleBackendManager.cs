using System;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 배틀 결과 백엔드 호출 관리
/// </summary>
public class BattleBackendManager : Singleton<BattleBackendManager>
{
    private const int PollingMaxAttempts = 8;
    private const int PollingIntervalMilliseconds = 1000;

    private bool isHandlingBattleResult;
    private bool battleFinishHandled;
    private string handledBattleId;

    public async Task GetRemainCount()
    {
        DailyPvpLimitResponse response = await ServiceRegistry.Instance.Battle.GetPvPLimitAsync(ApiClient.Instance.CurrentUserId);
        RankingboardManager.Instance.ApplyRemainCount(response);
    }

    public async void SubmitBattleResult(string battleId, string result, long enemyUserId, BattleUnit myUnit, BattleUnit oppUnit)
    {
        if (string.IsNullOrWhiteSpace(battleId))
        {
            Debug.LogError("[BattleBackendManager] Cannot submit a battle result without a battleId.");
            ErrorPopupManager.Instance.ShowMessage("전투 식별자를 확인할 수 없어 결과를 동기화하지 못했습니다.");
            return;
        }

        if (isHandlingBattleResult
            || (battleFinishHandled && string.Equals(handledBattleId, battleId, StringComparison.Ordinal)))
        {
            Debug.LogWarning($"[BattleBackendManager] Ignored duplicate battle result handling: {battleId}");
            return;
        }

        handledBattleId = battleId;
        battleFinishHandled = false;
        isHandlingBattleResult = true;

        try
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

            if (TryFinishConfirmedBattle(response, battleId))
                return;

            if (!IsPending(response))
            {
                HandleUnexpectedStatus(response, "submission");
                return;
            }

            for (int attempt = 1; attempt <= PollingMaxAttempts; attempt++)
            {
                await Task.Delay(PollingIntervalMilliseconds);

                try
                {
                    response = await ServiceRegistry.Instance.Battle.GetMyResultAsync(battleId);
                }
                catch (ApiException exception) when (exception.StatusCode != 401 && exception.StatusCode != 403)
                {
                    Debug.LogWarning(
                        $"[BattleBackendManager] Battle result polling failed "
                        + $"({attempt}/{PollingMaxAttempts}, HTTP {exception.StatusCode}): {exception.Message}"
                    );
                    continue;
                }
                catch (Exception exception) when (!(exception is ApiException))
                {
                    Debug.LogWarning(
                        $"[BattleBackendManager] Battle result polling failed "
                        + $"({attempt}/{PollingMaxAttempts}): {exception.Message}"
                    );
                    continue;
                }

                if (TryFinishConfirmedBattle(response, battleId))
                    return;

                if (!IsPending(response))
                {
                    HandleUnexpectedStatus(response, "polling");
                    return;
                }
            }

            Debug.LogWarning($"[BattleBackendManager] Battle result confirmation timed out: {battleId}");
            ErrorPopupManager.Instance.ShowMessage("전투 결과 동기화가 지연되고 있습니다. 잠시 후 다시 시도해 주세요.");
        }
        catch (ApiException exception)
        {
            Debug.LogError($"[BattleBackendManager] Failed to submit or confirm battle result: {exception}");
            ErrorPopupManager.Instance.ShowApiError(exception);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BattleBackendManager] Failed to submit or confirm battle result: {exception}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
        finally
        {
            isHandlingBattleResult = false;
        }
    }

    private bool TryFinishConfirmedBattle(BattleResultResponse response, string battleId)
    {
        if (response == null
            || !string.Equals(response.Status, "CONFIRMED", StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(response.BattleId, battleId, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                $"[BattleBackendManager] Ignored a result for a different battle: "
                + $"expected={battleId}, actual={response.BattleId ?? "null"}"
            );
            return false;
        }

        if (battleFinishHandled)
            return true;

        battleFinishHandled = true;
        BattleUIManager.Instance.FinishBattle(response);
        return true;
    }

    private static bool IsPending(BattleResultResponse response)
    {
        return response != null
            && string.Equals(response.Status, "PENDING", StringComparison.Ordinal);
    }

    private static void HandleUnexpectedStatus(BattleResultResponse response, string source)
    {
        Debug.LogWarning(
            $"[BattleBackendManager] Unexpected battle result during {source}: "
            + $"status={response?.Status ?? "null"}, battleId={response?.BattleId ?? "null"}"
        );
        ErrorPopupManager.Instance.ShowMessage("전투 결과를 동기화하지 못했습니다. 잠시 후 다시 시도해 주세요.");
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
