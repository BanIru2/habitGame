using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class BattleManager : Singleton<BattleManager>
{
    private BattleUnit player;
    private bool isBattle = false;

    private BattleUnit GetFirstAttack(BattleUnit my, BattleUnit opp)
    {
        if (my.spd > opp.spd) return my;
        if (my.spd < opp.spd) return opp;
        // 속도가 동일하면 반반확률로 랜덤 결정
        return (Random.value < 0.5f) ? my : opp;
    }

    private void PerformAttack(BattleUnit attacker, BattleUnit defender, int turn)
    {
        // 데미지 계산
        bool isCrit = Random.value < attacker.crtk;
        float damage = BattleCalculator.CalculateDamage(attacker, defender, isCrit, turn);
        defender.hp -= damage;
        Debug.Log($"{attacker.name}이(가) 공격, 데미지 : {damage}, 상대 남은 체력 : {defender.hp}");
        // 체력바 업데이트
        bool isPlayer = (defender == player) ? true : false;
        BattleUIManager.Instance.UpdateCharacterHpBar(isPlayer, defender.hp, defender.maxHp);
        // ++ 이펙트 등 그 외 동작
    }

    private void FinishBattle(BattleUnit winner)
    {
        // 전투 종료 동작
        if (winner == player) Debug.Log("전투종료 : 플레이어 승리");
        else if (winner == null) Debug.Log("전투종료 : 비김");
        else Debug.Log("전투 종료 : 플레이어 패배");
        player = null;
        isBattle = false;
        BattleUIManager.Instance.FinishBattle();
    }

    public void TreatTimeOver()
    {
        isBattle = false;
    }

    private BattleUnit GetWinner(BattleUnit first, BattleUnit second)
    {
        if (first.hp < second.hp) return second;
        else if(first.hp > second.hp) return first;
        else
        {
            return null;
        }
    }
    
    private IEnumerator BattleRoutine(BattleUnit first, BattleUnit second)
    {
        int turn = 1;

        while (first.hp > 0 && second.hp > 0 && isBattle)
        {
            PerformAttack(first, second, turn);
            yield return new WaitForSeconds(1f);
            if (second.hp > 0)
            {
                PerformAttack(second, first, turn);
            }
            yield return new WaitForSeconds(1f);

            turn++;
        }
        BattleUnit winner = GetWinner(first, second);
        FinishBattle(winner);
    }

    public void StartBattle(BattleUnit my, BattleUnit opp)
    {
        player = my;
        isBattle = true;
        BattleUnit first = GetFirstAttack(my, opp);
        BattleUnit second = (first == my) ? opp : my;

        StartCoroutine(BattleRoutine(first, second));
    }
}
