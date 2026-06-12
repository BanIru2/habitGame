using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : Singleton<BattleManager>
{
    private BattleUnit player;
    private bool isBattle = false;

    private BattleUnit GetFirstAttack(BattleUnit my, BattleUnit opp)
    {
        if (my.spd > opp.spd) return my;
        if (my.spd < opp.spd) return opp;
        // 속도 동일 시 반반 확률
        return (Random.value < 0.5f) ? my : opp;
    }

    private void PerformAttack(BattleUnit attacker, BattleUnit defender, int turn)
    {
        // 데미지 계산
        bool isCrit = Random.value < attacker.crtk;
        float damage = BattleCalculator.CalculateDamage(attacker, defender, isCrit, turn);

        // 최종 데미지 정수 변환 (버림) 적용
        int finalDamage = (int)damage;
        defender.hp -= finalDamage;

        Debug.Log($"{attacker.name}의 공격, 데미지 : {finalDamage}, 남은 상대 체력 : {defender.hp}");

        // 체력바 업데이트
        bool isPlayer = (defender == player) ? true : false;
        BattleUIManager.Instance.UpdateCharacterHpBar(isPlayer, defender.hp, defender.maxHp);
    }

    private void FinishBattle(BattleUnit winner)
    {
        // 결과 로그 출력
        if (winner == player) Debug.Log("전투 종료 : 플레이어 승리");
        else if (winner == null) Debug.Log("전투 종료 : 무승부");
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