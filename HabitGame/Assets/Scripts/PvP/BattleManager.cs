using LayerLab.ArtMakerUnity;
using Photon.Pun;
using System.Collections;
using UnityEngine;

public class BattleManager : SceneSingleton<BattleManager>
{
    public const int MaxTurns = 15;

    private const float BattleIntroDelay = 0.5f;
    private const float AttackHitDelay = 0.42f;
    private const float ActionRecoveryDelay = 1.03f;
    private const float BattleEndWaitTime = 1.8f;

    [SerializeField] private PhotonManager photonManager;

    [Header("캐릭터 애니메이션 제어")]
    [SerializeField] private PartsManager myPartsManager;
    [SerializeField] private PartsManager oppPartsManager;

    private PhotonView photonView;
    private BattleUnit myUnit;
    private BattleUnit oppUnit;

    private bool isBattle;
    private bool isBattleFinished;
    private bool resultSubmitted;
    private bool localWasMasterAtStart;
    private Coroutine replayCoroutine;

    private string battleId;
    private long opponentUserId;

    public bool IsBattleFinished => isBattleFinished;
    public string myResult { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (photonManager == null)
            photonManager = FindObjectOfType<PhotonManager>();
    }

    private BattleUnit GetFirstAttack(BattleUnit my, BattleUnit opp)
    {
        if (my.spd > opp.spd) return my;
        if (my.spd < opp.spd) return opp;
        return Random.value < 0.5f ? my : opp;
    }

    private static BattleUnit CloneUnit(BattleUnit src)
    {
        return new BattleUnit
        {
            name = src.name,
            maxHp = src.maxHp,
            hp = src.hp,
            atk = src.atk,
            def = src.def,
            spd = src.spd,
            crtk = src.crtk,
            attribute = src.attribute,
            attributeLevel = src.attributeLevel
        };
    }

    private BattleReplay SimulateBattle(BattleUnit master, BattleUnit remote)
    {
        BattleUnit simMaster = CloneUnit(master);
        BattleUnit simRemote = CloneUnit(remote);
        BattleReplay replay = new BattleReplay();

        BattleUnit first = GetFirstAttack(simMaster, simRemote);
        BattleUnit second = first == simMaster ? simRemote : simMaster;

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            if (first.hp <= 0 || second.hp <= 0) break;

            SimulateAttack(first, second, turn, simMaster, replay);
            if (second.hp <= 0) break;

            SimulateAttack(second, first, turn, simMaster, replay);
            if (first.hp <= 0) break;
        }

        long masterRatio = (long)simMaster.hp * simRemote.maxHp;
        long remoteRatio = (long)simRemote.hp * simMaster.maxHp;

        replay.winnerMasterResult = masterRatio > remoteRatio ? 1 : masterRatio < remoteRatio ? 2 : 0;

        return replay;
    }

    private void SimulateAttack(BattleUnit attacker, BattleUnit defender, int turn, BattleUnit simMaster, BattleReplay replay)
    {
        bool isCrit = Random.value < attacker.crtk;
        int damage = (int)BattleCalculator.CalculateDamage(attacker, defender, isCrit, turn);

        defender.hp = Mathf.Max(0, defender.hp - damage);

        replay.turns.Add(new BattleTurnLog
        {
            turn = turn,
            attackerIsMaster = attacker == simMaster,
            damage = damage,
            isCrit = isCrit,
            defenderHp = defender.hp
        });
    }

    public void SendReadyState(bool ready)
    {
        photonView.RPC(nameof(SyncReadyState), RpcTarget.Others, ready);
    }

    [PunRPC]
    public void SyncReadyState(bool ready)
    {
        BattleUIManager.Instance.RPC_UpdateOpponentReady(ready);
    }

    public void BroadcastStartBattle()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!photonManager.TryCreateBattleUnits(out BattleUnit masterUnit, out BattleUnit remoteUnit))
        {
            Debug.LogError("Failed to create battle units for simulation.");
            return;
        }

        BattleReplay replay = SimulateBattle(masterUnit, remoteUnit);
        string newBattleId = System.Guid.NewGuid().ToString();
        string replayJson = JsonUtility.ToJson(replay);
        int masterActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        // 같은 PhotonView에서 시작 → 결과 순서로 전송한다.
        photonView.RPC(nameof(RPC_StartBattle), RpcTarget.All, newBattleId, replayJson, masterActorNumber);

        photonView.RPC(nameof(RPC_FinishBattle), RpcTarget.All, newBattleId, replay.winnerMasterResult);
    }

    [PunRPC]
    public void RPC_StartBattle(string receivedBattleId, string replayJson, int masterActorNumber)
    {
        if (battleId == receivedBattleId) return;

        BattleReplay replay;
        try
        {
            replay = JsonUtility.FromJson<BattleReplay>(replayJson);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Invalid replay JSON: {exception.Message}");
            return;
        }

        if (replay == null || replay.turns == null || replay.turns.Count == 0)
        {
            Debug.LogError("Replay has no actions.");
            return;
        }

        if (!photonManager.TryGetOpponentUserId(out long receivedOpponentId))
            return;

        if (!photonManager.TryCreateBattleUnits(out BattleUnit createdMyUnit, out BattleUnit createdOppUnit))
            return;

        CancelBattle();

        battleId = receivedBattleId;
        opponentUserId = receivedOpponentId;
        myUnit = createdMyUnit;
        oppUnit = createdOppUnit;
        localWasMasterAtStart = PhotonNetwork.LocalPlayer.ActorNumber == masterActorNumber;

        // 연출용 승패. 서버 제출은 다음 결과 RPC에서 실행한다.
        myResult = ConvertMasterResultToLocalResult(replay.winnerMasterResult);
        isBattle = true;

        BattleUIManager.Instance.ReadyComplete(battleId);
        photonManager.PrepareBattlePanelUI(myUnit, oppUnit);
        BattleUIManager.Instance.UpdateCharacterHpBar(true, myUnit.hp, myUnit.maxHp);
        BattleUIManager.Instance.UpdateCharacterHpBar(false, oppUnit.hp, oppUnit.maxHp);

        myPartsManager?.ResetBattleAnimation();
        oppPartsManager?.ResetBattleAnimation();

        replayCoroutine = StartCoroutine(PlayReplayRoutine(replay, battleId));
    }

    [PunRPC]
    public void RPC_FinishBattle(string receivedBattleId, int masterResult)
    {
        if (battleId != receivedBattleId || resultSubmitted) return;
        if (myUnit == null || oppUnit == null) return;

        resultSubmitted = true;

        // 논리 결과 수신 완료를 의미한다. 연출을 멈추는 플래그가 아니다.
        isBattleFinished = true;
        myResult = ConvertMasterResultToLocalResult(masterResult);

        // 기존 서버 규약과 메서드는 유지한다. 표시용 유닛과 별도 사본을 전달한다.
        BattleBackendManager.Instance.SubmitBattleResult(battleId, myResult, opponentUserId, CloneUnit(myUnit), CloneUnit(oppUnit));
    }

    private string ConvertMasterResultToLocalResult(int masterResult)
    {
        if (masterResult == 0) return "DRAW";

        bool masterWon = masterResult == 1;
        bool iWon = localWasMasterAtStart ? masterWon : !masterWon;
        return iWon ? "WIN" : "LOSE";
    }

    private IEnumerator PlayReplayRoutine(BattleReplay replay, string replayBattleId)
    {
        yield return new WaitForSeconds(BattleIntroDelay);

        int displayedTurn = 0;
        foreach (BattleTurnLog log in replay.turns)
        {
            if (!isBattle || battleId != replayBattleId) yield break;

            if (displayedTurn != log.turn)
            {
                displayedTurn = log.turn;
                BattleUIManager.Instance.UpdateTurnText(log.turn);
            }

            bool attackerIsMe = log.attackerIsMaster == localWasMasterAtStart;
            bool defenderIsMe = !attackerIsMe;

            PartsManager attacker = attackerIsMe ? myPartsManager : oppPartsManager;
            PartsManager defender = defenderIsMe ? myPartsManager : oppPartsManager;
            BattleUnit defenderUnit = defenderIsMe ? myUnit : oppUnit;

            attacker?.PlayAttackAnimation();
            yield return new WaitForSeconds(AttackHitDelay);

            defender?.PlayHitAnimation();
            defenderUnit.hp = log.defenderHp;
            BattleUIManager.Instance.UpdateCharacterHpBar(defenderIsMe, log.defenderHp, defenderUnit.maxHp);

            yield return new WaitForSeconds(ActionRecoveryDelay);
        }

        if (myResult == "WIN")
        {
            myPartsManager?.PlayVictoryAnimation();
            oppPartsManager?.PlayDefeatAnimation();
        }
        else if (myResult == "LOSE")
        {
            myPartsManager?.PlayDefeatAnimation();
            oppPartsManager?.PlayVictoryAnimation();
        }
        else
        {
            myPartsManager?.PlayDefeatAnimation();
            oppPartsManager?.PlayDefeatAnimation();
        }

        yield return new WaitForSeconds(BattleEndWaitTime);

        isBattle = false;
        replayCoroutine = null;
        BattleUIManager.Instance.OnReplayFinished(replayBattleId);
    }

    public void CancelBattle()
    {
        if (replayCoroutine != null)
        {
            StopCoroutine(replayCoroutine);
            replayCoroutine = null;
        }

        isBattle = false;
        isBattleFinished = false;
        resultSubmitted = false;
        localWasMasterAtStart = false;
        battleId = null;
        opponentUserId = 0;
        myResult = null;
        myUnit = null;
        oppUnit = null;
    }

    private void OnDisable()
    {
        CancelBattle();
    }
}