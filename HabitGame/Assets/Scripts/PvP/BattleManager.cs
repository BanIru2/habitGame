using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class BattleManager : Singleton<BattleManager>
{
    private bool isBattle = false;
    private PhotonView photonView;
    [SerializeField]
    private PhotonManager photonManager;

    private BattleUnit myUnit;
    private BattleUnit oppUnit;

    private void Awake()
    {
        base.Awake();
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if(photonManager == null)
        {
            photonManager = FindObjectOfType<PhotonManager>();
        }
    }

    private BattleUnit GetFirstAttack(BattleUnit my, BattleUnit opp)
    {
        if (my.spd > opp.spd) return my;
        if (my.spd < opp.spd) return opp;
        // 속도 동일 시 반반 확률
        return (Random.value < 0.5f) ? my : opp;
    }

    private void PerformAttack(BattleUnit attacker, BattleUnit defender, int turn)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 데미지 계산
        bool isCrit = Random.value < attacker.crtk;
        float damage = BattleCalculator.CalculateDamage(attacker, defender, isCrit, turn);

        // 최종 데미지 정수 변환 (버림) 적용
        int finalDamage = (int)damage;
        // 마지막에 체력이 0 밑으로 떨어지지 않게 고정
        defender.hp = Mathf.Max(0, defender.hp - finalDamage);

        // 공격을 당하는 주체가 나(방장)이면 true
        bool defenderIsPlayerOnMaster = defender == myUnit;

        photonView.RPC(
            "RPC_ApplyAttackResult",
            RpcTarget.All,
            defenderIsPlayerOnMaster,
            defender.hp,
            finalDamage,
            isCrit
        );
    }

    // 공격 결과 RPC
    [PunRPC]
    public void RPC_ApplyAttackResult(bool defenderIsPlayerOnMaster, int currentHp, int damage, bool isCrit)
    {
        // 내가 마스터 클라이언트이면서 방어자인 경우
        bool defenderIsMe = PhotonNetwork.IsMasterClient ? defenderIsPlayerOnMaster : !defenderIsPlayerOnMaster;

        BattleUnit defenderUnit = defenderIsMe ? myUnit : oppUnit;
        defenderUnit.hp = currentHp;

        BattleUIManager.Instance.UpdateCharacterHpBar(defenderIsMe, currentHp, defenderUnit.maxHp);

        Debug.Log($"Attack result received. Damage:{damage}, Crit:{isCrit}, DefenderIsMe:{defenderIsMe}, HP:{currentHp}/{defenderUnit.maxHp}");
    }

    [PunRPC]
    public void RPC_UpdateTurn(int turn)
    {
        BattleUIManager.Instance.UpdateTurnText(turn);
    }

    private void FinishBattle(BattleUnit winner)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 마스터 기준 결과
        int result;

        if (winner == null)
        {
            result = 0; // 비김
        }
        else if (winner == myUnit)
        {
            result = 1; // master 승
        }
        else
        {
            result = 2; // master 패
        }

        photonView.RPC("RPC_FinishBattle", RpcTarget.All, result);
    }

    // 전투 종료 RPC
    [PunRPC]
    public void RPC_FinishBattle(int masterResult)
    {
        isBattle = false;

        bool isDraw = masterResult == 0;
        bool iWon = false;

        if (!isDraw)
        {
            bool masterWon = masterResult == 1;
            // 내가 마스터 클라이언트일 때 마스터 클라이언트의 승리여부
            // 마스터 클라이언트가 아닐 때 마스터가 아닌 클라이언트의 승리여부
            iWon = PhotonNetwork.IsMasterClient ? masterWon : !masterWon;
        }

        if (isDraw)
        {
            Debug.Log("전투 종료: Draw");
        }
        else
        {
            Debug.Log($"전투 종료: {(iWon ? "Win" : "Lose")}");
        }

        myUnit = null;
        oppUnit = null;

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
        if (!PhotonNetwork.IsMasterClient) yield break;
        int turn = 1;

        while (first.hp > 0 && second.hp > 0 && isBattle)
        {
            photonView.RPC("RPC_UpdateTurn", RpcTarget.All, turn);

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

    private void StartBattle()
    {
        isBattle = true;

        // 방장만 연산
        if (!PhotonNetwork.IsMasterClient) return;

        BattleUnit first = GetFirstAttack(myUnit, oppUnit);
        BattleUnit second = (first == myUnit) ? oppUnit : myUnit;

        StartCoroutine(BattleRoutine(first, second));
    }

    public void SendReadyState(bool ready)
    {
        photonView.RPC("SyncReadyState", RpcTarget.Others, ready);
    }

    [PunRPC]
    public void SyncReadyState(bool ready)
    {
        BattleUIManager.Instance.RPC_UpdateOpponentReady(ready);
    }

    public void BroadcastStartBattle()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("RPC_StartBattle", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_StartBattle()
    {

        if (!photonManager.TryCreateBattleUnits(out BattleUnit createdMyUnit, out BattleUnit createdOppUnit))
        {
            Debug.LogWarning("Failed to create battle units.");
            return;
        }

        myUnit = createdMyUnit;
        oppUnit = createdOppUnit;

        photonManager.PrepareBattlePanelUI(myUnit, oppUnit);
        BattleUIManager.Instance.ReadyComplete();

 
        StartBattle();
        
    }
}