using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class BattleManager : Singleton<BattleManager>
{
    private bool isBattle = false;    // 전투 로직/마스터 클라이언트 계산 진행 여부
    private PhotonView photonView;
    [SerializeField]
    private PhotonManager photonManager;

    private BattleUnit myUnit;
    private BattleUnit oppUnit;

    private const double BattleLimitTime = 60.0;
    private bool isBattleFinished = false;
    public bool IsBattleFinished => isBattleFinished;

    public string myResult { get; private set; }    // 나 자신의 승리여부 저장 (WIN, DRAW, LOSE)
    private string battleId;
    private long opponentUserId;

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
        if (isBattleFinished) return;    // 중복 방지

        isBattleFinished = true;
        isBattle = false;

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
        isBattleFinished = true;

        myResult = ConvertMasterResultToLocalResult(masterResult);

        Debug.Log($"전투 종료: {myResult}");

        myUnit = null;
        oppUnit = null;

        BattleBackendManager.Instance.SubmitBattleResult(
            battleId,
            myResult,
            opponentUserId
        );
    }

    // 마스터 클라이언트의 결과를 토대로 나의 결과를 산출
    private string ConvertMasterResultToLocalResult(int masterResult)
    {
        if (masterResult == 0)
        {
            return "DRAW";
        }

        bool masterWon = masterResult == 1;
        bool iWon = PhotonNetwork.IsMasterClient ? masterWon : !masterWon;

        return iWon ? "WIN" : "LOSE";
    }

    public void TreatTimeOver()
    {
        if (!PhotonNetwork.IsMasterClient) return;    // 마스터 클라이언트만 처리
        if (!isBattle || isBattleFinished) return;    // 중복 호출 방지
        if (myUnit == null || oppUnit == null) return;

        BattleUnit winner = GetWinner(myUnit, oppUnit);
        FinishBattle(winner);
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

            // 턴 사이클이 끝나기 전 타임오버가 되는 경우에 대한 안전장치
            if (!isBattle || isBattleFinished) break;

            if (second.hp > 0)
            {
                PerformAttack(second, first, turn);
            }
            yield return new WaitForSeconds(1f);

            // 안전장치 동일
            if (!isBattle || isBattleFinished) break;

            turn++;
        }
        BattleUnit winner = GetWinner(first, second);
        FinishBattle(winner);
    }

    private void StartBattle()
    {
        isBattle = true;
        isBattleFinished = false;

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

        battleId = System.Guid.NewGuid().ToString();

        double battleEndTime = PhotonNetwork.Time + BattleLimitTime;
        photonView.RPC("RPC_StartBattle", RpcTarget.All, battleEndTime, battleId);
    }

    [PunRPC]
    public void RPC_StartBattle(double battleEndTime, string receivedBattleId)
    {
        battleId = receivedBattleId;
        if (!photonManager.TryGetOpponentUserId(out opponentUserId))    // 상대방 id 저장
        {
            Debug.LogWarning("Failed to get opponent user id.");
            return;
        }

        if (!photonManager.TryCreateBattleUnits(out BattleUnit createdMyUnit, out BattleUnit createdOppUnit))
        {
            Debug.LogWarning("Failed to create battle units.");
            return;
        }

        myUnit = createdMyUnit;
        oppUnit = createdOppUnit;

        photonManager.PrepareBattlePanelUI(myUnit, oppUnit);
        BattleUIManager.Instance.ReadyComplete(battleEndTime);

 
        StartBattle();
    }
}